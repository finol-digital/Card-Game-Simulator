/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

// https://github.com/Unity-Technologies/multiplayer-community-contributions/tree/main/com.community.netcode.extensions/Runtime/NetworkDiscovery

using System;
using System.Collections.Generic;
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
        private const string BroadcastErrorMessage = "Unable to send LAN discovery request to ";
        private const string ReceiveErrorMessage = "Unable to receive LAN discovery packets: ";

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

        protected void OnDestroy()
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

            using var writer = new FastBufferWriter(1024, Allocator.Temp, 1024 * 64);
            WriteHeader(writer, MessageType.BroadCast);

            writer.WriteNetworkSerializable(broadCast);
            var data = writer.ToArray();

            // Directed broadcasts let routing reach each connected subnet instead of relying
            // on the single interface chosen for 255.255.255.255. Refresh on each search.
            _ = SendBroadcastsAsync(_mClient, data);
        }

        protected virtual IEnumerable<IPAddress> GetBroadcastAddresses()
        {
            return LanDiscoveryAddresses.GetBroadcastAddresses();
        }

        private async Task SendBroadcastsAsync(UdpClient client, byte[] data)
        {
            foreach (var address in GetBroadcastAddresses())
            {
                if (client != _mClient)
                    return;

                try
                {
                    await client.SendAsync(data, data.Length, new IPEndPoint(address, mPort));
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (SocketException e)
                {
                    if (client != _mClient)
                        return;
                    Debug.LogWarning(BroadcastErrorMessage + address + ": " + e.Message);
                }
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

            // If we are not a server we use the 0 port (let udp client assign a free port to us)
            var port = isServer ? mPort : 0;

            _mClient = new UdpClient(port) {EnableBroadcast = true, MulticastLoopback = false};

            IsServer = isServer;
            IsClient = !isServer;
            IsRunning = true;
            _ = ListenAsync(_mClient, isServer);
        }

        private async Task ListenAsync(UdpClient client, bool isServer)
        {
            // A stopped listener must never consume packets from a replacement socket.
            while (client == _mClient)
            {
                try
                {
                    if (isServer)
                        await ReceiveBroadcastAsync(client);
                    else
                        await ReceiveResponseAsync(client);
                }
                catch (ObjectDisposedException)
                {
                    // socket has been closed
                    break;
                }
                catch (SocketException e)
                {
                    if (client == _mClient)
                    {
                        Debug.LogWarning(ReceiveErrorMessage + e.Message);
                        StopDiscovery();
                    }
                    break;
                }
            }
        }

        private async Task ReceiveResponseAsync(UdpClient client)
        {
            var udpReceiveResult = await client.ReceiveAsync();
            if (client != _mClient)
                return;

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

        private async Task ReceiveBroadcastAsync(UdpClient client)
        {
            var udpReceiveResult = await client.ReceiveAsync();
            if (client != _mClient)
                return;

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

                    await client.SendAsync(data, data.Length, udpReceiveResult.RemoteEndPoint);
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
