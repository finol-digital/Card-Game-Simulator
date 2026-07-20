/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Cgs.CardGameView;
using Cgs.CardGameView.Multiplayer;
using Cgs.CardGameView.Viewer;
using Cgs.Cards;
using Cgs.Decks;
using Cgs.Play.Drawer;
using Cgs.Play.Multiplayer;
using Cgs.UI.ScrollRects;
using FinolDigital.Cgs.Json;
using FinolDigital.Cgs.Json.Unity;
using JetBrains.Annotations;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityExtensionMethods;
using CardAction = FinolDigital.Cgs.Json.CardAction;

namespace Cgs.Play
{
    [RequireComponent(typeof(Canvas))]
    public class PlayController : MonoBehaviour, ICardDropHandler, ICardContainer
    {
        public const string MainMenuPrompt = "Go back to the main menu?";
        public const string RestartPrompt = "Restart?";
        public const string DefaultStackName = "Stack";

        public static string LoadStartDecksAsk
        {
            get
            {
                var text = new StringBuilder("Load ");
                var deckUrls = CardGameManager.Current.GameStartDecks;
                text.Append($"'{deckUrls[0].Name}'");
                for (var i = 1; i < deckUrls.Count; i++)
                    text.Append($", '{deckUrls[i].Name}'");
                text.Append("?");
                return text.ToString();
            }
        }

        public const float PlayableMoveSpeed = 600f;
        private const float PlayAreaBuffer = 8;
        private const float DeckPositionBuffer = 50;

        public static PlayController Instance { get; private set; }

        public GameObject cardViewerPrefab;
        public GameObject playableViewerPrefab;
        public GameObject lobbyMenuPrefab;
        public GameObject playSettingsMenuPrefab;
        public GameObject deckLoadMenuPrefab;
        public GameObject searchMenuPrefab;
        public GameObject handDealerPrefab;
        public GameObject moveMenuPrefab;

        public GameObject boardPrefab;
        public GameObject cardStackPrefab;
        public GameObject cardModelPrefab;
        public GameObject diePrefab;
        [FormerlySerializedAs("tokenPrefab")] public GameObject counterPrefab;

        public GameObject diceZonePrefab;
        public GameObject horizontalCardZonePrefab;
        public GameObject verticalCardZonePrefab;
        public GameObject playMatPrefab;

        public readonly struct CardModelCreationOptions
        {
            public CardModelCreationOptions(string defaultAction = "", ulong? ownerClientId = null,
                int? siblingIndex = null)
            {
                DefaultAction = defaultAction;
                OwnerClientId = ownerClientId;
                SiblingIndex = siblingIndex;
            }

            public string DefaultAction { get; }
            public ulong? OwnerClientId { get; }
            public int? SiblingIndex { get; }
        }

        public Transform stackViewers;

        public RotateZoomableScrollRect playArea;
        public CardZone playAreaCardZone;
        public Image playMatImage;
        public List<CardDropArea> playDropZones;
        public CardDrawer drawer;
        public PlayMenu menu;
        public Scoreboard scoreboard;

        public bool IsBlocked => CardViewer.Instance.IsVisible || CardViewer.Instance.WasVisible ||
                                 CardViewer.Instance.Zoom || PlayableViewer.Instance.IsVisible ||
                                 PlayableViewer.Instance.WasVisible || scoreboard.nameInputField.isFocused ||
                                 scoreboard.pointsInputField.isFocused || CardGameManager.Instance.ModalCanvas != null;

        public Vector2 NewPlayablePosition
        {
            get
            {
                var allCardStacks = AllCardStacks.ToList();
                if (allCardStacks.Count < CardGameManager.Current.GamePlayDeckPositions.Count)
                {
                    var position = CardGameManager.Current.GamePlayDeckPositions[allCardStacks.Count];
                    return new Vector2(CardGameManager.PixelsPerInch * position.X,
                        CardGameManager.PixelsPerInch * position.Y);
                }

                var cardStack = cardStackPrefab.GetComponent<CardStack>();
                var cardStackLabelHeight =
                    ((RectTransform)cardStack.deckLabel.transform.parent).rect.height * playArea.CurrentZoom;
                var cardSize = CardGameManager.PixelsPerInch * playArea.CurrentZoom *
                               new Vector2(CardGameManager.Current.CardSize.X, CardGameManager.Current.CardSize.Y);

                var up = Vector2.up * (Screen.height - cardSize.y * 2f - cardStackLabelHeight);
                var right = Vector2.right * (cardSize.x * 2f);
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)playAreaCardZone.transform,
                    (up + right),
                    null, out var nextDeckPosition);

                var nextOffset = new Rect(nextDeckPosition, cardSize);
                while (allCardStacks.Any(stack =>
                           (new Rect(stack.transform.localPosition, cardSize)).Overlaps(nextOffset)))
                {
                    nextDeckPosition += Vector2.down * (cardSize.y + cardStackLabelHeight);
                    nextOffset = new Rect(nextDeckPosition, cardSize);
                }

                var widthDelta = CardGameManager.Current.PlayMatSize.X * CardGameManager.PixelsPerInch / 2.0f;
                var heightDelta = CardGameManager.Current.PlayMatSize.Y * CardGameManager.PixelsPerInch / 2.0f;
                nextDeckPosition = new Vector2(
                    Mathf.Clamp(nextDeckPosition.x, -widthDelta + cardSize.x / 2.0f, widthDelta - cardSize.x / 2.0f),
                    Mathf.Clamp(nextDeckPosition.y, -heightDelta + cardSize.y / 2.0f, heightDelta - cardSize.y / 2.0f));

