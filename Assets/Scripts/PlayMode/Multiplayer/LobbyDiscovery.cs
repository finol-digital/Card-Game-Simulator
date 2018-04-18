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
        if (Application.internetReachability != NetworkReachability.ReachableViaLocalAreaNetwork) {
            CardGameManager.Instance.Messenger.Show(BroadcastErrorMessage);
            return;
        }

        if (running)
            StopBroadcast();
        
        bool started = Initialize() && StartAsServer();
        if (!started)
            CardGameManager.Instance.Messenger.Show(BroadcastErrorMessage);
    }

    public void SearchForHost()
    {
        if (Application.internetReachability != NetworkReachability.ReachableViaLocalAreaNetwork) {
            if (Application.internetReachability != NetworkReachability.ReachableViaCarrierDataNetwork)
                CardGameManager.Instance.Messenger.Show(ListenErrorMessage);
            return;
        }

        if (running)
            StopBroadcast();
        Network.Disconnect();
        NetworkServer.Reset();

        bool started = Initialize() && StartAsClient();
        if (!started)
            CardGameManager.Instance.Messenger.Show(ListenErrorMessage);
    }

    public override void OnReceivedBroadcast(string fromAddress, string data)
    {
        HasReceivedBroadcast = true;
		if (lobby == null || !lobby.gameObject.activeInHierarchy || NetworkManager.singleton.isNetworkActive)
            return;

        lobby.DisplayHosts(broadcastsReceived.Keys.ToList());
    }
}
