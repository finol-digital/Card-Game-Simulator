using System.Linq;
using UnityEngine.Networking;

public class LobbyDiscovery : NetworkDiscovery
{
    public LobbyMenu lobby;

    public override void OnReceivedBroadcast(string fromAddress, string data)
    {
        if (lobby == null || NetworkManager.singleton.isNetworkActive)
            return;

        lobby.DisplayHosts(broadcastsReceived.Keys.ToList());
    }
}
