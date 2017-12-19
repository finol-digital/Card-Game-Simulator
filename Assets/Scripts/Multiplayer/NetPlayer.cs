using UnityEngine;
using UnityEngine.Networking;

public class NetPlayer : NetworkBehaviour
{
    void Start()
    {
        if (isLocalPlayer)
            ((LocalNetManager)NetworkManager.singleton).LocalPlayer = this;
    }

    public void MoveCardToServer(CardModel cardModel)
    {
        CmdSpawnCard(cardModel.Value.Id, cardModel.transform.position, cardModel.IsFacedown);
        Destroy(cardModel.gameObject);
    }

    [Command]
    public void CmdSpawnCard(string cardId, Vector3 position, bool isFacedown)
    {
        CardModel newCardModel = ((LocalNetManager)NetworkManager.singleton).SpawnCard(position, LocalNetManager.CardModelAssetId).GetOrAddComponent<CardModel>();
        newCardModel.Value = CardGameManager.Current.Cards [cardId];
        newCardModel.IsFacedown = isFacedown;
        NetworkServer.SpawnWithClientAuthority(newCardModel.gameObject, gameObject);
    }
}
