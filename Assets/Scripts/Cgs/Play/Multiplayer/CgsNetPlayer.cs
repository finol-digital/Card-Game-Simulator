/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CardGameDef;
using CardGameDef.Unity;
using Cgs.CardGameView;
using Cgs.CardGameView.Multiplayer;
using Cgs.Play.Drawer;
using Mirror;
using UnityEngine;

namespace Cgs.Play.Multiplayer
{
    public class CgsNetPlayer : NetworkBehaviour
    {
        public const string GameSelectionErrorMessage = "The host has selected a game that is not available!";
        public const string ShareDeckRequest = "Would you like to share the host's deck?";

        [field: SyncVar] public string Name { get; private set; }
        [field: SyncVar] public int Points { get; private set; }

        [field: SyncVar] public GameObject CurrentDeck { get; private set; }
        [field: SyncVar] public bool IsDeckShared { get; private set; }

        [field: SyncVar] public int CurrentHand { get; private set; }

        public int DefaultRotation { get; private set; }

        public string GetHandCount()
        {
            var handCards = GetHandCards();
            return handCards.Count > 0 && CurrentHand >= 0 && CurrentHand < handCards.Count
                ? handCards[CurrentHand].Count.ToString()
                : string.Empty;
        }

        public IReadOnlyList<IReadOnlyList<UnityCard>> GetHandCards()
        {
            return _handCards.Select(hand =>
                    hand.Select(cardId => CardGameManager.Current.Cards[cardId]).ToList())
                .Cast<IReadOnlyList<UnityCard>>().ToList();
        }

        private readonly SyncList<string[]> _handCards = new();

        public IReadOnlyList<string> GetHandNames()
        {
            return _handNames.Select(handName => handName).ToList();
        }

        private readonly SyncList<string> _handNames = new();

        public CardModel RemovedCard { get; set; }

        #region StartGame

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            Debug.Log("[CgsNet Player] Starting local player...");
            CgsNetManager.Instance.LocalPlayer = this;
            RequestNameUpdate(PlayerPrefs.GetString(Scoreboard.PlayerNamePlayerPrefs, Scoreboard.DefaultPlayerName));
            RequestNewHand(CardDrawer.DefaultHandName);
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

            CmdApplyPlayerRotation();
        }

        [Command]
        private void CmdApplyPlayerRotation()
        {
            TargetApplyPlayerRotation(CgsNetManager.ActiveConnectionCount);
        }

        [TargetRpc]
        private void TargetApplyPlayerRotation(int playerCount)
        {
            if (playerCount % 4 == 0)
                DefaultRotation = 270;
            else if (playerCount % 3 == 0)
                DefaultRotation = 90;
            else if (playerCount % 2 == 0)
                DefaultRotation = 180;
            else
                DefaultRotation = 0;
            Debug.Log("[CgsNet Player] Set PlayMat rotation based off player count: " + DefaultRotation);
            CgsNetManager.Instance.playController.playArea.CurrentRotation = DefaultRotation;
        }

        private IEnumerator DownloadGame(string url)
        {
            Debug.Log($"[CgsNet Player] Downloading game from {url}...");
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

            RequestNewHand(CardDrawer.DefaultHandName);
        }

        #endregion

        #region Score

        public void RequestNameUpdate(string playerName)
        {
            CmdUpdateName(playerName);
        }

        [Command]
        private void CmdUpdateName(string playerName)
        {
            Name = playerName;
        }

        public void RequestPointsUpdate(int points)
        {
            CmdUpdatePoints(points);
        }

        [Command]
        private void CmdUpdatePoints(int points)
        {
            Points = points;
        }

        #endregion

        #region CardStacks

        public void RequestNewDeck(string deckName, IEnumerable<UnityCard> cards)
        {
            Debug.Log($"[CgsNet Player] Requesting new deck {deckName}...");
            CmdCreateCardStack(deckName, cards.Select(card => card.Id).ToArray(), true,
                CgsNetManager.Instance.playController.NewDeckPosition);
        }

        public void RequestNewCardStack(string stackName, IEnumerable<UnityCard> cards, Vector2 position)
        {
            Debug.Log($"[CgsNet Player] Requesting new card stack {stackName}...");
            CmdCreateCardStack(stackName, cards.Select(card => card.Id).ToArray(), false, position);
        }

