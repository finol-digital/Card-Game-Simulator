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

    void Start()
    {
        customConfig = true;
        connectionConfig.NetworkDropThreshold = 90;
    }

    public void SearchForHost()
    {
        if (Discovery.running)
            Discovery.StopBroadcast();
        Discovery.Initialize();
        Discovery.StartAsClient();
    }

    public override void OnStartHost()
    {
        base.OnStartHost();
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
        Debug.Log("CGSNet: Starting client");
        ClientScene.RegisterSpawnHandler(cardModelPrefab.GetComponent<NetworkIdentity>().assetId, SpawnCard, UnSpawnCard);
        playController.netText.text = "Players: ";
    }

    public GameObject SpawnCard(Vector3 position, NetworkHash128 assetId)
    {
        GameObject newCardGO = Instantiate(cardModelPrefab, playController.playAreaContent);
        CardModel cardModel = newCardGO.GetComponent<CardModel>();
        cardModel.transform.localPosition = cardModel.LocalPosition;
        cardModel.transform.rotation = cardModel.Rotation;
        cardModel.HideHighlight();
        playController.SetPlayActions(playController.playAreaContent.GetComponent<CardStack>(), cardModel);
        return newCardGO;
    }

    public void UnSpawnCard(GameObject spawned)
    {
        CardModel cardModel = spawned?.GetComponent<CardModel>();
        if (cardModel != null && !cardModel.hasAuthority)
            Destroy(spawned);
    }

    public override void OnStopHost()
    {
        base.OnStopHost();
        Debug.Log("Host Stopped");
    }

    public override void OnServerError(NetworkConnection conn, int errorCode)
    {
        base.OnServerError(conn, errorCode);
        Debug.Log("Server error:" + errorCode);
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        Debug.Log("Server Stopped");
    }

    public override void OnServerDisconnect(NetworkConnection conn)
    {
        base.OnServerDisconnect(conn);
        Debug.Log("Server disconnected");
    }

    public override void OnClientError(NetworkConnection conn, int errorCode)
    {
        base.OnClientError(conn, errorCode);
        Debug.Log("Client error:" + errorCode);
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        Debug.Log("Client Stopped");
    }

    public override void OnClientDisconnect(NetworkConnection conn)
    {
        base.OnClientDisconnect(conn);
        Debug.Log("Client Disconnected");
    }
}
