using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class CGSNetPlayer : NetworkBehaviour
{
    public const string ShareDeckRequest = "Would you like to share the host's deck?";
    public const string ShareScoreRequest = "Also share score?";

    public int CurrentScore => CGSNetManager.Instance.Data.scoreboard.Count > 0 ? CGSNetManager.Instance.Data.scoreboard[scoreIndex].points : 0;
    public List<Card> CurrentDeck => CGSNetManager.Instance.Data.cardStacks.Count > 0 ?
        CGSNetManager.Instance.Data.cardStacks[deckIndex].cardIds.Select(cardId => CardGameManager.Current.Cards[cardId]).ToList() : new List<Card>();

    [SyncVar(hook ="OnChangeScore")]
    public int scoreIndex;
    [SyncVar(hook = "OnChangeDeck")]
    public int deckIndex;

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        CGSNetManager.Instance.LocalPlayer = this;
        if (!isServer)
            RequestCardGame();
    }

    public void RequestCardGame()
    {
        CmdSelectCardGame();
    }

    [Command]
    public void CmdSelectCardGame()
    {
        TargetSelectCardGame(connectionToClient, CardGameManager.Current.Name, CardGameManager.Current.AutoUpdateUrl);
    }

    [TargetRpc]
    public void TargetSelectCardGame(NetworkConnection target, string gameName, string gameUrl)
    {
        CardGameManager.Instance.SelectCardGame(gameName, gameUrl);
        CGSNetManager.Instance.Data.RegisterScore(gameObject, CardGameManager.Current.GameStartPointsCount);
        StartCoroutine(WaitToRequestDeck());
    }

    public void RequestScoreUpdate(int points)
    {
        CmdUpdateScore(points);
    }

    [Command]
    public void CmdUpdateScore(int points)
    {
        CGSNetManager.Instance.Data.ChangeScore(scoreIndex, points);
    }

    public void OnChangeScore(int scoreIndex)
    {
        CGSNetManager.Instance.pointsDisplay.UpdateText();
    }

    public void RequestDeckUpdate(List<Card> deckCards)
    {
        CmdUpdateDeck(deckCards.Select(card => card.Id).ToArray());
    }

    [Command]
    public void CmdUpdateDeck(string[] cardIds)
    {
        CGSNetManager.Instance.Data.ChangeDeck(deckIndex, cardIds);
    }

    public void OnChangeDeck(int deckIndex)
    {
        if (this.deckIndex == deckIndex)
            CGSNetManager.Instance.playController.zones.CurrentDeck.Sync(CurrentDeck);
    }

    public void RequestNewDeck(List<Card> deckCards)
    {
        CmdRegisterDeck(deckCards.Select(card => card.Id).ToArray());
    }

    [Command]
    public void CmdRegisterDeck(string[] cardIds)
    {
        CGSNetManager.Instance.Data.RegisterDeck(gameObject, cardIds);
    }

    public IEnumerator WaitToRequestDeck()
    {
        while(CardGameManager.Current.IsDownloading)
            yield return null;
        CardGameManager.Instance.Messenger.Ask(ShareDeckRequest, CGSNetManager.Instance.playController.ShowDeckMenu, RequestSharedDeck);
    }

    public void RequestSharedDeck()
    {
        CmdShareDeck();
    }

    [Command]
    public void CmdShareDeck()
    {
        TargetShareDeck(connectionToClient, CGSNetManager.Instance.LocalPlayer.deckIndex);
    }

    [TargetRpc]
    public void TargetShareDeck(NetworkConnection target, int deckIndex)
    {
        this.deckIndex = deckIndex;
        CGSNetManager.Instance.playController.LoadDeck(CurrentDeck, true);
        //CardGameManager.Instance.Messenger.Ask(ShareScoreRequest, () => {}, RequestSharedScore);
    }

    public void MoveCardToServer(CardStack cardStack, CardModel cardModel)
    {
        cardModel.transform.SetParent(cardStack.transform);
        cardModel.LocalPosition = cardModel.transform.localPosition;
        cardModel.Rotation = cardModel.transform.rotation;
        CmdSpawnCard(cardModel.Id, cardModel.LocalPosition, cardModel.Rotation, cardModel.IsFacedown);
        Destroy(cardModel.gameObject);
    }

    [Command]
    public void CmdSpawnCard(string cardId, Vector3 localPosition, Quaternion rotation, bool isFacedown)
    {
        PlayMode controller = CGSNetManager.Instance.playController;
        GameObject newCard = Instantiate(CGSNetManager.Instance.cardModelPrefab, controller.playAreaContent);
        CardModel cardModel = newCard.GetComponent<CardModel>();
        cardModel.Value = CardGameManager.Current.Cards[cardId];
        cardModel.transform.localPosition = localPosition;
        cardModel.LocalPosition = localPosition;
        cardModel.transform.rotation = rotation;
        cardModel.Rotation = rotation;
        cardModel.IsFacedown = isFacedown;
        controller.SetPlayActions(controller.playAreaContent.GetComponent<CardStack>(), cardModel);
        NetworkServer.SpawnWithClientAuthority(newCard, gameObject);
        cardModel.RpcHideHighlight();
    }
}
