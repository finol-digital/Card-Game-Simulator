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
        CardModel cardModel = spawned.GetComponent<CardModel>();
        if (cardModel != null && !cardModel.hasAuthority)
            Destroy(spawned);
    }

    public override void OnClientError(NetworkConnection conn, int errorCode)
    {
        base.OnClientError(conn, errorCode);
        CardGameManager.Instance.Messenger.Show("Client error:" + errorCode);
    }

    public override void OnClientDisconnect(NetworkConnection conn)
    {
        base.OnClientDisconnect(conn);
        CardGameManager.Instance.Messenger.Show("Client Disconnected");
    }

    public override void OnServerError(NetworkConnection conn, int errorCode)
    {
        base.OnServerError(conn, errorCode);
        CardGameManager.Instance.Messenger.Show("Server error:" + errorCode);
    }

    public override void OnServerDisconnect(NetworkConnection conn)
    {
        base.OnServerDisconnect(conn);
        CardGameManager.Instance.Messenger.Show("Server disconnected");
    }
}
