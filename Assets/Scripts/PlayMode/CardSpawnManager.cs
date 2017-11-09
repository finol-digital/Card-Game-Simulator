using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class CardSpawnManager : NetworkBehaviour
{
    public GameObject cardModelPrefab;

    public RectTransform PlayAreaContent { get; set; }

    public NetworkHash128 CardAssetId { get; set; }

    public delegate GameObject SpawnDelegate(Vector3 position,NetworkHash128 assetId);

    public delegate void UnSpawnDelegate(GameObject spawned);

    private static CardSpawnManager _instance;

    void Awake()
    {
        CardSpawnManager.Instance = this;
    }

    void Start()
    {
        CardAssetId = cardModelPrefab.GetComponent<NetworkIdentity>().assetId;
        ClientScene.RegisterSpawnHandler(CardAssetId, SpawnCard, UnSpawnCard);
    }

    public void MoveCardToServer(CardModel cardModel)
    {
        CmdSpawnCard(cardModel.Value.Id, cardModel.transform.position, cardModel.IsFacedown);
        Destroy(cardModel.gameObject);
    }

    [Command]
    public void CmdSpawnCard(string cardId, Vector3 position, bool isFacedown)
    {
        CardModel newCardModel = Instantiate(cardModelPrefab, PlayAreaContent).GetOrAddComponent<CardModel>();
        newCardModel.Value = CardGameManager.Current.Cards [cardId];
        newCardModel.transform.position = position;
        newCardModel.IsFacedown = isFacedown;
        SetPlayActions(PlayAreaContent.GetComponent<CardStack>(), newCardModel);
        NetworkServer.SpawnWithClientAuthority(newCardModel.gameObject, this.gameObject);
    }

    public GameObject SpawnCard(Vector3 position, NetworkHash128 assetId)
    {
        GameObject newCardGO = Instantiate(cardModelPrefab, PlayAreaContent);
        newCardGO.transform.position = position;
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
