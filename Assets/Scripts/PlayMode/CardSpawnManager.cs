using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class CardSpawnManager : MonoBehaviour
{
    public delegate GameObject SpawnDelegate(Vector3 position,NetworkHash128 assetId);

    public delegate void UnSpawnDelegate(GameObject spawned);

    public GameObject cardModelPrefab;

    public RectTransform PlayAreaContent { get; set; }

    public NetPlayer LocalPlayer { get; set; }

    public static NetworkHash128 CardModelAssetId { get; private set; }

    private static CardSpawnManager _instance;

    void Awake()
    {
        CardSpawnManager.Instance = this;
    }

    void Start()
    {
        CardSpawnManager.CardModelAssetId = cardModelPrefab.GetComponent<NetworkIdentity>().assetId;
        ClientScene.RegisterSpawnHandler(CardModelAssetId, SpawnCard, UnSpawnCard);
    }

    public GameObject SpawnCard(Vector3 position, NetworkHash128 assetId)
    {
        GameObject newCardGO = Instantiate(cardModelPrefab, PlayAreaContent);
        RectTransform cardRT = newCardGO.transform as RectTransform;
        cardRT.anchorMin = Vector2.zero;
        cardRT.anchorMin = Vector2.zero;
        cardRT.anchorMax = Vector2.zero;
        cardRT.pivot = Vector2.zero;
        cardRT.position = position;
        SetPlayActions(PlayAreaContent.GetComponent<CardStack>(), newCardGO.GetComponent<CardModel>());
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


    public static CardSpawnManager Instance {
        get {
            return _instance;
        }
        private set {
            _instance = value;
        }
    }
}
