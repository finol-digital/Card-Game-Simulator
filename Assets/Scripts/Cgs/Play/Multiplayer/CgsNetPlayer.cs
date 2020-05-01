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

// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedParameter.Global

namespace Cgs.Play.Multiplayer
{
    public class CgsNetPlayer : NetworkBehaviour
    {
        public const string ShareDeckRequest = "Would you like to share the host's deck?";

        public List<Card> CurrentDeck =>
            CgsNetManager.Instance.Data != null && CgsNetManager.Instance.Data.scores != null
                                                && CgsNetManager.Instance.Data.cardStacks.Count > 0
                ? CgsNetManager.Instance.Data.cardStacks[deckIndex].CardIds
                    .Select(cardId => CardGameManager.Current.Cards[cardId]).ToList()
                : new List<Card>();

        public string[] CurrentDeckCardIds => CgsNetManager.Instance.Data.cardStacks[deckIndex].CardIds;

        public int CurrentScore => CgsNetManager.Instance.Data != null && CgsNetManager.Instance.Data.scores != null
                                                                       && CgsNetManager.Instance.Data.scores.Count > 0
            ? CgsNetManager.Instance.Data.scores[scoreIndex].Points
            : 0;

        [SyncVar(hook = "OnChangeDeck")] public int deckIndex;

        [SyncVar(hook = "OnChangeScore")] public int scoreIndex;

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            Debug.Log("[CgsNet Player] Starting local player...");
            CgsNetManager.Instance.LocalPlayer = this;
            if (isServer)
                CgsNetManager.Instance.playController.ShowDeckMenu();
            else
                RequestCardGameSelection();
            Debug.Log("[CgsNet Player] Started local player!");
        }

        private void RequestCardGameSelection()
        {
            CgsNetManager.Instance.statusText.text = "Determining game id...";
            CmdSelectCardGame();
        }

        [Command]
        private void CmdSelectCardGame()
        {
            CgsNetManager.Instance.Data.RegisterScore(gameObject, CardGameManager.Current.GameStartPointsCount);
            TargetSelectCardGame(connectionToClient, CardGameManager.Current.Id);
        }

        [TargetRpc]
        private void TargetSelectCardGame(NetworkConnection target, string gameId)
        {
            CgsNetManager.Instance.statusText.text = $"Game id is {gameId}! Loading game details...";
            CardGameManager.Instance.Select(gameId);
            StartCoroutine(WaitToStartGame());
        }

        private IEnumerator WaitToStartGame()
        {
            while (CardGameManager.Current.IsDownloading)
                yield return null;

            CgsNetManager.Instance.statusText.text = "Game loaded and ready!";

            switch (CardGameManager.Current.DeckSharePreference)
            {
                case SharePreference.Individual:
                    CgsNetManager.Instance.playController.ShowDeckMenu();
                    break;
                case SharePreference.Share:
                    RequestSharedDeck();
                    break;
                case SharePreference.Ask:
                default:
                    CardGameManager.Instance.Messenger.Ask(ShareDeckRequest,
                        CgsNetManager.Instance.playController.ShowDeckMenu, RequestSharedDeck);
                    break;
            }
        }

        public void RequestNewDeck(IEnumerable<Card> deckCards)
        {
            CmdRegisterDeck(deckCards.Select(card => card.Id).ToArray());
        }

        [Command]
        private void CmdRegisterDeck(string[] cardIds)
        {
            CgsNetManager.Instance.Data.RegisterDeck(gameObject, cardIds);
        }

        private void RequestSharedDeck()
        {
            CgsNetManager.Instance.statusText.text = "Getting deck from server...";
            CmdShareDeck();
        }

        [Command]
        private void CmdShareDeck()
        {
            TargetShareDeck(connectionToClient, CgsNetManager.Instance.LocalPlayer.deckIndex);
        }

        [TargetRpc]
        private void TargetShareDeck(NetworkConnection target, int sharedDeckIndex)
        {
            CgsNetManager.Instance.statusText.text = "Got deck from server!";
            deckIndex = sharedDeckIndex;
            CgsNetManager.Instance.playController.LoadDeckCards(CurrentDeck, true);
        }

        private void RequestDeckUpdate(IEnumerable<Card> deckCards)
        {
            CmdUpdateDeck(deckCards.Select(card => card.Id).ToArray());
        }

        [Command]
        private void CmdUpdateDeck(string[] cardIds)
        {
            CgsNetManager.Instance.Data.ChangeDeck(deckIndex, cardIds);
        }

        public void OnChangeDeck(int oldDeckIndex, int newDeckIndex)
        {
            if (deckIndex == newDeckIndex)
                CgsNetManager.Instance.playController.zones.CurrentDeck.Sync(CurrentDeck);
        }

        public void RequestScoreUpdate(int points)
        {
            CmdUpdateScore(points);
        }

        [Command]
        private void CmdUpdateScore(int points)
        {
            CgsNetManager.Instance.Data.ChangeScore(scoreIndex, points);
        }

        public void OnChangeScore(int oldScoreIndex, int newScoreIndex)
        {
            if (scoreIndex == newScoreIndex)
                CgsNetManager.Instance.playController.scoreboard.CurrentDisplayValue = CurrentScore;
        }

        public void MoveCardToServer(CardStack cardStack, CardModel cardModel)
        {
            Transform cardModelTransform = cardModel.transform;
            cardModelTransform.SetParent(cardStack.transform);
            cardModel.position = ((RectTransform) cardModelTransform).anchoredPosition;
            cardModel.rotation = cardModelTransform.rotation;
            CmdSpawnCard(cardModel.Id, cardModel.position, cardModel.rotation, cardModel.IsFacedown);
            Destroy(cardModel.gameObject);
        }

        [Command]
        private void CmdSpawnCard(string cardId, Vector3 position, Quaternion rotation, bool isFacedown)
        {
            PlayMode controller = CgsNetManager.Instance.playController;
            GameObject newCard = Instantiate(CgsNetManager.Instance.cardModelPrefab, controller.playMatContent);
            var cardModel = newCard.GetComponent<CardModel>();
            cardModel.Value = CardGameManager.Current.Cards[cardId];
            cardModel.position = position;
            cardModel.rotation = rotation;
            cardModel.IsFacedown = isFacedown;
            controller.SetPlayActions(controller.playMatContent.GetComponent<CardStack>(), cardModel);
            NetworkServer.Spawn(newCard, connectionToClient);
            cardModel.RpcHideHighlight();
        }

        void Update()
        {
            if (!isLocalPlayer || CgsNetManager.Instance.Data == null ||
                CgsNetManager.Instance.Data.cardStacks == null || CgsNetManager.Instance.Data.cardStacks.Count < 1 ||
                CgsNetManager.Instance.playController.zones.CurrentDeck == null)
                return;

            IReadOnlyList<Card> localDeck = CgsNetManager.Instance.playController.zones.CurrentDeck.Cards;
            if (localDeck == null)
                return;

            bool deckMatches = localDeck.Count == CurrentDeckCardIds.Length;
            for (var i = 0; deckMatches && i < localDeck.Count; i++)
                if (!localDeck[i].Id.Equals(CurrentDeckCardIds[i]))
                    deckMatches = false;

            if (!deckMatches)
                RequestDeckUpdate(localDeck);
        }
    }
}
