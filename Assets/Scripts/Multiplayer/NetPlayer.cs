using UnityEngine;
using UnityEngine.Networking;

public class DealCardMsg : MessageBase
{
    public NetworkInstanceId netId;
    public string cardId;
}

public class NetPlayer : NetworkBehaviour
{
    public const string DeckLoadPrompt = "Would you like to join the game with your own deck?";
    public const short DealCardMsgId = 1000;

    public override void OnStartLocalPlayer()
    {
        NetworkManager.singleton.client.RegisterHandler(DealCardMsgId, DealCard);
        ((LocalNetManager)NetworkManager.singleton).LocalPlayer = this;
        if (!isServer)
            CardGameManager.Instance.Messenger.Ask(DeckLoadPrompt, RequestHand, LocalNetManager.Instance.playController.ShowDeckMenu);
    }

    public void RequestHand()
    {
        CmdDealHand();
    }

    [Command]
    public void CmdDealHand()
    {
        StackedZone deckZone = LocalNetManager.Instance.playController.DeckZone;
        for (int i = 0; i < CardGameManager.Current.GameStartHandCount; i++) {
            Card card = deckZone?.PopCard() ?? Card.Blank;
            DealCardMsg dealCardMsg = new DealCardMsg {
                netId = netId,
                cardId = card.Id
            };
            connectionToClient.Send(DealCardMsgId, dealCardMsg);
        }
    }

    static void DealCard(NetworkMessage netMsg)
    {
        DealCardMsg dealCardMsg = netMsg.ReadMessage<DealCardMsg>();
        if (LocalNetManager.Instance.playController.HandZone == null) {
            LocalNetManager.Instance.playController.HandZone = Instantiate(LocalNetManager.Instance.playController.handZonePrefab, LocalNetManager.Instance.playController.zones.ActiveScrollView.content).GetComponent<ExtensibleCardZone>();
            LocalNetManager.Instance.playController.zones.AddZone(LocalNetManager.Instance.playController.HandZone);
            LocalNetManager.Instance.playController.zones.IsExtended = true;
            LocalNetManager.Instance.playController.zones.IsVisible = true;
        }
        LocalNetManager.Instance.playController.HandZone.AddCard(CardGameManager.Current.Cards[dealCardMsg.cardId]);
    }

    public void MoveCardToServer(CardStack cardStack, CardModel cardModel)
    {
        CmdSpawnCard(cardModel.Id, cardModel.LocalPosition, cardModel.Rotation, cardModel.IsFacedown);
        Destroy(cardModel.gameObject);
    }

    [Command]
    public void CmdSpawnCard(string cardId, Vector3 localPosition, Quaternion rotation, bool isFacedown)
    {
        GameObject newCardGO = Instantiate(LocalNetManager.Instance.cardModelPrefab, LocalNetManager.Instance.playController.playAreaContent);
        CardModel cardModel = newCardGO.GetComponent<CardModel>();
        cardModel.Value = CardGameManager.Current.Cards[cardId];
        cardModel.transform.localPosition = localPosition;
        cardModel.LocalPosition = localPosition;
        cardModel.transform.rotation = rotation;
        cardModel.Rotation = rotation;
        cardModel.IsFacedown = isFacedown;
        LocalNetManager.Instance.SetPlayActions(LocalNetManager.Instance.playController.playAreaContent.GetComponent<CardStack>(), cardModel);
        NetworkServer.SpawnWithClientAuthority(newCardGO, gameObject);
        cardModel.RpcHideHighlight();
    }
}
