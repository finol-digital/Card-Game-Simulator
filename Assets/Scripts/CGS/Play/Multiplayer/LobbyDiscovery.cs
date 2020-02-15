using System.Net;
using System.Collections.Generic;

namespace CGS.Play.Multiplayer
{
    public class LobbyDiscovery
    {
        public LobbyMenu lobby;

        public HashSet<string> IPs { get; } = new HashSet<string>();

        public void ProcessDiscoveryRequest(IPEndPoint iPEndPoint, string text)
        {
            IPs.Add(iPEndPoint.Address.ToString());
            lobby.DisplayHosts(IPs);
        }
    }
}
