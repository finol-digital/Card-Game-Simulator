/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CardGameDef;
using CardGameDef.Unity;
using CardGameView;
using JetBrains.Annotations;
using Mirror;
using UnityEngine;

namespace Cgs.Play.Multiplayer
{
    public class CgsNetPlayer : NetworkBehaviour
    {
        public const string GameSelectionErrorMessage = "The host has selected a game that is not available!";
        public const string ShareDeckRequest = "Would you like to share the host's deck?";

        public int CurrentScore => CgsNetManager.Instance.Data != null && CgsNetManager.Instance.Data.Scores != null
                                                                       && CgsNetManager.Instance.Data.Scores.Count > 0
            ? CgsNetManager.Instance.Data.Scores[scoreIndex].Points
            : 0;

        [SyncVar(hook = "OnChangeScore")] public int scoreIndex;

        [field: SyncVar] public GameObject DeckZone { get; private set; }
        [field: SyncVar] public bool IsDeckShared { get; private set; }

        #region StartGame

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
            Debug.Log("[CgsNet Player] Requesting game id...");
            CmdSelectCardGame();
        }

        [Command]
        private void CmdSelectCardGame()
        {
            Debug.Log("[CgsNet Player] Sending game id...");
            CgsNetManager.Instance.Data.RegisterScore(gameObject, CardGameManager.Current.GameStartPointsCount);
            TargetSelectCardGame(CardGameManager.Current.Id, CardGameManager.Current.AutoUpdateUrl?.OriginalString);
        }

        [TargetRpc]
        private void TargetSelectCardGame(string gameId, string autoUpdateUrl)
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

        #endregion

        #region Scores

        public void RequestScoreUpdate(int points)
        {
            CmdUpdateScore(points);
        }

        [Command]
        private void CmdUpdateScore(int points)
        {
            CgsNetManager.Instance.Data.ChangeScore(scoreIndex, points);
        }

        [PublicAPI]
        public void OnChangeScore(int oldScoreIndex, int newScoreIndex)
        {
            if (scoreIndex == newScoreIndex)
                CgsNetManager.Instance.playController.scoreboard.CurrentDisplayValue = CurrentScore;
        }

        #endregion

        #region Zones

        public void RequestNewDeck(string zoneName, IEnumerable<UnityCard> cards)
        {
            CmdCreateZone(zoneName, cards.Select(card => card.Id).ToArray(), true);
        }

        public void RequestNewZone(string zoneName, IEnumerable<UnityCard> cards)
        {
            CmdCreateZone(zoneName, cards.Select(card => card.Id).ToArray(), false);
        }

        [Command]
        // ReSharper disable once ParameterTypeCanBeEnumerable.Local
        private void CmdCreateZone(string zoneName, string[] cardIds, bool isDeck)
        {
            CardStack stack = CgsNetManager.Instance.playController.CreateZone(Vector2.zero); // TODO: DYNAMIC LOCATION
            stack.Name = zoneName;
            stack.Cards = cardIds.Select(cardId => CardGameManager.Current.Cards[cardId]).ToList();
            GameObject zoneGameObject = stack.gameObject;
            NetworkServer.Spawn(zoneGameObject);
            if (isDeck)
                DeckZone = zoneGameObject;
        }

        private void RequestSharedDeck()
        {
            CmdShareDeck();
        }

        [Command]
        private void CmdShareDeck()
        {
            TargetShareDeck(CgsNetManager.Instance.LocalPlayer.DeckZone);
        }

        [TargetRpc]
        private void TargetShareDeck(GameObject deckZone)
        {
            DeckZone = deckZone;
            IsDeckShared = true;
        }

        public void RequestShuffle(GameObject toShuffle)
        {
            CmdShuffle(toShuffle);
        }

        [Command]
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void CmdShuffle(GameObject toShuffle)
        {
            var cardZone = toShuffle.GetComponent<CardStack>();
            cardZone.DoShuffle();
        }

        #endregion

        #region Cards

        public void MoveCardToServer(CardZone cardZone, CardModel cardModel)
        {
            Transform cardModelTransform = cardModel.transform;
            cardModelTransform.SetParent(cardZone.transform);
            cardModel.position = ((RectTransform) cardModelTransform).anchoredPosition;
            cardModel.rotation = cardModelTransform.rotation;
            CmdSpawnCard(cardModel.Id, cardModel.position, cardModel.rotation, cardModel.IsFacedown);
            Destroy(cardModel.gameObject);
        }

        [Command]
        private void CmdSpawnCard(string cardId, Vector3 position, Quaternion rotation, bool isFacedown)
        {
            PlayController controller = CgsNetManager.Instance.playController;
            GameObject newCard = Instantiate(controller.cardModelPrefab, controller.playArea.transform);
            var cardModel = newCard.GetComponent<CardModel>();
            cardModel.Value = CardGameManager.Current.Cards[cardId];
            cardModel.position = position;
            cardModel.rotation = rotation;
            cardModel.IsFacedown = isFacedown;
            PlayController.SetPlayActions(cardModel);
            NetworkServer.Spawn(newCard, connectionToClient);
            cardModel.RpcHideHighlight();
        }

        #endregion

        #region Dice

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

        #endregion

        #region RestartGame

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
        public void TargetRestart()
        {
            Debug.Log("[CgsNet Player] Game is restarting!...");
            CgsNetManager.Instance.playController.ResetPlayArea();
            CgsNetManager.Instance.playController.hand.Clear();
            DeckZone = null;
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

        #endregion

        public void RequestDelete(GameObject toDelete)
        {
            CmdDelete(toDelete);
        }

        [Command]
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void CmdDelete(GameObject toDelete)
        {
            NetworkServer.UnSpawn(toDelete);
            Destroy(toDelete);
        }
    }
}
