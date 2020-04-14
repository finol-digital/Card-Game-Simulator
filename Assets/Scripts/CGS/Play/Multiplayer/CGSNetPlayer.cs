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

        public List<Card> CurrentDeck => CGSNetManager.Instance.Data.cardStacks.Count > 0 ?
            CGSNetManager.Instance.Data.cardStacks[deckIndex].cardIds.Select(cardId => CardGameManager.Current.Cards[cardId]).ToList() : new List<Card>();
        public string[] CurrentDeckCardIds => CGSNetManager.Instance.Data.cardStacks[deckIndex].cardIds;

        public int CurrentScore => CGSNetManager.Instance.Data.scores.Count > 0 ? CGSNetManager.Instance.Data.scores[scoreIndex].points : 0;

        [SyncVar(hook = "OnChangeDeck")]
        public int deckIndex;

        [SyncVar(hook = "OnChangeScore")]
        public int scoreIndex;

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            Debug.Log("CGSNetPlayer OnStartLocalPlayer...");
            CGSNetManager.Instance.LocalPlayer = this;
            if (isServer)
                CGSNetManager.Instance.playController.ShowDeckMenu();
            else
                RequestCardGame();
            Debug.Log("CGSNetPlayer OnStartLocalPlayer!");
        }

        public void RequestCardGame()
        {
            CGSNetManager.Instance.statusText.text = "Determining game id...";
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
            CGSNetManager.Instance.statusText.text = $"Game id is {gameId}! Loading game details...";
            CardGameManager.Instance.Select(gameId);
            StartCoroutine(WaitToStartGame());
        }

        public IEnumerator WaitToStartGame()
        {
            while (CardGameManager.Current.IsDownloading)
                yield return null;

            CGSNetManager.Instance.statusText.text = "Game loaded and ready!";

            switch (CardGameManager.Current.DeckSharePreference)
            {
                case SharePreference.Share:
                    RequestSharedDeck();
                    break;
                case SharePreference.Individual:
                    CGSNetManager.Instance.playController.ShowDeckMenu();
                    break;
                case SharePreference.Ask:
                default:
                    CardGameManager.Instance.Messenger.Ask(ShareDeckRequest, CGSNetManager.Instance.playController.ShowDeckMenu, RequestSharedDeck);
                    break;
            }
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

        public void RequestSharedDeck()
        {
            CGSNetManager.Instance.statusText.text = "Getting deck from server...";
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
            CGSNetManager.Instance.statusText.text = "Got deck from server!";
            this.deckIndex = deckIndex;
            CGSNetManager.Instance.playController.LoadDeckCards(CurrentDeck, true);
            // TODO: CardGameManager.Instance.Messenger.Ask(ShareScoreRequest, () => { }, RequestSharedScore);
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
            // TODO: if (CGSNetManager.Instance.Data != null)
            //    CGSNetManager.Instance.pointsDisplay?.UpdateText();
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
