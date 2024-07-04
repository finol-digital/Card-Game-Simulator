// https://github.com/Unity-Technologies/multiplayer-community-contributions/tree/main/com.community.netcode.extensions/Runtime/NetworkDiscovery

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Cgs.Play.Multiplayer
{
    [DisallowMultipleComponent]
    public abstract class NetworkDiscovery<TBroadCast, TResponse> : MonoBehaviour
        where TBroadCast : INetworkSerializable, new()
        where TResponse : INetworkSerializable, new()
    {
        private enum MessageType : byte
        {
            BroadCast = 0,
            Response = 1,
        }

        private UdpClient _mClient;

        [FormerlySerializedAs("m_Port")] [SerializeField]
        private ushort mPort = 47777;

        // This is long because unity inspector does not like ulong.
        [FormerlySerializedAs("m_UniqueApplicationId")] [SerializeField]
        private long mUniqueApplicationId;

        /// <summary>
        /// Gets a value indicating whether the discovery is running.
        /// </summary>
        [PublicAPI]
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Gets whether the discovery is in server mode.
        /// </summary>
        [PublicAPI]
        public bool IsServer { get; private set; }

        /// <summary>
        /// Gets whether the discovery is in client mode.
        /// </summary>
        [PublicAPI]
        public bool IsClient { get; private set; }

        public void OnApplicationQuit()
        {
            StopDiscovery();
        }

        private void OnValidate()
        {
            if (mUniqueApplicationId != 0)
                return;

            var value1 = (long) Random.Range(int.MinValue, int.MaxValue);
            var value2 = (long) Random.Range(int.MinValue, int.MaxValue);
            mUniqueApplicationId = value1 + (value2 << 32);
        }

        public void ClientBroadcast(TBroadCast broadCast)
        {
            if (!IsClient)
            {
                throw new InvalidOperationException(
                    "Cannot send client broadcast while not running in client mode. Call StartClient first.");
            }

            var endPoint = new IPEndPoint(IPAddress.Broadcast, mPort);

            using var writer = new FastBufferWriter(1024, Allocator.Temp, 1024 * 64);
            WriteHeader(writer, MessageType.BroadCast);

            writer.WriteNetworkSerializable(broadCast);
            var data = writer.ToArray();

            try
            {
                // This works because PooledBitStream.Get resets the position to 0 so the array segment will always start from 0.
                _mClient.SendAsync(data, data.Length, endPoint);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        /// <summary>
        /// Starts the discovery in server mode which will respond to client broadcasts searching for servers.
        /// </summary>
        public void StartServer()
        {
            StartDiscovery(true);
        }

        /// <summary>
        /// Starts the discovery in client mode. <see cref="ClientBroadcast"/> can be called to send out broadcasts to servers and the client will actively listen for responses.
        /// </summary>
        public void StartClient()
        {
            StartDiscovery(false);
        }

        public void StopDiscovery()
        {
            IsClient = false;
            IsServer = false;
            IsRunning = false;

            if (_mClient == null)
                return;

            try
            {
                _mClient.Close();
            }
            catch (Exception)
            {
                // We don't care about socket exception here. Socket will always be closed after this.
            }

            _mClient = null;
        }

        /// <summary>
        /// Gets called whenever a broadcast is received. Creates a response based on the incoming broadcast data.
        /// </summary>
        /// <param name="sender">The sender of the broadcast</param>
        /// <param name="broadCast">The broadcast data which was sent</param>
        /// <param name="response">The response to send back</param>
        /// <returns>True if a response should be sent back else false</returns>
        protected abstract bool ProcessBroadcast(IPEndPoint sender, TBroadCast broadCast, out TResponse response);

        /// <summary>
        /// Gets called when a response to a broadcast gets received
        /// </summary>
        /// <param name="sender">The sender of the response</param>
        /// <param name="response">The value of the response</param>
        protected abstract void ResponseReceived(IPEndPoint sender, TResponse response);

        private void StartDiscovery(bool isServer)
        {
            StopDiscovery();

            IsServer = isServer;
            IsClient = !isServer;

            // If we are not a server we use the 0 port (let udp client assign a free port to us)
            var port = isServer ? mPort : 0;

            _mClient = new UdpClient(port) {EnableBroadcast = true, MulticastLoopback = false};

            _ = ListenAsync(isServer ? ReceiveBroadcastAsync : new Func<Task>(ReceiveResponseAsync));

            IsRunning = true;
        }

        private async Task ListenAsync(Func<Task> onReceiveTask)
        {
            while (true)
            {
                try
                {
                    await onReceiveTask();
                }
                catch (ObjectDisposedException)
                {
                    // socket has been closed
                    break;
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        private async Task ReceiveResponseAsync()
        {
            var udpReceiveResult = await _mClient.ReceiveAsync();

            var segment = new ArraySegment<byte>(udpReceiveResult.Buffer, 0, udpReceiveResult.Buffer.Length);
            using var reader = new FastBufferReader(segment, Allocator.Persistent);

            try
            {
                if (!ReadAndCheckHeader(reader, MessageType.Response))
                {
                    return;
                }

                reader.ReadNetworkSerializable(out TResponse receivedResponse);
                ResponseReceived(udpReceiveResult.RemoteEndPoint, receivedResponse);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private async Task ReceiveBroadcastAsync()
        {
            var udpReceiveResult = await _mClient.ReceiveAsync();

            var segment = new ArraySegment<byte>(udpReceiveResult.Buffer, 0, udpReceiveResult.Buffer.Length);
            using var reader = new FastBufferReader(segment, Allocator.Persistent);

            try
            {
                if (!ReadAndCheckHeader(reader, MessageType.BroadCast))
                {
                    return;
                }

                reader.ReadNetworkSerializable(out TBroadCast receivedBroadcast);

                if (ProcessBroadcast(udpReceiveResult.RemoteEndPoint, receivedBroadcast, out var response))
                {
                    using var writer = new FastBufferWriter(1024, Allocator.Persistent, 1024 * 64);
                    WriteHeader(writer, MessageType.Response);

                    writer.WriteNetworkSerializable(response);
                    var data = writer.ToArray();

                    await _mClient.SendAsync(data, data.Length, udpReceiveResult.RemoteEndPoint);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void WriteHeader(FastBufferWriter writer, MessageType messageType)
        {
            // Serialize unique application id to make sure packet received is from same application.
            writer.WriteValueSafe(mUniqueApplicationId);

            // Write a flag indicating whether this is a broadcast
            writer.WriteByteSafe((byte) messageType);
        }

        private bool ReadAndCheckHeader(FastBufferReader reader, MessageType expectedType)
        {
            reader.ReadValueSafe(out long receivedApplicationId);
            if (receivedApplicationId != mUniqueApplicationId)
            {
                return false;
            }

            reader.ReadByteSafe(out var messageType);
            return messageType == (byte) expectedType;
        }
    }
}
