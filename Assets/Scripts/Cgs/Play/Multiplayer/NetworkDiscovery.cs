/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
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

        private UdpClient _client;

        [SerializeField] private ushort port = 47777;

        // This is long because unity inspector does not like ulong.
        [SerializeField] private long uniqueApplicationId;

        /// <summary>
        /// Gets a value indicating whether the discovery is running.
        /// </summary>
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
            if (uniqueApplicationId != 0) return;
            var value1 = (long) Random.Range(int.MinValue, int.MaxValue);
            var value2 = (long) Random.Range(int.MinValue, int.MaxValue);
            uniqueApplicationId = value1 + (value2 << 32);
        }

        public void ClientBroadcast(TBroadCast broadCast)
        {
            if (!IsClient)
            {
                throw new InvalidOperationException(
                    "Cannot send client broadcast while not running in client mode. Call StartClient first.");
            }

            var endPoint = new IPEndPoint(IPAddress.Broadcast, port);

            using var writer = new FastBufferWriter(1024, Allocator.Temp, 1024 * 64);
            WriteHeader(writer, MessageType.BroadCast);

            writer.WriteNetworkSerializable(broadCast);
            var data = writer.ToArray();

            try
            {
                // This works because PooledBitStream.Get resets the position to 0 so the array segment will always start from 0.
                _client.SendAsync(data, data.Length, endPoint);
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

            if (_client == null)
                return;

            try
            {
                _client.Close();
            }
            catch (Exception)
            {
                // We don't care about socket exception here. Socket will always be closed after this.
            }

            _client = null;
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
            var localPort = isServer ? this.port : 0;

            _client = new UdpClient(localPort) {EnableBroadcast = true, MulticastLoopback = false};

            _ = ListenAsync(isServer ? ReceiveBroadcastAsync : new Func<Task>(ReceiveResponseAsync));

            IsRunning = true;
        }

        private static async Task ListenAsync(Func<Task> onReceiveTask)
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
                catch (Exception e)
                {
                    Debug.Log(e);
                    // will try again later
                }
            }
        }

        private async Task ReceiveResponseAsync()
        {
            var udpReceiveResult = await _client.ReceiveAsync();

            var segment = new ArraySegment<byte>(udpReceiveResult.Buffer, 0, udpReceiveResult.Buffer.Length);
            using var reader = new FastBufferReader(segment, Allocator.Temp);

            try
            {
                if (ReadAndCheckHeader(reader, MessageType.Response) == false)
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
            var udpReceiveResult = await _client.ReceiveAsync();

            var segment = new ArraySegment<byte>(udpReceiveResult.Buffer, 0, udpReceiveResult.Buffer.Length);
            using var reader = new FastBufferReader(segment, Allocator.Temp);

            try
            {
                if (ReadAndCheckHeader(reader, MessageType.BroadCast) == false)
                {
                    return;
                }

                reader.ReadNetworkSerializable(out TBroadCast receivedBroadcast);

                if (ProcessBroadcast(udpReceiveResult.RemoteEndPoint, receivedBroadcast, out TResponse response))
                {
                    using var writer = new FastBufferWriter(1024, Allocator.Temp, 1024 * 64);
                    WriteHeader(writer, MessageType.Response);

                    writer.WriteNetworkSerializable(response);
                    var data = writer.ToArray();

                    await _client.SendAsync(data, data.Length, udpReceiveResult.RemoteEndPoint);
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
            writer.WriteValueSafe(uniqueApplicationId);

            // Write a flag indicating whether this is a broadcast
            writer.WriteByteSafe((byte) messageType);
        }

        private bool ReadAndCheckHeader(FastBufferReader reader, MessageType expectedType)
        {
            reader.ReadValueSafe(out long receivedApplicationId);
            if (receivedApplicationId != uniqueApplicationId)
            {
                return false;
            }

            reader.ReadByteSafe(out var messageType);
            return messageType == (byte) expectedType;
        }
    }
}
