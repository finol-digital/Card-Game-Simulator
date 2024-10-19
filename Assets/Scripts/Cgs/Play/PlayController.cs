/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cgs.CardGameView;
using Cgs.CardGameView.Multiplayer;
using Cgs.CardGameView.Viewer;
using Cgs.Cards;
using Cgs.Decks;
using Cgs.Menu;
using Cgs.Play.Drawer;
using Cgs.Play.Multiplayer;
using Cgs.UI.ScrollRects;
using FinolDigital.Cgs.CardGameDef;
using FinolDigital.Cgs.CardGameDef.Unity;
using JetBrains.Annotations;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityExtensionMethods;
using CardAction = FinolDigital.Cgs.CardGameDef.CardAction;

namespace Cgs.Play
{
    [RequireComponent(typeof(Canvas))]
    public class PlayController : MonoBehaviour, ICardDropHandler
    {
        public const string MainMenuPrompt = "Go back to the main menu?";
        public const string RestartPrompt = "Restart?";
        public const string DefaultStackName = "Stack";

        private const float PlayAreaBuffer = 8;
        private const float DeckPositionBuffer = 50;

        public static string LoadStartDecksAsk
        {
            get
            {
                var text = "Load ";
                var deckUrls = CardGameManager.Current.GameStartDecks;
                text += $"'{deckUrls[0].Name}'";
                for (var i = 1; i < deckUrls.Count; i++)
                    text += $", '{deckUrls[i].Name}'";
                text += "?";
                return text;
            }
        }

        public static PlayController Instance { get; private set; }

        public GameObject cardViewerPrefab;
        public GameObject playableViewerPrefab;
        public GameObject lobbyMenuPrefab;
        public GameObject playSettingsMenuPrefab;
        public GameObject deckLoadMenuPrefab;
        public GameObject searchMenuPrefab;
        public GameObject handDealerPrefab;

        public GameObject boardPrefab;
        public GameObject cardStackPrefab;
        public GameObject cardModelPrefab;
        public GameObject diePrefab;
        public GameObject tokenPrefab;

        public GameObject horizontalCardZonePrefab;
        public GameObject verticalCardZonePrefab;
        public GameObject playMatPrefab;

        public Transform stackViewers;

        public RotateZoomableScrollRect playArea;
        public CardZone playAreaCardZone;
        public Image playMatImage;
        public List<CardDropArea> playDropZones;
        public CardDrawer drawer;
        public PlayMenu menu;
        public Scoreboard scoreboard;

        public Vector2 NewDeckPosition
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
                    ((RectTransform) cardStack.deckLabel.transform.parent).rect.height * playArea.CurrentZoom;
                var cardSize = CardGameManager.PixelsPerInch * playArea.CurrentZoom *
                               new Vector2(CardGameManager.Current.CardSize.X, CardGameManager.Current.CardSize.Y);

                var up = Vector2.up * (Screen.height - cardSize.y * 2f - cardStackLabelHeight);
                var right = Vector2.right * (cardSize.x * 2f);
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform) playAreaCardZone.transform,
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

        private IEnumerable<CardStack> AllCardStacks => playAreaCardZone.GetComponentsInChildren<CardStack>();

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

