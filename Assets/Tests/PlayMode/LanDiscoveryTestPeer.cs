/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Net;
using Cgs.Play.Multiplayer;

namespace Tests.PlayMode
{
    public class LanDiscoveryTestPeer : CgsNetDiscovery
    {
        internal int Requests { get; private set; }
        internal int Responses { get; private set; }
        internal DiscoveryResponseData LastResponse { get; private set; }

        protected override IEnumerable<IPAddress> GetBroadcastAddresses()
        {
            // Exercise multiple destinations without depending on the test machine's LAN.
            return new[] { IPAddress.Loopback, IPAddress.Parse("127.0.0.2") };
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
