/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CardGameDef;
using CardGameView;
using Mirror;
using UnityEngine;

namespace Cgs.Play.Multiplayer
{
    public class CgsNetPlayer : NetworkBehaviour
    {
        public const string GameSelectionErrorMessage = "The host has selected a game that is not available!";
        public const string ShareDeckRequest = "Would you like to share the host's deck?";

        private IEnumerable<Card> CurrentDeck =>
            CurrentDeckCardIds.Select(cardId => CardGameManager.Current.Cards[cardId]);

        public string[] CurrentDeckCardIds =>
            CgsNetManager.Instance.Data != null && CgsNetManager.Instance.Data.cardStacks != null
                                                && CgsNetManager.Instance.Data.cardStacks.Count > 0
                ? CgsNetManager.Instance.Data.cardStacks[deckIndex].CardIds
                : new string[] { };

        public int CurrentScore => CgsNetManager.Instance.Data != null && CgsNetManager.Instance.Data.scores != null
                                                                       && CgsNetManager.Instance.Data.scores.Count > 0
            ? CgsNetManager.Instance.Data.scores[scoreIndex].Points
            : 0;

        public bool IsDeckShared { get; private set; }
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
            Debug.Log("[CgsNet Player] Determining game id...");
            CmdSelectCardGame();
        }

        [Command]
        private void CmdSelectCardGame()
        {
            CgsNetManager.Instance.Data.RegisterScore(gameObject, CardGameManager.Current.GameStartPointsCount);
            TargetSelectCardGame(connectionToClient, CardGameManager.Current.Id,
                CardGameManager.Current.AutoUpdateUrl?.OriginalString);
        }

        [TargetRpc]
        // ReSharper disable once UnusedParameter.Local
        private void TargetSelectCardGame(NetworkConnection target, string gameId, string autoUpdateUrl)
        {
            Debug.Log($"[CgsNet Player] Game id is {gameId}! Loading game details...");
            if (!CardGameManager.Instance.AllCardGames.ContainsKey(gameId))
            {
                if (!Uri.IsWellFormedUriString(autoUpdateUrl, UriKind.Absolute))
                {
                    Debug.LogError(GameSelectionErrorMessage);
                    CardGameManager.Instance.Messenger.Show();
                    return;
                }

                StartCoroutine(DownloadGame(autoUpdateUrl));
            }
            else
            {
                CardGameManager.Instance.Select(gameId);
                StartCoroutine(WaitToStartGame());
            }
        }

        private IEnumerator DownloadGame(string url)
        {
            yield return CardGameManager.Instance.GetCardGame(url);
            yield return WaitToStartGame();
        }

        private IEnumerator WaitToStartGame()
        {
            while (CardGameManager.Current.IsDownloading)
                yield return null;

            Debug.Log("[CgsNet Player] Game loaded and ready!");

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
            Debug.Log("[CgsNet Player] Getting deck from server...");
            CmdShareDeck();
        }

        [Command]
        private void CmdShareDeck()
        {
            TargetShareDeck(connectionToClient, CgsNetManager.Instance.LocalPlayer.deckIndex);
        }

        [TargetRpc]
        // ReSharper disable once UnusedParameter.Local
        private void TargetShareDeck(NetworkConnection target, int sharedDeckIndex)
        {
            Debug.Log("[CgsNet Player] Got deck from server!");
            IsDeckShared = true;
            deckIndex = sharedDeckIndex;
            CgsNetManager.Instance.playController.LoadDeckCards(CurrentDeck, true);
        }

        public void RequestDeckUpdate(IEnumerable<Card> deckCards)
        {
            CmdUpdateDeck(deckCards.Select(card => card.Id).ToArray());
        }

        [Command]
        private void CmdUpdateDeck(string[] cardIds)
        {
            CgsNetManager.Instance.Data.ChangeDeck(deckIndex, cardIds);
        }

        // ReSharper disable once UnusedParameter.Global
        public void OnChangeDeck(int oldDeckIndex, int newDeckIndex)
        {
            // TODO: if (deckIndex == newDeckIndex)
            // TODO:    CgsNetManager.Instance.playController.zones.CurrentDeck.Sync(CurrentDeck);
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

        // ReSharper disable once UnusedParameter.Global
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
            PlayController controller = CgsNetManager.Instance.playController;
            GameObject newCard = Instantiate(controller.cardModelPrefab, controller.playAreaCardStack.transform);
            var cardModel = newCard.GetComponent<CardModel>();
            cardModel.Value = CardGameManager.Current.Cards[cardId];
            cardModel.position = position;
            cardModel.rotation = rotation;
            cardModel.IsFacedown = isFacedown;
            PlayController.SetPlayActions(cardModel);
            NetworkServer.Spawn(newCard, connectionToClient);
            cardModel.RpcHideHighlight();
        }

        public void RequestNewDie(int min, int max)
        {
            CmdCreateDie(min, max);
        }

        [Command]
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void CmdCreateDie(int min, int max)
        {
            Die die = CgsNetManager.Instance.playController.CreateDie(min, max);
            NetworkServer.Spawn(die.gameObject, connectionToClient);
        }

        public void RequestRestart()
        {
            CmdRestart();
        }

        [Command]
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void CmdRestart()
        {
            CgsNetManager.Instance.Restart();
        }

        [TargetRpc]
        // ReSharper disable once UnusedParameter.Global
        public void TargetRestart(NetworkConnection target)
        {
            Debug.Log("[CgsNet Player] Game is restarting!...");
            CgsNetManager.Instance.playController.ResetPlayArea();
            StartCoroutine(WaitToRestartGame());
        }

        private IEnumerator WaitToRestartGame()
        {
            if (isServer || CardGameManager.Current.DeckSharePreference == SharePreference.Individual)
            {
                CgsNetManager.Instance.playController.ShowDeckMenu();
                Debug.Log("[CgsNet Player] Game restarted!");
                yield break;
            }

            while (!CurrentDeck.Any())
                yield return null;

            Debug.Log("[CgsNet Player] Game restarted!");

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
    }
}
