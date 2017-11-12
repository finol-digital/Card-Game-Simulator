using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class LocalNetManager : NetworkManager
{
    public GameObject cardModelPrefab;
    public RectTransform playAreaContent;
    public NetworkDiscovery discovery;

    public NetPlayer LocalPlayer { get; set; }

    public static NetworkHash128 CardModelAssetId { get; private set; }

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
        Destroy(spawned);
    }

    public void SearchForHost()
    {
        if (discovery.running)
            discovery.StopBroadcast();
        discovery.Initialize();
        discovery.StartAsClient();
    }

    public override void OnStartHost()
    {
        base.OnStartHost();
        if (discovery.running)
            discovery.StopBroadcast();
        discovery.Initialize();
        discovery.StartAsServer();
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        ClientScene.AddPlayer(conn, 0);
    }
}
