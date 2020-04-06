/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Net;
using UnityEngine;
using Mirror;
using Mirror.Discovery;

namespace CGS.Play.Multiplayer
{
    public delegate void OnServerDiscoveredDelegate(DiscoveryResponse response);

    public class DiscoveryRequest : MessageBase
    {
        // Can be used to add filters, client info, etc...
    }

    public class DiscoveryResponse : MessageBase
    {
        public long serverId;
        public IPEndPoint EndPoint { get; set; }
        public Uri uri;
        public string gameName;
        public int players;
        public int capacity;

        public override string ToString()
        {
            return $"{gameName}\n{uri.AbsoluteUri} - {players}/{capacity}";
        }
    }

    public class CGSNetDiscovery : NetworkDiscoveryBase<DiscoveryRequest, DiscoveryResponse>
    {
        public long ServerId { get; private set; }
        public Transport transport;
        public OnServerDiscoveredDelegate OnServerFound;

        public override void Start()
        {
            base.Start();
            ServerId = RandomLong();
            if (transport == null)
                transport = Transport.activeTransport;
        }

        //protected override void ProcessClientRequest(DiscoveryRequest request, IPEndPoint endpoint) { base.ProcessClientRequest(request, endpoint); }
        protected override DiscoveryResponse ProcessRequest(DiscoveryRequest request, IPEndPoint endpoint)
        {
            try
            {
                return new DiscoveryResponse
                {
                    serverId = ServerId,
                    // the endpoint is populated by the client
                    uri = transport.ServerUri(),
                    gameName = CardGameManager.Current.Name,
                    players = NetworkServer.connections.Count,
                    capacity = NetworkManager.singleton.maxConnections
                };
            }
            catch (NotImplementedException)
            {
                Debug.LogError($"Transport {transport} does not support network discovery");
                throw;
            }
        }

        protected override DiscoveryRequest GetRequest() => new DiscoveryRequest();
        protected override void ProcessResponse(DiscoveryResponse response, IPEndPoint endpoint)
        {
            response.EndPoint = endpoint;
            UriBuilder realUri = new UriBuilder(response.uri)
            {
                Host = response.EndPoint.Address.ToString()
            };
            response.uri = realUri.Uri;

            OnServerFound(response);
        }
    }

}
