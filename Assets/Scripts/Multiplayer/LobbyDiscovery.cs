using System.Linq;
using UnityEngine.Networking;

public class LobbyDiscovery : NetworkDiscovery
{
    public LobbyMenu lobby;
    public bool HasReceivedBroadcast { get; set; }

    public void SearchForHost()
    {
        if (running)
            StopBroadcast();
        Initialize();
        StartAsClient();
    }

    public override void OnReceivedBroadcast(string fromAddress, string data)
    {
        HasReceivedBroadcast = true;
		if (lobby == null || !lobby.gameObject.activeInHierarchy || NetworkManager.singleton.isNetworkActive)
            return;

        lobby.DisplayHosts(broadcastsReceived.Keys.ToList());
    }

    public void StartHost()
    {
        if (running)
            StopBroadcast();
        Initialize();
        StartAsServer();
    }
}
