/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;

using CardGameDef;
using CardGameView;

namespace CGS.Play.Multiplayer
{
    public class CGSNetPlayer : NetworkBehaviour
    {
        public const string ShareDeckRequest = "Would you like to share the host's deck?";
        public const string ShareScoreRequest = "Also share score?";

        public int CurrentScore => CGSNetManager.Instance.Data.scores.Count > 0 ? CGSNetManager.Instance.Data.scores[scoreIndex].points : 0;
        public List<Card> CurrentDeck => CGSNetManager.Instance.Data.cardStacks.Count > 0 ?
            CGSNetManager.Instance.Data.cardStacks[deckIndex].cardIds.Select(cardId => CardGameManager.Current.Cards[cardId]).ToList() : new List<Card>();
        public string[] CurrentDeckCardIds => CGSNetManager.Instance.Data.cardStacks[deckIndex].cardIds;

        [SyncVar(hook = "OnChangeScore")]
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
            CGSNetManager.Instance.Data.RegisterScore(gameObject, CardGameManager.Current.GameStartPointsCount);
            TargetSelectCardGame(connectionToClient, CardGameManager.Current.Id);
        }

        [TargetRpc]
        public void TargetSelectCardGame(NetworkConnection target, string gameId)
        {
            CardGameManager.Instance.Select(gameId);
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

        public void OnChangeScore(int oldScoreIndex, int newScoreIndex)
        {
            //if (CGSNetManager.Instance.Data != null)
            //    CGSNetManager.Instance.pointsDisplay?.UpdateText();
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

        public void OnChangeDeck(int oldDeckIndex, int newDeckIndex)
        {
            if (this.deckIndex == newDeckIndex)
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
            while (CardGameManager.Current.IsDownloading)
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
            CGSNetManager.Instance.playController.LoadDeckCards(CurrentDeck, true);
            CardGameManager.Instance.Messenger.Ask(ShareScoreRequest, () => { }, RequestSharedScore);
        }

        public void RequestSharedScore()
        {
            CmdShareScore();
        }

        [Command]
        public void CmdShareScore()
        {
            scoreIndex = deckIndex;
        }

        public void MoveCardToServer(CardStack cardStack, CardModel cardModel)
        {
            cardModel.transform.SetParent(cardStack.transform);
            cardModel.position = ((RectTransform)cardModel.transform).anchoredPosition;
            cardModel.rotation = cardModel.transform.rotation;
            CmdSpawnCard(cardModel.Id, cardModel.position, cardModel.rotation, cardModel.IsFacedown);
            Destroy(cardModel.gameObject);
        }

        [Command]
        public void CmdSpawnCard(string cardId, Vector3 position, Quaternion rotation, bool isFacedown)
        {
            PlayMode controller = CGSNetManager.Instance.playController;
            GameObject newCard = Instantiate(CGSNetManager.Instance.cardModelPrefab, controller.playAreaContent);
            CardModel cardModel = newCard.GetComponent<CardModel>();
            cardModel.Value = CardGameManager.Current.Cards[cardId];
            cardModel.position = position;
            cardModel.rotation = rotation;
            cardModel.IsFacedown = isFacedown;
            controller.SetPlayActions(controller.playAreaContent.GetComponent<CardStack>(), cardModel);
            NetworkServer.Spawn(newCard, connectionToClient);
            cardModel.RpcHideHighlight();
        }

        void Update()
        {
            if (!isLocalPlayer || CGSNetManager.Instance.Data == null || CGSNetManager.Instance.Data.cardStacks == null || CGSNetManager.Instance.Data.cardStacks.Count < 1)
                return;

            IReadOnlyList<Card> localDeck = CGSNetManager.Instance.playController.zones.CurrentDeck?.Cards;
            if (localDeck == null)
                return;

            bool deckMatches = localDeck.Count == CurrentDeckCardIds.Length;
            for (int i = 0; deckMatches && i < localDeck.Count; i++)
                if (!localDeck[i].Id.Equals(CurrentDeckCardIds[i]))
                    deckMatches = false;

            if (!deckMatches)
                CmdUpdateDeck(localDeck.Select(card => card.Id).ToArray());
        }
    }
}
