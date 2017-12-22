using UnityEngine;
using UnityEngine.Networking;

public class LocalNetManager : NetworkManager
{
    public GameObject cardModelPrefab;
    public RectTransform playAreaContent;

    public NetPlayer LocalPlayer { get; set; }

    public static NetworkHash128 CardModelAssetId { get; private set; }

    public static LocalNetManager Instance => (LocalNetManager)singleton;

    private LobbyDiscovery _discovery;
    public LobbyDiscovery Discovery => _discovery ??
                                         (_discovery = CardGameManager.Instance.gameObject.GetOrAddComponent<LobbyDiscovery>());

    void Start()
    {
        CardModelAssetId = cardModelPrefab.GetComponent<NetworkIdentity>().assetId;
        ClientScene.RegisterSpawnHandler(CardModelAssetId, SpawnCard, UnSpawnCard);
    }

    public GameObject SpawnCard(Vector3 position, NetworkHash128 assetId)
    {
        GameObject newCardGO = Instantiate(cardModelPrefab, position, Quaternion.identity, playAreaContent);
        CardModel cardModel = newCardGO.GetComponent<CardModel>();
        cardModel.transform.localPosition = cardModel.LocalPosition;
        cardModel.HideHighlight();
        SetPlayActions(playAreaContent.GetComponent<CardStack>(), cardModel);
        return newCardGO;
    }

    public void SetPlayActions(CardStack cardStack, CardModel cardModel)
    {
        cardModel.DoubleClickAction = CardModel.ToggleFacedown;
        cardModel.SecondaryDragAction = cardModel.Rotate;
    }

    public void UnSpawnCard(GameObject spawned)
    {
        CardModel cardModel = spawned.GetComponent<CardModel>();
        if (cardModel != null && !cardModel.hasAuthority)
            Destroy(spawned);
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

    public override void OnClientConnect(NetworkConnection conn)
    {
        ClientScene.AddPlayer(conn, 0);
    }
}
