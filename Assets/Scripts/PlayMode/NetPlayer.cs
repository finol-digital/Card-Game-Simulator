using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NetPlayer : NetworkBehaviour
{
    void Start()
    {
        if (this.isLocalPlayer)
            CardSpawnManager.Instance.LocalPlayer = this;
    }

    public void MoveCardToServer(CardModel cardModel)
    {
        Vector3 position = cardModel.transform.position;
        RectTransform cardRT = cardModel.transform as RectTransform;
        cardRT.anchorMin = Vector2.zero;
        cardRT.anchorMax = Vector2.zero;
        cardRT.pivot = Vector2.zero;
        cardRT.position = position;
        CmdSpawnCard(cardModel.Value.Id, cardModel.transform.position, cardModel.IsFacedown);
        Destroy(cardModel.gameObject);
    }

    [Command]
    public void CmdSpawnCard(string cardId, Vector3 position, bool isFacedown)
    {
        CardModel newCardModel = CardSpawnManager.Instance.SpawnCard(position, CardSpawnManager.CardModelAssetId).GetOrAddComponent<CardModel>();
        newCardModel.Value = CardGameManager.Current.Cards [cardId];
        newCardModel.IsFacedown = isFacedown;
        NetworkServer.SpawnWithClientAuthority(newCardModel.gameObject, this.gameObject);
    }
}
