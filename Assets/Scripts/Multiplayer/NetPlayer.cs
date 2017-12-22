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
        LocalNetManager netManager = ((LocalNetManager) NetworkManager.singleton);
        GameObject newCardGO = Instantiate(netManager.cardModelPrefab, position, Quaternion.identity, netManager.playAreaContent);
        CardModel cardModel = newCardGO.GetComponent<CardModel>();
        cardModel.Value = CardGameManager.Current.Cards[cardId];
        cardModel.transform.position = position;
        cardModel.LocalPosition = cardModel.transform.localPosition;
        cardModel.IsFacedown = isFacedown;
        netManager.SetPlayActions(netManager.playAreaContent.GetComponent<CardStack>(), cardModel);
        NetworkServer.SpawnWithClientAuthority(newCardGO, gameObject);
    }
}
