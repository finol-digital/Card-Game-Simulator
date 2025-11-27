/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cgs.CardGameView.Multiplayer;
using Cgs.Play.Drawer;
using FinolDigital.Cgs.Json;
using FinolDigital.Cgs.Json.Unity;
using Unity.Netcode;
using UnityEngine;

namespace Cgs.Play.Multiplayer
{
    public class CgsNetPlayer : NetworkBehaviour
    {
        public const string GameSelectionErrorMessage = "The host has selected a game that is not available!";
        public const string ShareDeckRequest = "Would you like to share the host's deck?";

        public ClientRpcParams OwnerClientRpcParams => new()
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] {OwnerClientId}
            }
        };

        public string Name
        {
            get => _name.Value;
            private set => _name.Value = value;
        }

        private NetworkVariable<CgsNetString> _name;

        public int Points
        {
            get => _points.Value;
            private set => _points.Value = value;
        }

        private NetworkVariable<int> _points;

        public NetworkObject CurrentDeck
        {
            get => _currentDeck.Value;
            private set => _currentDeck.Value = value;
        }

        private NetworkVariable<NetworkObjectReference> _currentDeck;

        public bool IsDeckShared
        {
            get => _isDeckShared.Value;
            private set => _isDeckShared.Value = value;
        }

        private NetworkVariable<bool> _isDeckShared;

        private IReadOnlyList<CardStack> CardStacks
        {
            get
            {
                var cardStacks = new List<CardStack>();
                foreach (var cardStack in _cardStacks)
                {
                    var networkObject = (NetworkObject) cardStack;
                    if (networkObject == null)
                        continue;
                    var cardStackObject = networkObject.GetComponent<CardStack>();
                    if (cardStackObject != null)
                        cardStacks.Add(cardStackObject);
                }
                return cardStacks;
            }
        }

        private NetworkList<NetworkObjectReference> _cardStacks;

        public int CurrentHand
        {
            get => _currentHand.Value;
            private set => _currentHand.Value = value;
        }

        private NetworkVariable<int> _currentHand;

        public int DefaultZRotation { get; private set; }

        public Quaternion DefaultRotation => Quaternion.Euler(new Vector3(0, 0, DefaultZRotation));

        public string GetHandCount()
        {
            var handCards = HandCards;
            return handCards.Count > 0 && CurrentHand >= 0 && CurrentHand < handCards.Count
                ? handCards[CurrentHand].Count.ToString()
                : string.Empty;
        }

        public IReadOnlyList<IReadOnlyList<UnityCard>> HandCards
        {
            // This getter is slow, so it should be cached when appropriate
            get
            {
                List<IReadOnlyList<UnityCard>> handCards = new();
                foreach (var stringList in _handCards)
                {
                    var cardList = stringList.ToListString().Select(cardId => CardGameManager.Current.Cards[cardId])
                        .ToList();
                    handCards.Add(cardList);
                }

                return handCards;
            }
        }

        private NetworkList<CgsNetStringList> _handCards;

        private NetworkList<CgsNetString> _handNames;

        private void Awake()
        {
            _name = new NetworkVariable<CgsNetString>();
            _points = new NetworkVariable<int>();
            _currentDeck = new NetworkVariable<NetworkObjectReference>();
            _isDeckShared = new NetworkVariable<bool>();
            _cardStacks = new NetworkList<NetworkObjectReference>();
            _currentHand = new NetworkVariable<int>();
            _handCards = new NetworkList<CgsNetStringList>();
            _handNames = new NetworkList<CgsNetString>();
        }

        #region StartGame

        public override void OnNetworkSpawn()
        {
            if (!GetComponent<NetworkObject>().IsOwner)
                return;

            Debug.Log("[CgsNet Player] Starting local player...");
            CgsNetManager.Instance.LocalPlayer = this;
            if (IsServer)
            {
                RequestNameUpdate(PlayerPrefs.GetString(Scoreboard.PlayerNamePlayerPrefs,
                    Scoreboard.DefaultPlayerName));
                RequestNewHand(CardDrawer.DefaultHandName);
                ApplyPlayerTranslationServerRpc();
            }
            else
                RequestCardGameSelection();

            Debug.Log("[CgsNet Player] Started local player!");
        }

        private void RequestCardGameSelection()
        {
            Debug.Log("[CgsNet Player] Requesting game id...");
            SelectCardGameServerRpc();
        }

        [ServerRpc]
        private void SelectCardGameServerRpc()
        {
            Debug.Log("[CgsNet Player] Sending game id...");
            SelectCardGameOwnerClientRpc(CardGameManager.Current.Id,
                CardGameManager.Current.AutoUpdateUrl?.OriginalString, OwnerClientRpcParams);
        }

        [ClientRpc]
        // ReSharper disable once UnusedParameter.Local
        private void SelectCardGameOwnerClientRpc(string gameId, string autoUpdateUrl,
            // ReSharper disable once UnusedParameter.Local
            ClientRpcParams clientRpcParams = default)
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

            RequestNameUpdate(PlayerPrefs.GetString(Scoreboard.PlayerNamePlayerPrefs, Scoreboard.DefaultPlayerName));
            ApplyPlayerRotationServerRpc();
        }

        [ServerRpc]
        private void ApplyPlayerRotationServerRpc()
        {
            ApplyPlayerRotationOwnerClientRpc(NetworkManager.Singleton.ConnectedClients.Count, OwnerClientRpcParams);
        }

        [ClientRpc]
        // ReSharper disable once UnusedParameter.Local
        private void ApplyPlayerRotationOwnerClientRpc(int playerCount, ClientRpcParams clientRpcParams = default)
        {
            if (playerCount % 4 == 0)
                DefaultZRotation = 270;
            else if (playerCount % 3 == 0)
                DefaultZRotation = 90;
            else if (playerCount % 2 == 0)
                DefaultZRotation = 180;
            else
                DefaultZRotation = 0;
            Debug.Log("[CgsNet Player] Set PlayMat rotation based off player count: " + DefaultZRotation);
            PlayController.Instance.playArea.CurrentRotation = DefaultZRotation;

            ApplyPlayerTranslationServerRpc();
        }

        [ServerRpc]
        private void ApplyPlayerTranslationServerRpc()
        {
            ApplyPlayerTranslationOwnerClientRpc(OwnerClientRpcParams);
        }

        [ClientRpc]
        // ReSharper disable once UnusedParameter.Local
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void ApplyPlayerTranslationOwnerClientRpc(ClientRpcParams clientRpcParams = default)
        {
            PlayController.Instance.playArea.verticalNormalizedPosition = 0;
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
                    PlayController.Instance.ShowDeckMenu();
                    break;
                case SharePreference.Share:
                    RequestSharedDeck();
                    break;
                case SharePreference.Ask:
                default:
                    CardGameManager.Instance.Messenger.Ask(ShareDeckRequest,
                        PlayController.Instance.ShowDeckMenu, RequestSharedDeck);
                    break;
            }

            RequestNewHand(CardDrawer.DefaultHandName);
        }

        #endregion

        #region Score

        public void RequestNameUpdate(string playerName)
        {
            UpdateNameServerRpc(playerName);
        }

        [ServerRpc]
        private void UpdateNameServerRpc(string playerName)
        {
            Name = playerName;
        }

        public void RequestPointsUpdate(int points)
        {
            UpdatePointsServerRpc(points);
        }

        [ServerRpc]
        private void UpdatePointsServerRpc(int points)
        {
            Points = points;
        }

        #endregion

        #region Boards

        public void RequestNewBoard(string gameBoardId, Vector2 size, Vector2 position)
        {
            CreateBoardServerRpc(gameBoardId, size, position);
        }

        [ServerRpc]
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void CreateBoardServerRpc(string gameBoardId, Vector2 size, Vector2 position)
        {
            PlayController.Instance.CreateBoard(gameBoardId, size, position);
        }

        #endregion

        #region CardStacks

        public void RequestNewDeck(string deckName, IEnumerable<UnityCard> cards, bool isFaceup)
        {
            Debug.Log($"[CgsNet Player] Requesting new deck {deckName}...");
            CreateCardStackServerRpc(deckName, cards.Select(card => (CgsNetString) card.Id).ToArray(), true,
                PlayController.Instance.NewPlayablePosition, DefaultRotation, isFaceup);
        }

        public void RequestNewCardStack(string stackName, IEnumerable<UnityCard> cards, Vector2 position,
            Quaternion rotation, bool isFaceup)
        {
            Debug.Log($"[CgsNet Player] Requesting new card stack {stackName}...");
            CreateCardStackServerRpc(stackName, cards.Select(card => (CgsNetString) card.Id).ToArray(), false,
                position, rotation, isFaceup);
        }

        [ServerRpc]
        // ReSharper disable once ParameterTypeCanBeEnumerable.Local
        private void CreateCardStackServerRpc(string stackName, CgsNetString[] cardIds, bool isDeck, Vector2 position,
            Quaternion rotation, bool isFaceup)
        {
            Debug.Log($"[CgsNet Player] Creating new card stack {stackName}...");
            var cardStack = PlayController.Instance.CreateCardStack(stackName,
                cardIds.Select(cardId => CardGameManager.Current.Cards[cardId]).ToList(), position, rotation, isFaceup);
            if (isDeck)
                CurrentDeck = cardStack.GetComponent<NetworkObject>();
            _cardStacks.Add(cardStack.gameObject);
            Debug.Log($"[CgsNet Player] Created new card stack {stackName}!");
        }

        private void RequestSharedDeck()
        {
            Debug.Log("[CgsNet Player] Requesting shared deck..");
            ShareDeckServerRpc();
        }

        [ServerRpc]
        private void ShareDeckServerRpc()
        {
            Debug.Log("[CgsNet Player] Sending shared deck...");
            CurrentDeck = CgsNetManager.Instance.LocalPlayer.CurrentDeck;
            IsDeckShared = true;
            _cardStacks.Add(CurrentDeck);
            ShareDeckOwnerClientRpc(NetworkManager.Singleton.ConnectedClients.Count, OwnerClientRpcParams);
        }

        [ClientRpc]
        // ReSharper disable once UnusedParameter.Local
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void ShareDeckOwnerClientRpc(int playerCount, ClientRpcParams clientRpcParams = default)
        {
            Debug.Log($"[CgsNet Player] Received share deck callback for {playerCount}!");
            PlayController.Instance.DecksCallback(CardStacks, playerCount);
        }

        public void RequestDecks(int deckCount)
        {
            Debug.Log($"[CgsNet Player] Requesting decks {deckCount}...");
            StartDecksServerRpc(deckCount);
        }

        [ServerRpc]
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void StartDecksServerRpc(int deckCount)
        {
            Debug.Log($"[CgsNet Player] Waiting for decks {deckCount}...");
            StartCoroutine(WaitForDecks(deckCount));
        }

        private IEnumerator WaitForDecks(int deckCount)
        {
            while (CardStacks.Count < deckCount)
                yield return null;
            DecksCallbackOwnerClientRpc(NetworkManager.Singleton.ConnectedClients.Count, OwnerClientRpcParams);
        }

        [ClientRpc]
        // ReSharper disable once UnusedParameter.Local
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void DecksCallbackOwnerClientRpc(int playerCount, ClientRpcParams clientRpcParams = default)
        {
            Debug.Log($"[CgsNet Player] Received decks callback for {playerCount}!");
            PlayController.Instance.DecksCallback(CardStacks, playerCount);
        }

        public void RequestShuffle(GameObject toShuffle)
        {
            Debug.Log("[CgsNet Player] Requesting shuffle...");
            ShuffleServerRpc(toShuffle);
        }

        [ServerRpc]
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void ShuffleServerRpc(NetworkObjectReference toShuffle)
        {
            Debug.Log("[CgsNet Player] Shuffling!");
            var cardStack = ((NetworkObject) toShuffle).GetComponent<CardStack>();
            cardStack.DoShuffle();
        }

        public void RequestInsert(GameObject stack, int index, string cardId)
        {
            Debug.Log($"[CgsNet Player] Requesting insert {cardId} at {index}...");
            InsertServerRpc(stack, index, cardId);
        }

        [ServerRpc]
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void InsertServerRpc(NetworkObjectReference stack, int index, string cardId)
        {
            Debug.Log($"[CgsNet Player] Insert {cardId} at {index}!");
            var cardStack = ((NetworkObject) stack).GetComponent<CardStack>();
            cardStack.OwnerInsert(index, cardId);
        }

        public void RequestRemoveAt(GameObject stack, int index)
        {
            Debug.Log($"[CgsNet Player] Requesting remove at {index}...");
            RemoveAtServerRpc(stack, index);
        }

        [ServerRpc]
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void RemoveAtServerRpc(NetworkObjectReference stack, int index)
        {
            Debug.Log($"[CgsNet Player] Remove at {index}!");
            var cardStack = ((NetworkObject) stack).GetComponent<CardStack>();
            cardStack.OwnerRemoveAt(index);
        }

        public void RequestDeal(NetworkObject stack, int count)
        {
            Debug.Log($"[CgsNet Player] Requesting deal {count}...");
            DealServerRpc(stack, count);
        }

        [ServerRpc]
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void DealServerRpc(NetworkObjectReference stack, int count)
        {
            Debug.Log($"[CgsNet Player] Dealing {count}!");
            var cardStack = ((NetworkObject) stack).GetComponent<CardStack>();
            var cardIds = new CgsNetString[count];
            for (var i = 0; i < count && cardStack.Cards.Count > 0; i++)
                cardIds[i] = cardStack.OwnerPopCard();
            DealClientRpc(cardIds, OwnerClientRpcParams);
        }

        [ClientRpc]
        // ReSharper disable once ParameterTypeCanBeEnumerable.Local
        // ReSharper disable once MemberCanBeMadeStatic.Local
        // ReSharper disable once UnusedParameter.Local
        private void DealClientRpc(CgsNetString[] cardIds, ClientRpcParams clientRpcParams = default)
        {
            Debug.Log($"[CgsNet Player] Dealt {cardIds}!");
            PlayController.Instance.AddCardsToHand(
                cardIds.Where(cardId => !string.IsNullOrEmpty(cardId) && !UnityCard.Blank.Id.Equals(cardId))
                    .Select(cardId => CardGameManager.Current.Cards[cardId]));
        }

        #endregion

        #region Hands

        public void RequestNewHand(string handName)
        {
            Debug.Log($"[CgsNet Player] Requesting new hand {handName}...");
            AddHandServerRpc(handName);
        }

        [ServerRpc]
        private void AddHandServerRpc(string handName)
        {
            Debug.Log($"[CgsNet Player] Add hand {handName}!");
            _handCards.Add(new CgsNetStringList());
            _handNames.Add(handName);
            CurrentHand = _handNames.Count - 1;
            UseHandClientRpc(CurrentHand, OwnerClientRpcParams);
        }

        [ClientRpc]
        // ReSharper disable once MemberCanBeMadeStatic.Local
        // ReSharper disable once UnusedParameter.Local
        private void UseHandClientRpc(int handIndex, ClientRpcParams clientRpcParams = default)
        {
            PlayController.Instance.drawer.SelectTab(handIndex);
        }

        public void RequestUseHand(int handIndex)
        {
            Debug.Log($"[CgsNet Player] Requesting use hand {handIndex}...");
            UseHandServerRpc(handIndex);
        }

        [ServerRpc]
        private void UseHandServerRpc(int handIndex)
        {
            Debug.Log($"[CgsNet Player] Use hand {handIndex}!");
            CurrentHand = handIndex;
        }

        public void RequestSyncHand(int handIndex, CgsNetString[] cardIds)
        {
            Debug.Log($"[CgsNet Player] Requesting sync hand {handIndex} to {cardIds.Length}...");
            SyncHandServerRpc(handIndex, cardIds);
        }

        [ServerRpc]
        private void SyncHandServerRpc(int handIndex, CgsNetString[] cardIds)
        {
            Debug.Log($"[CgsNet Player] Sync hand {handIndex} to {cardIds.Length} cards on Server!");
            if (handIndex < 0 || handIndex >= _handCards.Count)
            {
                Debug.LogError($"[CgsNet Player] {handIndex} is out of bounds of {_handCards.Count}");
                return;
            }

            _handCards[handIndex] = CgsNetStringList.Of(cardIds);
            SyncHandClientRpc(handIndex, cardIds, OwnerClientRpcParams);
        }

        [ClientRpc]
        // ReSharper disable once MemberCanBeMadeStatic.Local
        // ReSharper disable once UnusedParameter.Local
        private void SyncHandClientRpc(int handIndex, CgsNetString[] cardIds, ClientRpcParams clientRpcParams = default)
        {
            Debug.Log($"[CgsNet Player] Sync hand {handIndex} to {cardIds.Length} cards on client!");
            PlayController.Instance.drawer.SyncHand(handIndex, cardIds);
        }

        public void RequestRemoveHand(int handIndex)
        {
            Debug.Log($"[CgsNet Player] Requesting remove hand {handIndex}...");
            RemoveHandServerRpc(handIndex);
        }

        [ServerRpc]
        private void RemoveHandServerRpc(int handIndex)
        {
            Debug.Log($"[CgsNet Player] Remove hand {handIndex}!");
            if (handIndex < 1 || handIndex >= _handCards.Count)
            {
                Debug.LogError($"[CgsNet Player] {handIndex} is out of bounds of {_handCards.Count}");
                return;
            }

            _handCards.RemoveAt(handIndex);

            CurrentHand = 0;
            UseHandClientRpc(CurrentHand, OwnerClientRpcParams);
        }

        #endregion

        #region Cards

        public void MoveCardToServer(CardZone cardZone, CardModel cardModel)
        {
            var cardModelTransform = cardModel.transform;
            cardModelTransform.SetParent(cardZone.transform);
            cardModel.SnapToGrid();
            var position = ((RectTransform) cardModelTransform).localPosition;
            var rotation = cardModelTransform.localRotation;
            var isFacedown = cardModel.IsFacedown && !cardModel.Value.IsBackFaceCard;

            if (cardZone.IsSpawned)
                SpawnCardInZoneServerRpc(cardZone.gameObject, cardModel.Id, position, rotation, isFacedown,
                    cardZone.DefaultAction.ToString());
            else
                SpawnCardInPlayAreaServerRpc(cardModel.Id, position, rotation, isFacedown);

            if (cardModel.IsSpawned)
                DespawnCardServerRpc(cardModel.gameObject);
            else
                Destroy(cardModel.gameObject);
        }

        [ServerRpc]
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void SpawnCardInZoneServerRpc(NetworkObjectReference container, string cardId, Vector3 position,
            Quaternion rotation, bool isFacedown, string defaultAction)
        {
            PlayController.Instance.CreateCardModel(container, cardId, position, rotation, isFacedown, defaultAction);
        }

        [ServerRpc]
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void SpawnCardInPlayAreaServerRpc(string cardId, Vector3 position, Quaternion rotation, bool isFacedown)
        {
            PlayController.Instance.CreateCardModel(null, cardId, position, rotation, isFacedown);
        }

        [ServerRpc]
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void DespawnCardServerRpc(NetworkObjectReference toDespawn)
        {
            var go = ((NetworkObject) toDespawn).gameObject;
            go.GetComponent<NetworkObject>().Despawn();
            Destroy(go);
        }

        #endregion

        #region Dice

        public void RequestNewDie(Vector2 position, Quaternion rotation, int max, int value, Vector3 color)
        {
            CreateDieServerRpc(position, rotation, max, value, color);
        }

        [ServerRpc]
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void CreateDieServerRpc(Vector2 position, Quaternion rotation, int max, int value, Vector3 color)
        {
            PlayController.Instance.CreateDie(position, rotation, max, value, new Color(color.x, color.y, color.z));
        }

        #endregion

        #region Tokens

        public void RequestNewToken(Vector2 position, Quaternion rotation, Vector3 color)
        {
            CreateTokenServerRpc(position, rotation, color);
        }

        [ServerRpc]
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void CreateTokenServerRpc(Vector2 position, Quaternion rotation, Vector3 color)
        {
            PlayController.Instance.CreateToken(position, rotation, new Color(color.x, color.y, color.z));
        }

        #endregion

        #region Zones

        public void RequestNewZone(string type, Vector2 position, Quaternion rotation, Vector2 size, string face,
            string action)
        {
            CreateZoneServerRpc(type, position, rotation, size, face, action);
        }

        [ServerRpc]
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void CreateZoneServerRpc(string type, Vector2 position, Quaternion rotation, Vector2 size, string face,
            string action)
        {
            PlayController.Instance.CreateZone(type, position, rotation, size, face, action);
        }

        #endregion

        #region RestartGame

        public void RequestRestart()
        {
            Debug.Log("[CgsNet Player] Requesting restart!...");
            RestartServerRpc();
        }

        [Rpc(SendTo.Server)]
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void RestartServerRpc()
        {
            Debug.Log("[CgsNet Player] Game server to restart!...");
            foreach (var cardStack in PlayController.Instance.playAreaCardZone.GetComponentsInChildren<CardStack>())
                cardStack.MyNetworkObject.Despawn();
            foreach (var cardModel in PlayController.Instance.playAreaCardZone.GetComponentsInChildren<CardModel>())
                cardModel.MyNetworkObject.Despawn();
            foreach (var die in PlayController.Instance.playAreaCardZone.GetComponentsInChildren<Die>())
                die.MyNetworkObject.Despawn();
            foreach (var token in PlayController.Instance.playAreaCardZone.GetComponentsInChildren<Token>())
                token.MyNetworkObject.Despawn();
            foreach (var player in FindObjectsByType<CgsNetPlayer>(FindObjectsSortMode.None))
                player.RestartClientRpc(player.OwnerClientRpcParams);
        }

        [ClientRpc]
        // ReSharper disable once UnusedParameter.Local
        private void RestartClientRpc(ClientRpcParams clientRpcParams = default)
        {
            Debug.Log("[CgsNet Player] Game is restarting!...");
            PlayController.Instance.ResetPlayArea();
            PlayController.Instance.drawer.Clear();
            Points = CardGameManager.Current.GameStartPointsCount;
            CurrentDeck = null;
            CurrentHand = 0;
            StartCoroutine(WaitToRestartGame());
        }

        private IEnumerator WaitToRestartGame()
        {
            if (IsServer || CardGameManager.Current.DeckSharePreference == SharePreference.Individual)
            {
                PlayController.Instance.ShowDeckMenu();
                Debug.Log("[CgsNet Player] Game restarted!");
                yield break;
            }

            yield return null;

            Debug.Log("[CgsNet Player] Game restarted!");

            switch (CardGameManager.Current.DeckSharePreference)
            {
                case SharePreference.Individual:
                    PlayController.Instance.ShowDeckMenu();
                    break;
                case SharePreference.Share:
                    RequestSharedDeck();
                    break;
                case SharePreference.Ask:
                default:
                    CardGameManager.Instance.Messenger.Ask(ShareDeckRequest,
                        PlayController.Instance.ShowDeckMenu, RequestSharedDeck);
                    break;
            }
        }

        #endregion
    }
}
