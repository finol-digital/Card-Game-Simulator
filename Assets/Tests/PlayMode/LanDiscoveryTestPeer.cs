/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Cgs.Play.Multiplayer;

namespace Tests.PlayMode
{
    public class LanDiscoveryTestPeer : CgsNetDiscovery
    {
        internal int Requests { get; private set; }
        internal int Responses { get; private set; }
        internal DiscoveryResponseData LastResponse { get; private set; }
        internal Queue<Exception> ReceiveErrors { get; } = new();

        protected override IEnumerable<IPAddress> GetBroadcastAddresses()
        {
            // Exercise every target entry without requiring platform-specific loopback aliases.
            return new[] { IPAddress.Loopback, IPAddress.Loopback };
        }

        protected override Task<UdpReceiveResult> ReceiveAsync(UdpClient client)
        {
            return ReceiveErrors.Count > 0
                ? Task.FromException<UdpReceiveResult>(ReceiveErrors.Dequeue())
                : base.ReceiveAsync(client);
        }

        protected override bool ProcessBroadcast(IPEndPoint sender, DiscoveryBroadcastData broadCast,
            out DiscoveryResponseData response)
        {
            Requests++;
            response = new DiscoveryResponseData { ServerName = "LAN test room", Port = 12345 };
            return true;
        }

        protected override void ResponseReceived(IPEndPoint sender, DiscoveryResponseData response)
        {
            Responses++;
            LastResponse = response;
            OnServerFound?.Invoke(sender, response);
        }
    }
}