        [Command]
        // ReSharper disable once ParameterTypeCanBeEnumerable.Local
        private void CmdCreateCardStack(string stackName, string[] cardIds, bool isDeck, Vector2 position)
        {
            Debug.Log($"[CgsNet Player] Creating new card stack {stackName}...");
            var cardStack = CgsNetManager.Instance.playController.CreateCardStack(stackName,
                cardIds.Select(cardId => CardGameManager.Current.Cards[cardId]).ToList(), position);
            var stackGameObject = cardStack.gameObject;
            NetworkServer.Spawn(stackGameObject);
            if (isDeck)
                CurrentDeck = stackGameObject;
            Debug.Log($"[CgsNet Player] Created new card stack {stackName}!");
        }

        private void RequestSharedDeck()
        {
            Debug.Log("[CgsNet Player] Requesting shared deck..");
            CmdShareDeck();
        }

        [Command]
        private void CmdShareDeck()
        {
            Debug.Log("[CgsNet Player] Sending shared deck...");
            TargetShareDeck(CgsNetManager.Instance.LocalPlayer.CurrentDeck);
        }

        [TargetRpc]
        private void TargetShareDeck(GameObject deckStack)
        {
            Debug.Log("[CgsNet Player] Received shared deck!");
            CurrentDeck = deckStack;
            IsDeckShared = true;
            CgsNetManager.Instance.playController.PromptForHand();
        }

        public void RequestShuffle(GameObject toShuffle)
        {
            Debug.Log("[CgsNet Player] Requesting shuffle...");
            CmdShuffle(toShuffle);
        }

        [Command]
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void CmdShuffle(GameObject toShuffle)
        {
            Debug.Log("[CgsNet Player] Shuffling!");
            var cardStack = toShuffle.GetComponent<CardStack>();
            cardStack.DoShuffle();
        }

        public void RequestInsert(GameObject stack, int index, string cardId)
        {
            Debug.Log($"[CgsNet Player] Requesting insert {cardId} at {index}...");
            CmdInsert(stack, index, cardId);
        }

        [Command]
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void CmdInsert(GameObject stack, int index, string cardId)
        {
            Debug.Log($"[CgsNet Player] Insert {cardId} at {index}!");
            var cardStack = stack.GetComponent<CardStack>();
            cardStack.Insert(index, cardId);
        }

        public void RequestRemoveAt(GameObject stack, int index)
        {
            Debug.Log($"[CgsNet Player] Requesting remove at {index}...");
            CmdRemoveAt(stack, index);
        }

        [Command]
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void CmdRemoveAt(GameObject stack, int index)
        {
            Debug.Log($"[CgsNet Player] Remove at {index}!");
            var cardStack = stack.GetComponent<CardStack>();
            var removedCardId = cardStack.RemoveAt(index);
            TargetSyncRemovedCard(removedCardId);
        }

        [TargetRpc]
        private void TargetSyncRemovedCard(string removedCardId)
        {
            if (RemovedCard != null)
                RemovedCard.Value = CardGameManager.Current.Cards[removedCardId];
            RemovedCard = null;
        }

        public void RequestDeal(GameObject stack, int count)
        {
            Debug.Log($"[CgsNet Player] Requesting deal {count}...");
            CmdDeal(stack, count);
        }

        [Command]
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void CmdDeal(GameObject stack, int count)
        {
            Debug.Log($"[CgsNet Player] Dealing {count}!");
            var cardStack = stack.GetComponent<CardStack>();
            var cardIds = new string[count];
            for (var i = 0; i < count && cardStack.Cards.Count > 0; i++)
                cardIds[i] = cardStack.PopCard();
            TargetDeal(cardIds);
        }

        [TargetRpc]
        // ReSharper disable once ParameterTypeCanBeEnumerable.Local
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void TargetDeal(string[] cardIds)
        {
            Debug.Log($"[CgsNet Player] Dealt {cardIds}!");
            CgsNetManager.Instance.playController.AddCardsToHand(
                cardIds.Where(cardId => !string.IsNullOrEmpty(cardId) && !UnityCard.Blank.Id.Equals(cardId))
                    .Select(cardId => CardGameManager.Current.Cards[cardId]));
        }

        #endregion

        #region Hands

