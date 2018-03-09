using System.Linq;
using UnityEngine.Networking;

public class LobbyDiscovery : NetworkDiscovery
{
    public const string BroadcastErrorMessage = "Unable to broadcast game session. Other players may not be able to join this game.";
    public const string ListenErrorMessage = "Error: Unable to listen for game sessions.";

    public LobbyMenu lobby;
    public bool HasReceivedBroadcast { get; set; }

    void Start()
    {
        Initialize();
    }

    public void StartAsHost()
    {
        if (hostId == -1) {
            CardGameManager.Instance.Messenger.Show(BroadcastErrorMessage);
            return;
        }

        if (running)
            StopBroadcast();
        StartAsServer();
    }

    public void SearchForHost()
    {
        if (hostId == -1) {
            CardGameManager.Instance.Messenger.Show(BroadcastErrorMessage);
            return;
        }

        if (running)
            StopBroadcast();
        StartAsClient();
    }

    public override void OnReceivedBroadcast(string fromAddress, string data)
    {
        HasReceivedBroadcast = true;
		if (lobby == null || !lobby.gameObject.activeInHierarchy || NetworkManager.singleton.isNetworkActive)
            return;

        lobby.DisplayHosts(broadcastsReceived.Keys.ToList());
    }
}
