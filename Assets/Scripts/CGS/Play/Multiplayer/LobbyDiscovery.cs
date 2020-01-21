using System.Net;
using System.Collections.Generic;
using Mirror.LiteNetLib4Mirror;

namespace CGS.Play.Multiplayer
{
    public class LobbyDiscovery : LiteNetLib4MirrorDiscovery
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