        public void RequestNewHand(string handName)
        {
            Debug.Log($"[CgsNet Player] Requesting new hand {handName}...");
            CmdAddHand(handName);
        }

        [Command]
        private void CmdAddHand(string handName)
        {
            Debug.Log($"[CgsNet Player] Add hand {handName}!");
            _handCards.Add(Array.Empty<string>());
            _handNames.Add(handName);
            CurrentHand = _handNames.Count - 1;
        }

        public void RequestUseHand(int handIndex)
        {
            Debug.Log($"[CgsNet Player] Requesting use hand {handIndex}...");
            CmdUseHand(handIndex);
        }

        [Command]
        private void CmdUseHand(int handIndex)
        {
            Debug.Log($"[CgsNet Player] Use hand {handIndex}!");
            CurrentHand = handIndex;
        }

        public void RequestSyncHand(int handIndex, string[] cardIds)
        {
            Debug.Log($"[CgsNet Player] Requesting sync hand {handIndex} to {cardIds.Length}...");
            CmdSyncHand(handIndex, cardIds);
        }

        [Command]
        private void CmdSyncHand(int handIndex, string[] cardIds)
        {
            Debug.Log($"[CgsNet Player] Sync hand {handIndex} to {cardIds.Length} cards on Server!");
            if (handIndex < 0 || handIndex >= _handCards.Count)
            {
                Debug.LogError($"[CgsNet Player] {handIndex} is out of bounds of {_handCards.Count}");
                return;
            }

            _handCards[handIndex] = cardIds;
            TargetSyncHand(handIndex, cardIds);
        }

        [TargetRpc]
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void TargetSyncHand(int handIndex, string[] cardIds)
        {
            Debug.Log($"[CgsNet Player] Sync hand {handIndex} to {cardIds.Length} cards on client!");
            CgsNetManager.Instance.playController.drawer.SyncHand(handIndex, cardIds);
        }

        #endregion

        #region Cards

        public void MoveCardToServer(CardZone cardZone, CardModel cardModel)
        {
            var cardModelTransform = cardModel.transform;
            cardModelTransform.SetParent(cardZone.transform);
            cardModel.SnapToGrid();
            cardModel.position = ((RectTransform) cardModelTransform).localPosition;
            cardModel.rotation = cardModelTransform.rotation;
            CmdSpawnCard(cardModel.Id, cardModel.position, cardModel.rotation, cardModel.isFacedown);
            if (cardModel.IsOnline && cardModel.hasAuthority)
                CmdUnSpawnCard(cardModel.gameObject);
            Destroy(cardModel.gameObject);
        }

        [Command]
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void CmdSpawnCard(string cardId, Vector3 position, Quaternion rotation, bool isFacedown)
        {
            var playController = CgsNetManager.Instance.playController;
            var newCardGameObject = Instantiate(playController.cardModelPrefab, playController.playMat.transform);
            var cardModel = newCardGameObject.GetComponent<CardModel>();
            cardModel.Value = CardGameManager.Current.Cards[cardId];
            cardModel.position = position;
            cardModel.rotation = rotation;
            cardModel.isFacedown = isFacedown;
            PlayController.SetPlayActions(cardModel);
            NetworkServer.Spawn(newCardGameObject);
            cardModel.RpcHideHighlight();
        }

        [Command]
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void CmdUnSpawnCard(GameObject toUnSpawn)
        {
            NetworkServer.UnSpawn(toUnSpawn);
            Destroy(toUnSpawn);
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
            var die = CgsNetManager.Instance.playController.CreateDie(min, max);
            NetworkServer.Spawn(die.gameObject);
        }

        #endregion

        #region RestartGame

        public void RequestRestart()
        {
            Debug.Log("[CgsNet Player] Requesting restart!...");
            CmdRestart();
        }

        [Command]
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void CmdRestart()
        {
            Debug.Log("[CgsNet Player] Game server to restart!...");
            CgsNetManager.Instance.Restart();
        }

        [TargetRpc]
        public void TargetRestart()
        {
            Debug.Log("[CgsNet Player] Game is restarting!...");
            CgsNetManager.Instance.playController.ResetPlayArea();
            CgsNetManager.Instance.playController.drawer.Clear();
            CurrentDeck = null;
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
    }
}
