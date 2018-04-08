using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class LobbyDiscovery : NetworkDiscovery
{
    public const string BroadcastErrorMessage = "Unable to broadcast game session. Other players may not be able to join this game.";
    public const string ListenErrorMessage = "Error: Unable to listen for game sessions.";

    public LobbyMenu lobby;
    public bool HasReceivedBroadcast { get; set; }

    public void StartAsHost()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable
            || Network.player.ipAddress == "0.0.0.0" || Network.player.ipAddress == "0.0.0.0.0.0") {
            CardGameManager.Instance.Messenger.Show(BroadcastErrorMessage);
            return;
        }

        if (running)
            StopBroadcast();
        Initialize();
        StartAsServer();
    }

    public void SearchForHost()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable
            || Network.player.ipAddress == "0.0.0.0" || Network.player.ipAddress == "0.0.0.0.0.0") {
            CardGameManager.Instance.Messenger.Show(ListenErrorMessage);
            return;
        }

        if (running)
            StopBroadcast();
        Network.Disconnect();
        NetworkServer.Reset();
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

    void OnDestroy()
    {
        if (running)
            StopBroadcast();
        Network.Disconnect();
    }
}