                return nextDeckPosition;
            }
        }

        // Distinct extra group names in order of first appearance; defines each group's fixed slot offset
        private static List<string> ExtraGroupNames
        {
            get
            {
                var extraGroupNames = new List<string>();
                foreach (var groupName in CardGameManager.Current.Extras.Select(extraDef =>
                             !string.IsNullOrEmpty(extraDef.Group)
                                 ? extraDef.Group
                                 : ExtraDef.DefaultExtraGroup).Where(groupName => !extraGroupNames.Contains(groupName)))
                    extraGroupNames.Add(groupName);

                return extraGroupNames;
            }
        }

        // GamePlayDeckPositions is interpreted as consecutive per-seat blocks of (deck + extra group) slots,
        // but only when it is authored in whole block multiples; otherwise callers fall back to load-order filling
        private static bool TryGetSeatSlotPosition(int blockIndex, int slotOffset, out Vector2 position)
        {
            position = Vector2.zero;
            if (blockIndex < 0 || slotOffset < 0)
                return false;

            var deckPositions = CardGameManager.Current.GamePlayDeckPositions;
            var slotsPerPlayer = 1 + ExtraGroupNames.Count;
            if (deckPositions.Count == 0 || deckPositions.Count % slotsPerPlayer != 0 ||
                slotOffset >= slotsPerPlayer)
                return false;

            var slotIndex = blockIndex * slotsPerPlayer + slotOffset;
            if (slotIndex >= deckPositions.Count)
                return false;

            var slotPosition = deckPositions[slotIndex];
            position = CardGameManager.PixelsPerInch * new Vector2(slotPosition.X, slotPosition.Y);
            return true;
        }

        public IEnumerable<CardStack> AllCardStacks => playAreaCardZone.GetComponentsInChildren<CardStack>();

        public IEnumerable<CardZone> AllCardZones => playAreaCardZone.GetComponentsInChildren<CardZone>();

        public CardZone FindCardZoneAt(Vector2 worldPosition)
        {
            foreach (var cardZone in AllCardZones)
            {
                if (cardZone == playAreaCardZone ||
                    cardZone.Type is not (CardZoneType.Horizontal or CardZoneType.Vertical))
                    continue;

                var rectTransform = (RectTransform)cardZone.transform;
                if (rectTransform.rect.Contains((Vector2)rectTransform.InverseTransformPoint(worldPosition)))
                    return cardZone;
            }

            return null;
        }

        public CardStack CurrentDeckStack { get; set; }

        // Solo play: which seat block the next deck load occupies (counts deck loads, not stacks)
        private int _soloDeckLoadCount;

        public LobbyMenu Lobby => _lobby ??= Instantiate(lobbyMenuPrefab).GetOrAddComponent<LobbyMenu>();

        private LobbyMenu _lobby;

        private PlaySettingsMenu Settings =>
            _settings ??= Instantiate(playSettingsMenuPrefab).GetOrAddComponent<PlaySettingsMenu>();

        private PlaySettingsMenu _settings;

        private DeckLoadMenu DeckLoader =>
            _deckLoader ??= Instantiate(deckLoadMenuPrefab).GetOrAddComponent<DeckLoadMenu>();

        private DeckLoadMenu _deckLoader;

        private CardSearchMenu CardSearcher =>
            _cardSearcher ??= Instantiate(searchMenuPrefab).GetOrAddComponent<CardSearchMenu>();

        private CardSearchMenu _cardSearcher;

        private HandDealer Dealer => _dealer ??= Instantiate(handDealerPrefab).GetOrAddComponent<HandDealer>();

        private HandDealer _dealer;

        private MoveMenu Mover => _moveMenu ??= Instantiate(moveMenuPrefab).GetOrAddComponent<MoveMenu>();

        private MoveMenu _moveMenu;

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            Instantiate(cardViewerPrefab);
            Instantiate(playableViewerPrefab);
            if (Menu.Settings.PreviewOnMouseOver)
                CardViewer.Instance.Mode = CardViewerMode.Expanded;
            CardViewer.Instance.IsActionable = true;
            CardGameManager.Instance.OnSceneActions.Add(ResetPlayArea);

            InputSystem.actions.FindAction(Tags.PlayGameMenu).performed += InputPlayGameMenu;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed += InputCancel;
        }

        private IEnumerator Start()
        {
            CardGameManager.Instance.CardCanvases.Add(GetComponent<Canvas>());

            playAreaCardZone.OnAddCardActions.Add(AddCardToPlayArea);
            playDropZones.ForEach(dropZone => dropZone.DropHandler = this);

            yield return null;
            while (!CardGameManager.IsCurrentReady)
                yield return null;

            StartLobby();
        }

        private void Restart()
        {
            if (CgsNetManager.Instance.IsOnline && CgsNetManager.Instance.LocalPlayer != null)
                CgsNetManager.Instance.LocalPlayer.RequestRestart();
            else
            {
                ResetPlayArea();
                drawer.Clear();
                StartDecks();
            }
        }

        public void ResetPlayArea()
        {
            _soloDeckLoadCount = 0;

            var rectTransform = (RectTransform)playAreaCardZone.transform;
            if (!NetworkManager.Singleton.IsConnectedClient)
                rectTransform.DestroyAllChildren();
            else if (CgsNetManager.Instance.IsHost)
            {
                foreach (var cardStack in playAreaCardZone.GetComponentsInChildren<CardStack>())
                    if (cardStack != null && cardStack.MyNetworkObject != null && cardStack.MyNetworkObject.IsSpawned)
                        cardStack.MyNetworkObject.Despawn();
                foreach (var cardModel in playAreaCardZone.GetComponentsInChildren<CardModel>())
                    if (cardModel != null && cardModel.MyNetworkObject != null && cardModel.MyNetworkObject.IsSpawned)
                        cardModel.MyNetworkObject.Despawn();
                foreach (var die in playAreaCardZone.GetComponentsInChildren<Die>())
                    if (die != null && die.MyNetworkObject != null && die.MyNetworkObject.IsSpawned)
                        die.MyNetworkObject.Despawn();
                foreach (var counter in playAreaCardZone.GetComponentsInChildren<Counter>())
                    if (counter != null && counter.MyNetworkObject != null && counter.MyNetworkObject.IsSpawned)
                        counter.MyNetworkObject.Despawn();
                foreach (var zone in playAreaCardZone.GetComponentsInChildren<CardZone>())
                    if (zone != null && zone.MyNetworkObject != null && zone.MyNetworkObject.IsSpawned
                        && zone != playAreaCardZone)
                        zone.MyNetworkObject.Despawn();
                foreach (var zone in playAreaCardZone.GetComponentsInChildren<DiceZone>())
                    if (zone != null && zone.MyNetworkObject != null && zone.MyNetworkObject.IsSpawned)
                        zone.MyNetworkObject.Despawn();
                rectTransform.DestroyAllChildren();
            }

            rectTransform.sizeDelta = new Vector2(CardGameManager.Current.PlayMatSize.X + PlayAreaBuffer,
                CardGameManager.Current.PlayMatSize.Y + PlayAreaBuffer) * CardGameManager.PixelsPerInch;

            var isOfflineOrHost = !NetworkManager.Singleton.IsConnectedClient || CgsNetManager.Instance.IsHost;

            // The playmat is not a networked object, so even connected clients must refresh their local playmat,
            // e.g. after downloading the host's game
            if (isOfflineOrHost || playMatImage == null)
                playMatImage = Instantiate(playMatPrefab.gameObject, playAreaCardZone.transform)
                    .GetOrAddComponent<Image>();
            var playMatRectTransform = (RectTransform)playMatImage.transform;
            playMatRectTransform.anchoredPosition = Vector2.zero;
            playMatRectTransform.sizeDelta = new Vector2(CardGameManager.Current.PlayMatSize.X,
                CardGameManager.Current.PlayMatSize.Y) * CardGameManager.PixelsPerInch;
            playMatImage.sprite = CardGameManager.Current.PlayMatImageSprite;
            playMatImage.transform.SetAsFirstSibling();

            if (isOfflineOrHost)
                CreateZones();

            scoreboard.ChangePoints(CardGameManager.Current.GameStartPointsCount.ToString());
        }

        private void StartLobby()
        {
            if (CardGameManager.Instance.IsSearchingForServer)
                Lobby.Show();
            else
            {
                Lobby.IsLanConnectionSource = true;
#if !UNITY_WEBGL
                Lobby.Host();
#endif
                StartDecks();
            }
        }

        private void StartDecks()
        {
            if (CardGameManager.Current.GameStartDecks.Count > 0)
                CardGameManager.Instance.Messenger.Ask(LoadStartDecksAsk, ShowDeckMenu, StartLoadStartDecks);
            else
                ShowDeckMenu();
        }

        private void StartLoadStartDecks()
        {
            StartCoroutine(LoadStartDecks());
        }

        private IEnumerator LoadStartDecks()
        {
            foreach (var deckUrl in CardGameManager.Current.GameStartDecks)
            {
                if (string.IsNullOrEmpty(deckUrl.Name) || !deckUrl.IsAvailable)
                {
                    Debug.Log($"Ignoring deckUrl {deckUrl}");
                    continue;
                }

                var deckFilePath = Path.Combine(CardGameManager.Current.DecksDirectoryPath,
                    deckUrl.Name + "." + CardGameManager.Current.DeckFileType.ToString().ToLower());

                if (!File.Exists(deckFilePath))
                {
                    if (!string.IsNullOrEmpty(CardGameManager.Current.AllDecksUrlTxtRoot) &&
                        !string.IsNullOrEmpty(deckUrl.Txt))
                        yield return UnityFileMethods.SaveUrlToFile(
                            CardGameManager.Current.AllDecksUrlTxtRoot + deckUrl.Txt, deckFilePath);
                    else if (deckUrl.Url.IsAbsoluteUri)
                        yield return UnityFileMethods.SaveUrlToFile(deckUrl.Url.AbsoluteUri, deckFilePath);
                    else
                    {
                        Debug.Log($"Empty url for deckUrl {deckUrl}");
                        continue;
                    }
                }

                try
                {
                    var deckText = File.ReadAllText(deckFilePath);
                    var newDeck = UnityDeck.Parse(CardGameManager.Current, deckUrl.Name,
                        CardGameManager.Current.DeckFileType, deckText);
                    LoadDeck(newDeck);
                }
                catch (Exception e)
                {
                    Debug.LogError(DeckLoadMenu.DeckLoadErrorMessage + e);
                    CardGameManager.Instance.Messenger.Show(DeckLoadMenu.DeckLoadErrorMessage + e.Message);
                }
            }
        }

        private void InputPlayGameMenu(InputAction.CallbackContext context)
        {
            if (IsBlocked)
                return;

            if (CardViewer.Instance.PreviewCardModel == null)
                menu.ToggleMenu();
        }

        public void ShowPlaySettingsMenu()
        {
            Settings.Show();
        }

        public void ShowCardsMenu()
        {
            CardSearcher.Show(DisplayResults);
        }

        public void ShowDeckMenu()
        {
            DeckLoader.Show(LoadDeck);
        }

        private void LoadDeck(UnityDeck deck)
        {
            foreach (var card in deck.Cards)
            {
                foreach (var gameBoardCard in CardGameManager.Current.GameBoardCards.Where(boardCard =>
                             card.Id.Equals(boardCard.Card)))
                    CreateGameBoards(gameBoardCard.Boards);
            }

            var extraGroups = deck.GetExtraGroups();
            var extraCards = deck.GetExtraCards();
            var deckCards = deck.Cards.Where(card => !extraCards.Contains(card)).Cast<UnityCard>().ToList();
            deckCards.Shuffle();

            var deckName = !string.IsNullOrEmpty(CardGameManager.Current.GamePlayDeckName)
                ? CardGameManager.Current.GamePlayDeckName
                : deck.Name;
            var extraGroupNames = ExtraGroupNames;
            if (CgsNetManager.Instance.IsOnline && CgsNetManager.Instance.LocalPlayer != null)
            {
                var seatIndex = CgsNetManager.Instance.LocalPlayer.SeatIndex;
                var startingDeckCount = AllCardStacks.ToList().Count;
                if (!TryGetSeatSlotPosition(seatIndex, 0, out var newDeckPosition))
                    newDeckPosition = NewPlayablePosition;
                CgsNetManager.Instance.LocalPlayer.RequestNewDeck(deckName, deckCards, newDeckPosition, false);
                var i = 1;
                foreach (var (stackName, cards) in extraGroups)
                {
                    var slotOffset = 1 + extraGroupNames.IndexOf(stackName);
                    if (slotOffset < 1 || !TryGetSeatSlotPosition(seatIndex, slotOffset, out var position))
                    {
                        position = newDeckPosition +
                                   Vector2.right *
                                   (CardGameManager.PixelsPerInch * i * CardGameManager.Current.CardSize.X +
                                    DeckPositionBuffer);

                        var deckCount = startingDeckCount + i;
                        if (deckCount < CardGameManager.Current.GamePlayDeckPositions.Count)
                        {
                            var targetPosition = CardGameManager.Current.GamePlayDeckPositions[deckCount];
                            position = CardGameManager.PixelsPerInch * new Vector2(targetPosition.X, targetPosition.Y);
                        }
                    }

                    CgsNetManager.Instance.LocalPlayer.RequestNewCardStack(stackName, cards.Cast<UnityCard>().Reverse(),
                        position, CgsNetManager.Instance.LocalPlayer.DefaultRotation, false);
                    i++;
                }

                CgsNetManager.Instance.LocalPlayer.RequestDecks(i);
            }
            else
            {
                var blockIndex = _soloDeckLoadCount;
                _soloDeckLoadCount++;
                if (!TryGetSeatSlotPosition(blockIndex, 0, out var newDeckPosition))
                    newDeckPosition = NewPlayablePosition;
                List<CardStack> cardStacks = new();
                CurrentDeckStack = CreateCardStack(deckName, deckCards, newDeckPosition, Quaternion.identity, false);
                cardStacks.Add(CurrentDeckStack);
                var i = 1;
                foreach (var (groupName, cards) in extraGroups)
                {
                    var slotOffset = 1 + extraGroupNames.IndexOf(groupName);
                    if (slotOffset < 1 || !TryGetSeatSlotPosition(blockIndex, slotOffset, out var position))
                    {
                        position = newDeckPosition + Vector2.right *
                            (CardGameManager.PixelsPerInch * i * CardGameManager.Current.CardSize.X);

                        var deckCount = AllCardStacks.ToList().Count;
                        if (deckCount < CardGameManager.Current.GamePlayDeckPositions.Count)
                        {
                            var targetPosition = CardGameManager.Current.GamePlayDeckPositions[deckCount];
                            position = CardGameManager.PixelsPerInch * new Vector2(targetPosition.X, targetPosition.Y);
                        }
                    }

                    var cardStack = CreateCardStack(groupName, cards.Cast<UnityCard>().Reverse().ToList(), position,
                        Quaternion.identity, false);
                    cardStacks.Add(cardStack);
                    i++;
                }

                DecksCallback(cardStacks, 1);
            }
        }

        private void CreateGameBoards(IEnumerable<GameBoard> gameBoards)
        {
            foreach (var gameBoard in gameBoards)
            {
                var size = new Vector2(gameBoard.Size.X, gameBoard.Size.Y);
                var position = new Vector2(gameBoard.OffsetMin.X, gameBoard.OffsetMin.Y) *
                               CardGameManager.PixelsPerInch;
                if (CgsNetManager.Instance.IsOnline && CgsNetManager.Instance.LocalPlayer != null)
                    CgsNetManager.Instance.LocalPlayer.RequestNewBoard(gameBoard.Id, size, position);
                else
                    CreateBoard(gameBoard.Id, size, position);
            }
        }

        public Board CreateBoard(string gameBoardId, Vector2 size, Vector2 position)
        {
            var board = Instantiate(boardPrefab, playAreaCardZone.transform).GetComponent<Board>();
            if (!string.IsNullOrEmpty(gameBoardId))
                board.GameBoardId = gameBoardId;
            if (!Vector2.zero.Equals(size))
                board.Size = size;
            var rectTransform = (RectTransform)board.transform;
            if (!Vector2.zero.Equals(position))
                rectTransform.localPosition = position;
            board.Position = rectTransform.localPosition;
            return board;
        }

        public CardStack CreateCardStack(string stackName, IReadOnlyList<UnityCard> cards, Vector2 position,
            Quaternion rotation, bool isFaceup, ulong? ownerClientId = null)
        {
            var cardStack = Instantiate(cardStackPrefab, playAreaCardZone.transform).GetComponent<CardStack>();
            if (CgsNetManager.Instance.IsOnline)
            {
                if (ownerClientId.HasValue)
                    cardStack.MyNetworkObject.SpawnWithOwnership(ownerClientId.Value);
                else
                    cardStack.MyNetworkObject.Spawn();
            }

            if (!string.IsNullOrEmpty(stackName))
                cardStack.Name = stackName;
            if (cards != null)
                cardStack.Cards = cards;

            cardStack.Position = position;
            cardStack.Rotation = rotation;

            if (isFaceup)
                cardStack.IsTopFaceup = true;

            return cardStack;
        }

        public void DecksCallback(IReadOnlyList<CardStack> cardStacks, int playerCount)
        {
            var decksToCardsToPlay = FindDeckToCardsToPlay(cardStacks, playerCount);

            var deckPlayCardsAsk = BuildAskForDeckPlayCards(decksToCardsToPlay);
            if (!string.IsNullOrEmpty(deckPlayCardsAsk))
                CardGameManager.Instance.Messenger.Ask(deckPlayCardsAsk, PromptForHand,
                    () => MoveToPlay(decksToCardsToPlay));
            else
                PromptForHand();
        }

        private static Dictionary<DeckPlayCard, Dictionary<CardStack, List<int>>> FindDeckToCardsToPlay(
            IReadOnlyList<CardStack> cardStacks, int playerCount)
        {
            var deckToCardsToPlay = new Dictionary<DeckPlayCard, Dictionary<CardStack, List<int>>>();

            var deckQuery = "playerCount=" + playerCount;
            foreach (var deckPlayCard in CardGameManager.Current.DeckPlayCards.Where(deckPlayCard =>
                         string.IsNullOrEmpty(deckPlayCard.DeckQuery) || deckPlayCard.DeckQuery.Equals(deckQuery)))
                deckToCardsToPlay[deckPlayCard] = FindCardsToPlay(deckPlayCard, cardStacks);

            return deckToCardsToPlay;
        }

        private static Dictionary<CardStack, List<int>> FindCardsToPlay(DeckPlayCard deckPlayCard,
            IEnumerable<CardStack> cardStacks)
        {
            var cardsToPlay = new Dictionary<CardStack, List<int>>();

            var cardSearchFilters = new CardSearchFilters();
            cardSearchFilters.Parse(deckPlayCard.CardQuery);
            var matchingCards = CardGameManager.Current.FilterCards(cardSearchFilters).ToList();

            foreach (var cardStack in cardStacks)
            {
                var cardIndexes = new List<int>();
                var cards = cardStack.Cards;
                for (var i = 0; i < cards.Count; i++)
                    if (matchingCards.Contains(cards[i]) && cardIndexes.Count == 0) // Only play the first match
                        cardIndexes.Add(i);
                if (cardIndexes.Any())
                    cardsToPlay[cardStack] = cardIndexes;
            }

            return cardsToPlay;
        }

        private static string BuildAskForDeckPlayCards(
            Dictionary<DeckPlayCard, Dictionary<CardStack, List<int>>> deckToCardsToPlay)
        {
            StringBuilder text = null;

            foreach (var deckPlayCard in deckToCardsToPlay)
            {
                foreach (var cardStackToPlay in deckPlayCard.Value)
                {
                    var cards = cardStackToPlay.Key.Cards;
                    foreach (var cardToPlay in cardStackToPlay.Value)
                    {
                        if (text == null)
                            text = new StringBuilder("Play '" + cards[cardToPlay].Name + "'");
                        else
                            text.Append(" and '" + cards[cardToPlay].Name + "'");
                    }
                }
            }

            text?.Append("?");
            return text?.ToString();
        }

        private void MoveToPlay(Dictionary<DeckPlayCard, Dictionary<CardStack, List<int>>> deckToCardsToPlay)
        {
            foreach (var deckPlayCard in deckToCardsToPlay)
            {
                foreach (var cardStackToPlay in deckPlayCard.Value)
                {
                    var cards = cardStackToPlay.Key.Cards;
                    cardStackToPlay.Value.Reverse();
                    foreach (var cardToPlay in cardStackToPlay.Value)
                    {
                        var position = new Vector2(deckPlayCard.Key.Position.X, deckPlayCard.Key.Position.Y) *
                                       CardGameManager.PixelsPerInch;
                        var rotation = Quaternion.Euler(0, 0, deckPlayCard.Key.Rotation);
                        var cardModel = CreateCardModel(playAreaCardZone.gameObject, cards[cardToPlay].Id, position,
                            rotation, false, cardStackToPlay.Key.IsDeckShared);
                        cardStackToPlay.Key.RequestRemoveAt(cardToPlay);
                        AddCardToPlayArea(playAreaCardZone, cardModel);
                    }
                }
            }

            PromptForHand();
        }

        private void PromptForHand()
        {
            if (CardGameManager.Current.GameStartHandCount > 0)
                ShowDealer();
        }

        public void ShowDealer()
        {
            Dealer.Show(DealHand);
        }

        private void DealHand()
        {
            drawer.SemiShow();
            DealHand(Dealer.Count);
        }

        public void DealHand(int cardCount)
        {
            if (CgsNetManager.Instance.IsOnline && CgsNetManager.Instance.LocalPlayer != null &&
                CgsNetManager.Instance.LocalPlayer.CurrentDeck != null)
                CgsNetManager.Instance.LocalPlayer.RequestDeal(CgsNetManager.Instance.LocalPlayer.CurrentDeck,
                    cardCount);
            else
                AddCardsToHand(PopSoloDeckCards(cardCount));
        }

        public void AddCardsToHand(IEnumerable<UnityCard> cards)
        {
            drawer.SelectTab(0);
            foreach (var card in cards)
                drawer.AddCard(card);
        }

        private IEnumerable<UnityCard> PopSoloDeckCards(int count)
        {
            var cards = new List<UnityCard>(count);
            if (CurrentDeckStack == null)
                return cards;

            for (var i = 0; i < count && CurrentDeckStack.Cards.Count > 0; i++)
                cards.Add(CardGameManager.Current.Cards[CurrentDeckStack.OwnerPopCard()]);
            return cards;
        }

        public void ShowMoveMenu(CardModel cardModel)
        {
            Mover.Show(cardModel);
        }

        public void AddCard(Card card)
        {
            AddCard(card, false);
        }

        public void AddCard(Card card, bool isFacedown)
        {
            isFacedown = isFacedown && !card.IsBackFaceCard;

            if (CgsNetManager.Instance.IsOnline && CgsNetManager.Instance.LocalPlayer != null)
                CgsNetManager.Instance.LocalPlayer.RequestNewCard(card.Id, Vector2.zero, Quaternion.identity,
                    isFacedown, SharePreference.Share == CardGameManager.Current.DeckSharePreference);
            else
            {
                var cardModel = CreateCardModel(playAreaCardZone.gameObject, card.Id, Vector2.zero, Quaternion.identity,
                    isFacedown, SharePreference.Share == CardGameManager.Current.DeckSharePreference);
                AddCardToPlayArea(playAreaCardZone, cardModel);
            }
        }

        private static void AddCardToPlayArea(CardZone cardZone, CardModel cardModel)
        {
            if (cardModel.IsFacedown && cardModel.Value.IsBackFaceCard)
                cardModel.IsFacedown = false;

            // A card added over a card zone moves into that zone, which then applies its own add actions
            if (cardModel.MoveToOverlappingCardZone())
                return;

            if (CgsNetManager.Instance.IsOnline && !cardModel.IsSpawned)
                CgsNetManager.Instance.LocalPlayer.MoveCardToServer(cardZone, cardModel);
            else
                SetPlayAreaActions(cardModel);
        }

        public static void SetPlayAreaActions(CardModel cardModel)
        {
            cardModel.DefaultAction =
                CardActionPanel.CardActionDictionary[CardGameManager.Current.GameDefaultCardAction];
            cardModel.SecondaryDragAction = cardModel.Rotate;
        }

        private void DisplayResults(string filters, List<UnityCard> cards)
        {
            var position = Vector2.left * (CardGameManager.PixelsPerInch * CardGameManager.Current.CardSize.X);
            if (CgsNetManager.Instance.IsOnline && CgsNetManager.Instance.LocalPlayer != null)
                CgsNetManager.Instance.LocalPlayer.RequestNewCardStack(filters, cards, position, Quaternion.identity,
                    false);
            else
                CreateCardStack(filters, cards, position, Quaternion.identity, false);
        }

        public CardModel CreateCardModel(GameObject container, string cardId, Vector3 position, Quaternion rotation,
            bool isFacedown, bool isCardShared, CardModelCreationOptions options = default)
        {
            if (container == null)
                container = playAreaCardZone.gameObject;
            var cardModel = Instantiate(cardModelPrefab, position, rotation, container.transform)
                .GetComponent<CardModel>();
            if (CgsNetManager.Instance.IsOnline)
            {
                if (options.OwnerClientId.HasValue)
                    cardModel.MyNetworkObject.SpawnWithOwnership(options.OwnerClientId.Value);
                else
                    cardModel.MyNetworkObject.Spawn();
            }

            cardModel.Value = CardGameManager.Current.Cards[cardId];
            cardModel.Container = container;
            cardModel.Position = position;
            cardModel.Rotation = rotation;
            if (options.SiblingIndex is >= 0)
                cardModel.SiblingIndex = options.SiblingIndex.Value;
            cardModel.IsCardShared = isCardShared;
            cardModel.IsFacedown = isFacedown;
            if (!string.IsNullOrEmpty(options.DefaultAction) &&
                Enum.TryParse<CardAction>(options.DefaultAction, out var cardAction))
                cardModel.DefaultAction = CardActionPanel.CardActionDictionary[cardAction];

            cardModel.HideHighlightClientRpc();

            return cardModel;
        }

        public void CreateDefaultDie()
        {
            if (CgsNetManager.Instance.IsOnline && CgsNetManager.Instance.LocalPlayer != null)
                CgsNetManager.Instance.LocalPlayer.RequestNewDie(Vector2.zero,
                    CgsNetManager.Instance.LocalPlayer.DefaultRotation, PlaySettings.DieFaceCount, Die.DefaultValue,
                    new Vector3(Color.white.r, Color.white.g, Color.white.b));
            else
                CreateDie(Vector2.zero, Quaternion.identity, PlaySettings.DieFaceCount, Die.DefaultValue, Color.white);
        }

        public Die CreateDie(Vector2 position, Quaternion rotation, int max, int value, Color color,
            ulong? ownerClientId = null)
        {
            var die = Instantiate(diePrefab, playAreaCardZone.transform).GetOrAddComponent<Die>();
            if (CgsNetManager.Instance.IsOnline)
            {
                if (ownerClientId.HasValue)
                    die.MyNetworkObject.SpawnWithOwnership(ownerClientId.Value);
                else
                    die.MyNetworkObject.Spawn();
            }

            die.Position = position;
            die.Rotation = rotation;
            die.Max = max;
            die.Value = value;
            die.DieColor = color;

            return die;
        }

        public void CreateDefaultCounter()
        {
            if (CgsNetManager.Instance.IsOnline && CgsNetManager.Instance.LocalPlayer != null)
                CgsNetManager.Instance.LocalPlayer.RequestNewCounter(Vector2.zero,
                    CgsNetManager.Instance.LocalPlayer.DefaultRotation, Counter.DefaultValue, Vector3.one);
            else
                CreateCounter(Vector2.zero, Quaternion.identity, Counter.DefaultValue, Color.white);
        }

        public Counter CreateCounter(Vector2 position, Quaternion rotation, int value, Color color,
            ulong? ownerClientId = null)
        {
            var counter = Instantiate(counterPrefab, playAreaCardZone.transform).GetOrAddComponent<Counter>();
            if (CgsNetManager.Instance.IsOnline)
            {
                if (ownerClientId.HasValue)
                    counter.MyNetworkObject.SpawnWithOwnership(ownerClientId.Value);
                else
                    counter.MyNetworkObject.Spawn();
            }

            counter.Position = position;
            counter.Rotation = rotation;
            counter.Value = value;
            counter.CounterColor = color;

            return counter;
        }

        private void CreateZones()
        {
            foreach (var gamePlayZone in CardGameManager.Current.GamePlayZones)
                CreateZone(gamePlayZone);
        }

        // Zones created before the network session starts (ResetPlayArea runs at scene load, before hosting begins)
        // are local-only, so the host must spawn them when it starts hosting
        public void SpawnUnspawnedZones()
        {
            foreach (var cardZone in AllCardZones)
                if (cardZone != playAreaCardZone && !cardZone.IsSpawned && cardZone.MyNetworkObject != null)
                    cardZone.MyNetworkObject.Spawn();

            foreach (var diceZone in playAreaCardZone.GetComponentsInChildren<DiceZone>())
                if (!diceZone.IsSpawned && diceZone.MyNetworkObject != null)
                    diceZone.MyNetworkObject.Spawn();
        }

        // A joining client's locally-created zones are superseded by the host's spawned zones
        public void DestroyUnspawnedZones()
        {
            foreach (var cardZone in AllCardZones)
                if (cardZone != playAreaCardZone && !cardZone.IsSpawned)
                    Destroy(cardZone.gameObject);

            foreach (var diceZone in playAreaCardZone.GetComponentsInChildren<DiceZone>())
                if (!diceZone.IsSpawned)
                    Destroy(diceZone.gameObject);
        }

        private void CreateZone(GamePlayZone gamePlayZone)
        {
            var cardAction = gamePlayZone.DefaultCardAction ?? CardGameManager.Current.GameDefaultCardAction;
            var zone = new GamePlayZoneParams
            {
                Type = gamePlayZone.Type.ToString(),
                Name = gamePlayZone.Name ?? string.Empty,
                Position = CardGameManager.PixelsPerInch *
                           new Vector2(gamePlayZone.Position.X, gamePlayZone.Position.Y),
                Rotation = Quaternion.Euler(0, 0, gamePlayZone.Rotation),
                Size = CardGameManager.PixelsPerInch *
                       new Vector2(gamePlayZone.Size.X, gamePlayZone.Size.Y),
                Face = gamePlayZone.Face.ToString(),
                Action = cardAction.ToString()
            };

            if (CgsNetManager.Instance.IsOnline && CgsNetManager.Instance.LocalPlayer != null)
                CgsNetManager.Instance.LocalPlayer.RequestNewZone(zone);
            else
                CreateZone(zone);
        }

        public CgsNetPlayable CreateZone(GamePlayZoneParams zone, ulong? ownerClientId = null)
        {
            if (Enum.TryParse(zone.Type, true, out GamePlayZoneType gamePlayZoneType))
            {
                CgsNetPlayable playable = gamePlayZoneType switch
                {
                    GamePlayZoneType.Area => CreateAreaZone(zone.Position, zone.Rotation),
                    GamePlayZoneType.Dice => CreateDiceZone(zone.Position, zone.Rotation, ownerClientId),
                    GamePlayZoneType.Horizontal => CreateHorizontalZone(zone.Position, zone.Rotation, ownerClientId),
                    GamePlayZoneType.Vertical => CreateVerticalZone(zone.Position, zone.Rotation, ownerClientId),
                    _ => CreateAreaZone(zone.Position, zone.Rotation)
                };

                playable.Position = zone.Position;
                playable.Rotation = zone.Rotation;
                playable.Size = zone.Size;

                if (playable is not CardZone cardZone)
                    return playable;

                cardZone.Name = zone.Name;
                if (Enum.TryParse(zone.Face, true, out FacePreference facePreference))
                    cardZone.DefaultFace = facePreference;
                if (Enum.TryParse(zone.Action, true, out CardAction cardAction))
                    cardZone.DefaultAction = cardAction;

                return playable;
            }

            Debug.LogError($"CreateZone failed to parse gamePlayZoneType: {zone.Type}");
            return null;
        }

        // ReSharper disable once MemberCanBeMadeStatic.Local
        private CardZone CreateAreaZone(Vector2 position, Quaternion rotation)
        {
            Debug.LogWarning($"CreateAreaZone position: {position}, rotation: {rotation}");
            return null;
        }

        private DiceZone CreateDiceZone(Vector2 position, Quaternion rotation, ulong? ownerClientId = null)
        {
            var diceZone = Instantiate(diceZonePrefab, position, rotation, playAreaCardZone.transform)
                .GetOrAddComponent<DiceZone>();
            if (CgsNetManager.Instance.IsOnline)
            {
                if (ownerClientId.HasValue)
                    diceZone.MyNetworkObject.SpawnWithOwnership(ownerClientId.Value);
                else
                    diceZone.MyNetworkObject.Spawn();
            }
            return diceZone;
        }

        private CardZone CreateHorizontalZone(Vector2 position, Quaternion rotation, ulong? ownerClientId = null)
        {
            var cardZone = Instantiate(horizontalCardZonePrefab, position, rotation, playAreaCardZone.transform)
                .GetOrAddComponent<CardZone>();
            if (CgsNetManager.Instance.IsOnline)
            {
                if (ownerClientId.HasValue)
                    cardZone.MyNetworkObject.SpawnWithOwnership(ownerClientId.Value);
                else
                    cardZone.MyNetworkObject.Spawn();
            }
            cardZone.Type = CardZoneType.Horizontal;
            return cardZone;
        }

        private CardZone CreateVerticalZone(Vector2 position, Quaternion rotation, ulong? ownerClientId = null)
        {
            var cardZone = Instantiate(verticalCardZonePrefab, position, rotation, playAreaCardZone.transform)
                .GetComponent<CardZone>();
            if (CgsNetManager.Instance.IsOnline)
            {
                if (ownerClientId.HasValue)
                    cardZone.MyNetworkObject.SpawnWithOwnership(ownerClientId.Value);
                else
                    cardZone.MyNetworkObject.Spawn();
            }
            cardZone.Type = CardZoneType.Vertical;
            return cardZone;
        }

        public void OnDrop(CardModel cardModel)
        {
            AddCardToPlayArea(playAreaCardZone, cardModel);
        }

        [UsedImplicitly]
        public void FocusPlayArea()
        {
            foreach (var stackViewer in AllCardStacks.Select(stack => stack.Viewer).Where(v => v != null && !v.IsNew))
                stackViewer.Close();
        }

        private void InputCancel(InputAction.CallbackContext context)
        {
            if (IsBlocked)
                return;

            var isAnyStackViewer = AllCardStacks.Select(stack => stack.Viewer).Any(v => v != null && !v.IsNew);
            if (isAnyStackViewer)
                FocusPlayArea();
            else if (menu.panels.activeSelf)
                menu.ToggleMenu();
            else
#if CGS_SINGLEPLAYER
                menu.ToggleFullscreen();
#else
                PromptBackToMainMenu();
#endif
        }

        [UsedImplicitly]
        public void PromptBackToMainMenu()
        {
            CardGameManager.Instance.Messenger.Prompt(MainMenuPrompt, BackToMainMenu);
        }

        [UsedImplicitly]
        public void PromptRestart()
        {
            CardGameManager.Instance.Messenger.Prompt(RestartPrompt, Restart);
        }

        public void BackToMainMenu()
        {
            StopNetworking();
            SceneManager.LoadScene(Tags.MainMenuSceneIndex);
        }

        private static void StopNetworking()
        {
            if (CgsNetManager.Instance != null)
                CgsNetManager.Instance.Stop();
        }

        private void OnDisable()
        {
            StopNetworking();

            InputSystem.actions.FindAction(Tags.PlayGameMenu).performed -= InputPlayGameMenu;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed -= InputCancel;
        }
    }
}
