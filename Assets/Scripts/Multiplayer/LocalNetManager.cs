using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class LocalNetManager : NetworkManager
{
    public GameObject cardModelPrefab;
    public RectTransform playAreaContent;

    public NetPlayer LocalPlayer { get; set; }

    public static NetworkHash128 CardModelAssetId { get; private set; }

    private NetworkDiscovery _discovery;

    void Start()
    {
        LocalNetManager.CardModelAssetId = cardModelPrefab.GetComponent<NetworkIdentity>().assetId;
        ClientScene.RegisterSpawnHandler(CardModelAssetId, SpawnCard, UnSpawnCard);
    }

    public GameObject SpawnCard(Vector3 position, NetworkHash128 assetId)
    {
        GameObject newCardGO = Instantiate(cardModelPrefab, playAreaContent);
        newCardGO.transform.position = position;
        SetPlayActions(playAreaContent.GetComponent<CardStack>(), newCardGO.GetComponent<CardModel>());
        return newCardGO;
    }

    public void SetPlayActions(CardStack cardStack, CardModel cardModel)
    {
        cardModel.DoubleClickAction = CardModel.ToggleFacedown;
        cardModel.SecondaryDragAction = cardModel.Rotate;
    }

    public void UnSpawnCard(GameObject spawned)
    {
        Debug.Log("Unspawning on network");
        CardModel cardModel = spawned.GetComponent<CardModel>();
        if (cardModel != null && !cardModel.hasAuthority)
            GameObject.Destroy(spawned);
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

    public NetworkDiscovery Discovery {
        get {
            if (_discovery == null)
                _discovery = CardGameManager.Instance.gameObject.GetOrAddComponent<AutoJoinDiscovery>();
            return _discovery;
        }
    }
}
