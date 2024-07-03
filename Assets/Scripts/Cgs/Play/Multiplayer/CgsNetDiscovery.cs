/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Net;
using Unity.Netcode;
using UnityEngine;

namespace Cgs.Play.Multiplayer
{
    public struct DiscoveryBroadcastData : INetworkSerializable
    {
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            // Required by interface
        }
    }

    public struct DiscoveryResponseData : INetworkSerializable
    {
        public ushort Port;
        public string ServerName;

        public override string ToString()
        {
            return $"{ServerName}";
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Port);
            serializer.SerializeValue(ref ServerName);
        }
    }

    public class CgsNetDiscovery : NetworkDiscovery<DiscoveryBroadcastData, DiscoveryResponseData>
    {
        public delegate void OnServerFoundDelegate(IPEndPoint ipEndPoint, DiscoveryResponseData discoveryResponseData);

        public OnServerFoundDelegate OnServerFound { get; set; }

        protected override bool ProcessBroadcast(IPEndPoint sender, DiscoveryBroadcastData broadCast,
            out DiscoveryResponseData response)
        {
            response = new DiscoveryResponseData
            {
                ServerName = CardGameManager.Current.Name,
                Port = CgsNetManager.Instance.Transport.ConnectionData.Port
            };
            Debug.Log($"Broadcast: {response}");
            return true;
        }

        protected override void ResponseReceived(IPEndPoint sender, DiscoveryResponseData response)
        {
            OnServerFound(sender, response);
        }
    }
}