        private CardStack _soloDeckStack;

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
        }

        private void Start()
        {
            CardGameManager.Instance.CardCanvases.Add(GetComponent<Canvas>());

            playAreaCardZone.OnAddCardActions.Add(AddCardToPlayArea);
            playDropZones.ForEach(dropZone => dropZone.DropHandler = this);

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

        private void Update()
        {
            if (CardViewer.Instance.IsVisible || CardViewer.Instance.Zoom || PlayableViewer.Instance.IsVisible ||
                !Input.anyKeyDown || CardGameManager.Instance.ModalCanvas != null ||
                scoreboard.nameInputField.isFocused)
                return;

            if (Inputs.IsOption && CardViewer.Instance.PreviewCardModel == null)
                menu.ToggleMenu();
            else if (Inputs.IsCancel)
                PromptBackToMainMenu();
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
            var rectTransform = (RectTransform) playAreaCardZone.transform;
            if (!NetworkManager.Singleton.IsConnectedClient)
                rectTransform.DestroyAllChildren();
            else if (CgsNetManager.Instance.IsHost)
            {
                foreach (var cardStack in playAreaCardZone.GetComponentsInChildren<CardStack>())
                    cardStack.MyNetworkObject.Despawn();
                foreach (var cardModel in playAreaCardZone.GetComponentsInChildren<CardModel>())
                    cardModel.MyNetworkObject.Despawn();
                foreach (var die in playAreaCardZone.GetComponentsInChildren<Die>())
                    die.MyNetworkObject.Despawn();
                foreach (var zone in playAreaCardZone.GetComponentsInChildren<CardZone>())
                    zone.MyNetworkObject.Despawn();
                rectTransform.DestroyAllChildren();
            }

            rectTransform.sizeDelta = new Vector2(CardGameManager.Current.PlayMatSize.X + PlayAreaBuffer,
                CardGameManager.Current.PlayMatSize.Y + PlayAreaBuffer) * CardGameManager.PixelsPerInch;

            if (!NetworkManager.Singleton.IsConnectedClient)
            {
                playMatImage = Instantiate(playMatPrefab.gameObject, playAreaCardZone.transform)
                    .GetOrAddComponent<Image>();
                var playMatRectTransform = (RectTransform) playMatImage.transform;
                playMatRectTransform.anchoredPosition = Vector2.zero;
                playMatRectTransform.sizeDelta = new Vector2(CardGameManager.Current.PlayMatSize.X,
                    CardGameManager.Current.PlayMatSize.Y) * CardGameManager.PixelsPerInch;
                playMatImage.sprite = CardGameManager.Current.PlayMatImageSprite;
                playMatImage.transform.SetAsFirstSibling();

                CreateZones();
            }

            scoreboard.ChangePoints(CardGameManager.Current.GameStartPointsCount.ToString());
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

        public void ShowPlaySettingsMenu()
        {
            Settings.Show();
        }

        public void ShowDeckMenu()
        {
            DeckLoader.Show(LoadDeck);
        }

        public void ShowCardsMenu()
        {
            CardSearcher.Show(DisplayResults);
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
            var newDeckPosition = NewDeckPosition;
            if (CgsNetManager.Instance.IsOnline && CgsNetManager.Instance.LocalPlayer != null)
            {
                var startingDeckCount = AllCardStacks.ToList().Count;
                CgsNetManager.Instance.LocalPlayer.RequestNewDeck(deckName, deckCards, false);
                var i = 1;
                foreach (var (stackName, cards) in extraGroups)
                {
                    var position = newDeckPosition +
                                   Vector2.right *
                                   (CardGameManager.PixelsPerInch * i * CardGameManager.Current.CardSize.X +
                                    DeckPositionBuffer);

                    var deckCount = startingDeckCount + i;
                    if (deckCount < CardGameManager.Current.GamePlayDeckPositions.Count)
                    {
                        var targetPosition = CardGameManager.Current.GamePlayDeckPositions[deckCount];
                        position = CardGameManager.PixelsPerInch * new Vector2(targetPosition.X, targetPosition.Y);
                    }

                    CgsNetManager.Instance.LocalPlayer.RequestNewCardStack(stackName, cards.Cast<UnityCard>().Reverse(),
                        position, CgsNetManager.Instance.LocalPlayer.DefaultRotation, false);
                    i++;
                }

                CgsNetManager.Instance.LocalPlayer.RequestDecks(i);
            }
            else
            {
                List<CardStack> cardStacks = new();
                _soloDeckStack = CreateCardStack(deckName, deckCards, newDeckPosition, Quaternion.identity, false);
                cardStacks.Add(_soloDeckStack);
                var i = 1;
                foreach (var (groupName, cards) in extraGroups)
                {
                    var position = newDeckPosition + Vector2.right *
                        (CardGameManager.PixelsPerInch * i * CardGameManager.Current.CardSize.X);

                    var deckCount = AllCardStacks.ToList().Count;
                    if (deckCount < CardGameManager.Current.GamePlayDeckPositions.Count)
                    {
                        var targetPosition = CardGameManager.Current.GamePlayDeckPositions[deckCount];
                        position = CardGameManager.PixelsPerInch * new Vector2(targetPosition.X, targetPosition.Y);
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
            var rectTransform = (RectTransform) board.transform;
            if (!Vector2.zero.Equals(position))
                rectTransform.localPosition = position;
            board.Position = rectTransform.localPosition;
            return board;
        }

        public CardStack CreateCardStack(string stackName, IReadOnlyList<UnityCard> cards, Vector2 position,
            Quaternion rotation, bool isFaceup)
        {
            var cardStack = Instantiate(cardStackPrefab, playAreaCardZone.transform).GetComponent<CardStack>();
            if (CgsNetManager.Instance.IsOnline)
                cardStack.MyNetworkObject.Spawn();

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

            var deckPlayCardsAsk = GenerateAskForDeckPlayCards(decksToCardsToPlay);
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

        private static string GenerateAskForDeckPlayCards(
            Dictionary<DeckPlayCard, Dictionary<CardStack, List<int>>> deckToCardsToPlay)
        {
            var text = string.Empty;

            foreach (var deckPlayCard in deckToCardsToPlay)
            {
                foreach (var cardStackToPlay in deckPlayCard.Value)
                {
                    var cards = cardStackToPlay.Key.Cards;
                    foreach (var cardToPlay in cardStackToPlay.Value)
                    {
                        if (string.IsNullOrEmpty(text))
                            text = "Play '" + cards[cardToPlay].Name + "'";
                        else
                            text += " and '" + cards[cardToPlay].Name + "'";
                    }
                }
            }

            if (!string.IsNullOrEmpty(text))
                text += "?";
            return text;
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
                            rotation, false);
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
            if (_soloDeckStack == null)
                return cards;

            for (var i = 0; i < count && _soloDeckStack.Cards.Count > 0; i++)
                cards.Add(CardGameManager.Current.Cards[_soloDeckStack.OwnerPopCard()]);
            return cards;
        }

        private static void AddCardToPlayArea(CardZone cardZone, CardModel cardModel)
        {
            if (cardModel.IsFacedown && cardModel.Value.IsBackFaceCard)
                cardModel.IsFacedown = false;

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
            bool isFacedown, string defaultAction = "")
        {
            if (container == null)
                container = playAreaCardZone.gameObject;
            var cardModel = Instantiate(cardModelPrefab, position, rotation, container.transform)
                .GetComponent<CardModel>();
            if (CgsNetManager.Instance.IsOnline)
                cardModel.MyNetworkObject.Spawn();

            cardModel.Value = CardGameManager.Current.Cards[cardId];
            cardModel.Container = container;
            cardModel.Position = position;
            cardModel.Rotation = rotation;
            cardModel.IsFacedown = isFacedown;
            if (Enum.TryParse<CardAction>(defaultAction, out var cardAction))
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

        public Die CreateDie(Vector2 position, Quaternion rotation, int max, int value, Color color)
        {
            var die = Instantiate(diePrefab, playAreaCardZone.transform).GetOrAddComponent<Die>();
            if (CgsNetManager.Instance.IsOnline)
                die.MyNetworkObject.Spawn();

            die.Position = position;
            die.Rotation = rotation;
            die.Max = max;
            die.Value = value;
            die.DieColor = color;

            return die;
        }

        public void CreateDefaultToken()
        {
            if (CgsNetManager.Instance.IsOnline && CgsNetManager.Instance.LocalPlayer != null)
                CgsNetManager.Instance.LocalPlayer.RequestNewToken(Vector2.zero,
                    CgsNetManager.Instance.LocalPlayer.DefaultRotation);
            else
                CreateToken(Vector2.zero, Quaternion.identity);
        }

        public Token CreateToken(Vector2 position, Quaternion rotation)
        {
            var token = Instantiate(tokenPrefab, playAreaCardZone.transform).GetOrAddComponent<Token>();
            if (CgsNetManager.Instance.IsOnline)
                token.MyNetworkObject.Spawn();

            token.Position = position;
            token.Rotation = rotation;

            return token;
        }

        private void CreateZones()
        {
            foreach (var gamePlayZone in CardGameManager.Current.GamePlayZones)
                CreateZone(gamePlayZone);
        }

        private void CreateZone(GamePlayZone gamePlayZone)
        {
            var position = CardGameManager.PixelsPerInch *
                           new Vector2(gamePlayZone.Position.X, gamePlayZone.Position.Y);
            var rotation = Quaternion.Euler(0, 0, gamePlayZone.Rotation);
            var size = CardGameManager.PixelsPerInch *
                       new Vector2(gamePlayZone.Size.X, gamePlayZone.Size.Y);
            var cardAction = gamePlayZone.DefaultCardAction ?? CardGameManager.Current.GameDefaultCardAction;

            if (CgsNetManager.Instance.IsOnline && CgsNetManager.Instance.LocalPlayer != null)
                CgsNetManager.Instance.LocalPlayer.RequestNewZone(gamePlayZone.Type.ToString(), position,
                    rotation, size, gamePlayZone.Face.ToString(), cardAction.ToString());
            else
                CreateZone(gamePlayZone.Type.ToString(), position, rotation, size,
                    gamePlayZone.Face.ToString(), cardAction.ToString());
        }

        public CardZone CreateZone(string type, Vector2 position, Quaternion rotation, Vector2 size, string face,
            string action)
        {
            if (Enum.TryParse(type, true, out GamePlayZoneType gamePlayZoneType)
                && Enum.TryParse(face, true, out FacePreference facePreference)
                && Enum.TryParse(action, true, out CardAction cardAction))
            {
                var cardZone = gamePlayZoneType switch
                {
                    GamePlayZoneType.Area => CreateAreaZone(position, rotation),
                    GamePlayZoneType.Horizontal => CreateHorizontalZone(position, rotation),
                    GamePlayZoneType.Vertical => CreateVerticalZone(position, rotation),
                    _ => CreateAreaZone(position, rotation)
                };

                var rectTransform = (RectTransform) cardZone.transform;
                rectTransform.anchorMin = 0.5f * Vector2.one;
                rectTransform.anchorMax = 0.5f * Vector2.one;
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.localPosition = position;
                rectTransform.sizeDelta = size;

                cardZone.Position = position;
                cardZone.Rotation = rotation;
                cardZone.Size = size;
                cardZone.DefaultFace = facePreference;
                cardZone.DefaultAction = cardAction;

                return cardZone;
            }

            Debug.LogError($"CreateZone failed to parse type: {type}, face: {face}, action: {action}");
            return null;
        }

        // ReSharper disable once MemberCanBeMadeStatic.Local
        private CardZone CreateAreaZone(Vector2 position, Quaternion rotation)
        {
            Debug.LogWarning($"CreateAreaZone position: {position}, rotation: {rotation}");
            return null;
        }

        private CardZone CreateHorizontalZone(Vector2 position, Quaternion rotation)
        {
            var cardZone = Instantiate(horizontalCardZonePrefab, position, rotation, playAreaCardZone.transform)
                .GetOrAddComponent<CardZone>();
            if (CgsNetManager.Instance.IsOnline)
                cardZone.MyNetworkObject.Spawn();
            cardZone.Type = CardZoneType.Horizontal;
            return cardZone;
        }

        private CardZone CreateVerticalZone(Vector2 position, Quaternion rotation)
        {
            var cardZone = Instantiate(verticalCardZonePrefab, position, rotation, playAreaCardZone.transform)
                .GetComponent<CardZone>();
            if (CgsNetManager.Instance.IsOnline)
                cardZone.MyNetworkObject.Spawn();
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
            SceneManager.LoadScene(MainMenu.MainMenuSceneIndex);
        }

        private static void StopNetworking()
        {
            if (CgsNetManager.Instance != null)
                CgsNetManager.Instance.Stop();
        }

        private void OnDisable()
        {
            StopNetworking();
        }
    }
}
