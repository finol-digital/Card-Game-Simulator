using UnityEngine;
using UnityEngine.Networking;

public class LocalNetManager : NetworkManager
{
    public static LocalNetManager Instance => (LocalNetManager)singleton;
    public NetPlayer LocalPlayer { get; set; }
    public PlayMode playController;
    public GameObject cardModelPrefab;

    private LobbyDiscovery _discovery;
    public LobbyDiscovery Discovery => _discovery ??
                                         (_discovery = CardGameManager.Instance.gameObject.GetOrAddComponent<LobbyDiscovery>());

    public void SearchForHost()
    {
        Debug.Log("CGSNet: Searching For Host");
        if (Discovery.running)
            Discovery.StopBroadcast();
        Discovery.Initialize();
        Discovery.StartAsClient();
    }

    public override void OnStartHost()
    {
        base.OnStartHost();
        playController.netText.text = "Players: ";
        Debug.Log("CGSNet: Starting Host");
        if (Discovery.running)
            Discovery.StopBroadcast();
        Discovery.Initialize();
        Discovery.StartAsServer();
    }

    public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
    {
        base.OnServerAddPlayer(conn, playerControllerId);
        Debug.Log("CGSNet: Host adds player: " + playerControllerId);
    }

    public override void OnStartClient(NetworkClient netClient)
    {
        base.OnStartClient(netClient);
        Debug.Log("CGSNet: Registering card spawn handler");
        ClientScene.RegisterSpawnHandler(cardModelPrefab.GetComponent<NetworkIdentity>().assetId, SpawnCard, UnSpawnCard);
    }

    public GameObject SpawnCard(Vector3 position, NetworkHash128 assetId)
    {
        Debug.Log("CGSNet: Spawning card as directed by server");
        GameObject newCardGO = Instantiate(cardModelPrefab, position, Quaternion.identity, playController.playAreaContent);
        CardModel cardModel = newCardGO.GetComponent<CardModel>();
        cardModel.transform.localPosition = cardModel.LocalPosition;
        cardModel.transform.rotation = cardModel.Rotation;
        cardModel.HideHighlight();
        playController.SetPlayActions(playController.playAreaContent.GetComponent<CardStack>(), cardModel);
        return newCardGO;
    }

    public void UnSpawnCard(GameObject spawned)
    {
        Debug.Log("CGSNet: Unspawning card as directed by server");
        CardModel cardModel = spawned?.GetComponent<CardModel>();
        if (cardModel != null && !cardModel.hasAuthority)
            Destroy(spawned);
    }

    public override void OnStopHost()
    {
        base.OnStopHost();
        Debug.Log("Host Stopped");
        CardGameManager.Instance.Messenger.Show("Host Stopped");
    }

    public override void OnServerError(NetworkConnection conn, int errorCode)
    {
        base.OnServerError(conn, errorCode);
        Debug.Log("Server error:" + errorCode);
        CardGameManager.Instance.Messenger.Show("Server error:" + errorCode);
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        Debug.Log("Server Stopped");
        CardGameManager.Instance.Messenger.Show("Server Stopped");
    }

    public override void OnServerDisconnect(NetworkConnection conn)
    {
        base.OnServerDisconnect(conn);
        Debug.Log("Server disconnected");
        CardGameManager.Instance.Messenger.Show("Server disconnected");
    }

    public override void OnClientError(NetworkConnection conn, int errorCode)
    {
        base.OnClientError(conn, errorCode);
        Debug.Log("Client error:" + errorCode);
        CardGameManager.Instance.Messenger.Show("Client error:" + errorCode);
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        Debug.Log("Client Stopped");
        CardGameManager.Instance.Messenger.Show("Client Stopped");
    }

    public override void OnClientDisconnect(NetworkConnection conn)
    {
        base.OnClientDisconnect(conn);
        Debug.Log("Client Disconnected");
        CardGameManager.Instance.Messenger.Show("Client Disconnected");
    }
}
