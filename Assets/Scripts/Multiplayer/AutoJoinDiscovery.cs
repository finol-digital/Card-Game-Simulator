using UnityEngine.Networking;

public class AutoJoinDiscovery : NetworkDiscovery
{
    public override void OnReceivedBroadcast(string fromAddress, string data)
    {
        if (NetworkManager.singleton.isNetworkActive)
            return;
        
        NetworkManager.singleton.networkAddress = fromAddress;
        NetworkManager.singleton.StartClient();
    }
}
