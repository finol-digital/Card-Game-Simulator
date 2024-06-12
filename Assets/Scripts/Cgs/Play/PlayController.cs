/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
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
using CardAction = Cgs.CardGameView.Viewer.CardAction;

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

                var up = Vector2.up * (Screen.height - cardSize.y  * 2f - cardStackLabelHeight);
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
            CardViewer.Instance.GetComponent<CardActions>().Show();
            if (Menu.Settings.PreviewOnMouseOver)
                CardViewer.Instance.Mode = CardViewerMode.Expanded;
            CardGameManager.Instance.OnSceneActions.Add(ResetPlayArea);
        }

        private void Start()
        {
            CardGameManager.Instance.CardCanvases.Add(GetComponent<Canvas>());

            playAreaCardZone.OnAddCardActions.Add(AddCardToPlay);
            playDropZones.ForEach(dropZone => dropZone.DropHandler = this);

            CardGameManager.Current.GamePlayZones.ForEach(CreateZone);

            if (CardGameManager.Instance.IsSearchingForServer)
                Lobby.Show();
            else
            {
                Lobby.IsLanConnectionSource = true;
#if !UNITY_WEBGL
                Lobby.Host();
#endif
                DeckLoader.Show(LoadDeck);
            }
        }

        private void Update()
        {
            if (CardViewer.Instance.IsVisible || CardViewer.Instance.Zoom || PlayableViewer.Instance.IsVisible ||
                !Input.anyKeyDown || CardGameManager.Instance.ModalCanvas != null ||
                scoreboard.nameInputField.isFocused)
                return;

            if (Inputs.IsFocusBack && !Inputs.WasFocusBack)
                Deal(1);
            else if (Inputs.IsOption && CardViewer.Instance.PreviewCardModel == null)
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
                _soloDeckStack = null;
                DeckLoader.Show(LoadDeck);
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
                rectTransform.DestroyAllChildren();
            }

            rectTransform.sizeDelta = new Vector2(CardGameManager.Current.PlayMatSize.X + PlayAreaBuffer,
                CardGameManager.Current.PlayMatSize.Y + PlayAreaBuffer) * CardGameManager.PixelsPerInch;

            if (!NetworkManager.Singleton.IsConnectedClient)
            {
                playMatImage = Instantiate(playMatPrefab.gameObject, playAreaCardZone.transform).GetOrAddComponent<Image>();
                var playMatRectTransform = (RectTransform) playMatImage.transform;
                playMatRectTransform.anchoredPosition = Vector2.zero;
                playMatRectTransform.sizeDelta = new Vector2(CardGameManager.Current.PlayMatSize.X,
                    CardGameManager.Current.PlayMatSize.Y) * CardGameManager.PixelsPerInch;
                playMatImage.sprite = CardGameManager.Current.PlayMatImageSprite;
                playMatImage.transform.SetAsFirstSibling();
            }

            scoreboard.ChangePoints(CardGameManager.Current.GameStartPointsCount.ToString());
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
                CgsNetManager.Instance.LocalPlayer.RequestNewDeck(deckName, deckCards, false);
                var i = 1;
                foreach (var (stackName, cards) in extraGroups)
                {
                    var position = newDeckPosition +
                                   Vector2.right *
                                   (CardGameManager.PixelsPerInch * i * CardGameManager.Current.CardSize.X +
                                    DeckPositionBuffer);

                    var deckCount = AllCardStacks.ToList().Count + i;
                    if (deckCount < CardGameManager.Current.GamePlayDeckPositions.Count)
                    {
                        var targetPosition = CardGameManager.Current.GamePlayDeckPositions[deckCount];
                        position = CardGameManager.PixelsPerInch * new Vector2(targetPosition.X, targetPosition.Y);
                    }

                    CgsNetManager.Instance.LocalPlayer.RequestNewCardStack(stackName, cards.Cast<UnityCard>().Reverse(),
                        position, CgsNetManager.Instance.LocalPlayer.DefaultRotation, false);
                    i++;
                }
            }
            else
            {
                _soloDeckStack = CreateCardStack(deckName, deckCards, newDeckPosition, Quaternion.identity, false);
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

                    CreateCardStack(groupName, cards.Cast<UnityCard>().Reverse().ToList(), position,
                        Quaternion.identity, false);
                    i++;
                }
            }

            PromptForHand();
        }

        private void CreateGameBoards(IEnumerable<GameBoard> gameBoards)
        {
            foreach (var gameBoard in gameBoards)
            {
                var size = new Vector2(gameBoard.Size.X, gameBoard.Size.Y);
                var position = new Vector2(gameBoard.OffsetMin.X, gameBoard.OffsetMin.Y);
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
            var rectTransform = (RectTransform) cardStack.transform;
            if (!Vector2.zero.Equals(position))
                rectTransform.localPosition = position;
            cardStack.Position = rectTransform.localPosition;
            if (!Quaternion.identity.Equals(rotation))
                rectTransform.localRotation = rotation;
            cardStack.Rotation = rectTransform.localRotation;
            if (isFaceup)
                cardStack.IsTopFaceup = true;
            return cardStack;
        }

        public void PromptForHand()
        {
            if (CardGameManager.Current.GameStartHandCount > 0)
                Dealer.Show(DealStartingHand);
        }

        private void DealStartingHand()
        {
            drawer.SemiShow();
            Deal(Dealer.Count);
        }

        private void Deal(int cardCount)
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
                cards.Add(CardGameManager.Current.Cards[_soloDeckStack.PopCard()]);
            return cards;
        }

        private static void AddCardToPlay(CardZone cardZone, CardModel cardModel)
        {
            if (CgsNetManager.Instance.IsOnline)
                CgsNetManager.Instance.LocalPlayer.MoveCardToServer(cardZone, cardModel);
            else
                SetPlayActions(cardModel);
        }

        public static void SetPlayActions(CardModel cardModel)
        {
            cardModel.DefaultAction = CardActions.ActionsDictionary[CardGameManager.Current.GameDefaultCardAction];
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

        public void CreateCardModel(string cardId, Vector3 position, Quaternion rotation, bool isFacedown)
        {
            var cardModel = Instantiate(cardModelPrefab, playAreaCardZone.transform).GetComponent<CardModel>();
            if (CgsNetManager.Instance.IsOnline)
                cardModel.MyNetworkObject.Spawn();
            cardModel.Value = CardGameManager.Current.Cards[cardId];
            cardModel.Position = position;
            cardModel.Rotation = rotation;
            cardModel.IsFacedown = isFacedown;
            SetPlayActions(cardModel);
            cardModel.HideHighlightClientRpc();
        }

        public void CreateDefaultDie()
        {
            if (CgsNetManager.Instance.IsOnline && CgsNetManager.Instance.LocalPlayer != null)
                CgsNetManager.Instance.LocalPlayer.RequestNewDie(Die.DefaultMin, PlaySettings.DieFaceCount);
            else
                CreateDie(Die.DefaultMin, PlaySettings.DieFaceCount);
        }

        public Die CreateDie(int min, int max)
        {
            var die = Instantiate(diePrefab, playAreaCardZone.transform).GetOrAddComponent<Die>();
            if (CgsNetManager.Instance.IsOnline)
                die.MyNetworkObject.Spawn();
            die.Min = min;
            die.Max = max;
            var rectTransform = (RectTransform) die.transform;
            rectTransform.localPosition = Vector2.zero;
            die.Position = rectTransform.localPosition;
            return die;
        }

        public void CreateDefaultToken()
        {
            if (CgsNetManager.Instance.IsOnline && CgsNetManager.Instance.LocalPlayer != null)
                CgsNetManager.Instance.LocalPlayer.RequestNewToken();
            else
                CreateToken();
        }

        public Token CreateToken()
        {
            var token = Instantiate(tokenPrefab, playAreaCardZone.transform).GetOrAddComponent<Token>();
            if (CgsNetManager.Instance.IsOnline)
                token.MyNetworkObject.Spawn();
            var rectTransform = (RectTransform) token.transform;
            rectTransform.localPosition = Vector2.zero;
            token.Position = rectTransform.localPosition;
            return token;
        }

        private void CreateZone(GamePlayZone gamePlayZone)
        {
            var position = CardGameManager.PixelsPerInch *
                           new Vector2(gamePlayZone.Position.X, gamePlayZone.Position.Y);
            var size = CardGameManager.PixelsPerInch *
                       new Vector2(gamePlayZone.Size.X, gamePlayZone.Size.Y);
            var cardAction = gamePlayZone.DefaultCardAction != null
                ? CardActions.ActionsDictionary[gamePlayZone.DefaultCardAction.Value]
                : CardActions.ActionsDictionary[CardGameManager.Current.GameDefaultCardAction];
            switch (gamePlayZone.Type)
            {
                case GamePlayZoneType.Area:
                    CreateAreaZone(position, size, gamePlayZone.Face, cardAction);
                    break;
                case GamePlayZoneType.Horizontal:
                    CreateHorizontalZone(position, size, gamePlayZone.Face, cardAction);
                    break;
                case GamePlayZoneType.Vertical:
                    CreateVerticalZone(position, size, gamePlayZone.Face, cardAction);
                    break;
                default:
                    CreateAreaZone(position, size, gamePlayZone.Face, cardAction);
                    break;
            }
        }

        private void CreateAreaZone(Vector2 position, Vector2 size, FacePreference facePreference,
            CardAction cardAction)
        {
            Debug.Log(
                $"CreateAreaZone position: {position}, size: {size}, face: {facePreference}, cardAction: {cardAction}");
        }

        private void CreateHorizontalZone(Vector2 position, Vector2 size, FacePreference facePreference,
            CardAction cardAction)
        {
            var cardZone = Instantiate(horizontalCardZonePrefab, playAreaCardZone.transform)
                .GetOrAddComponent<CardZone>();
            var cardZoneRectTransform = (RectTransform) cardZone.transform;
            cardZoneRectTransform.anchorMin = 0.5f * Vector2.one;
            cardZoneRectTransform.anchorMax = 0.5f * Vector2.one;
            cardZoneRectTransform.anchoredPosition = Vector2.zero;
            cardZoneRectTransform.localPosition = position;
            cardZoneRectTransform.sizeDelta = size;

            cardZone.type = CardZoneType.Horizontal;
            cardZone.allowsFlip = true;
            cardZone.allowsRotation = true;
            cardZone.scrollRectContainer = playArea;
            cardZone.DoesImmediatelyRelease = true;

            var spacing = PlaySettings.StackViewerOverlap switch
            {
                2 => StackViewer.HighOverlapSpacing,
                1 => StackViewer.LowOverlapSpacing,
                _ => StackViewer.NoOverlapSpacing
            };

            cardZone.GetComponent<HorizontalLayoutGroup>().spacing = spacing;

            switch (facePreference)
            {
                case FacePreference.Any:
                    cardZone.OnAddCardActions.Add(OnAddCardModel);
                    break;
                case FacePreference.Down:
                    cardZone.OnAddCardActions.Add(OnAddCardModelFaceDown);
                    break;
                case FacePreference.Up:
                    cardZone.OnAddCardActions.Add(OnAddCardModelFaceUp);
                    break;
                default:
                    cardZone.OnAddCardActions.Add(OnAddCardModel);
                    break;
            }

            cardZone.OnAddCardActions.Add((_, cardModel) => cardModel.DefaultAction = cardAction);
        }

        private void CreateVerticalZone(Vector2 position, Vector2 size, FacePreference facePreference,
            CardAction cardAction)
        {
            var cardZone = Instantiate(verticalCardZonePrefab, playAreaCardZone.transform).GetComponent<CardZone>();
            var cardZoneRectTransform = (RectTransform) cardZone.transform;
            cardZoneRectTransform.anchorMin = 0.5f * Vector2.one;
            cardZoneRectTransform.anchorMax = 0.5f * Vector2.one;
            cardZoneRectTransform.anchoredPosition = Vector2.zero;
            cardZoneRectTransform.localPosition = position;
            cardZoneRectTransform.sizeDelta = size;

            cardZone.type = CardZoneType.Vertical;
            cardZone.allowsFlip = true;
            cardZone.allowsRotation = true;
            cardZone.scrollRectContainer = playArea;
            cardZone.DoesImmediatelyRelease = true;

            var spacing = PlaySettings.StackViewerOverlap switch
            {
                2 => StackViewer.HighOverlapSpacing,
                1 => StackViewer.LowOverlapSpacing,
                _ => StackViewer.NoOverlapSpacing
            };

            cardZone.GetComponent<VerticalLayoutGroup>().spacing = spacing;

            switch (facePreference)
            {
                case FacePreference.Any:
                    cardZone.OnAddCardActions.Add(OnAddCardModel);
                    break;
                case FacePreference.Down:
                    cardZone.OnAddCardActions.Add(OnAddCardModelFaceDown);
                    break;
                case FacePreference.Up:
                    cardZone.OnAddCardActions.Add(OnAddCardModelFaceUp);
                    break;
                default:
                    cardZone.OnAddCardActions.Add(OnAddCardModel);
                    break;
            }

            cardZone.OnAddCardActions.Add((_, cardModel) => cardModel.DefaultAction = cardAction);
        }

        private static void OnAddCardModel(CardZone cardZone, CardModel cardModel)
        {
            if (cardZone == null || cardModel == null)
                return;

            cardModel.SecondaryDragAction = cardModel.UpdateParentCardZoneScrollRect;
            cardModel.DefaultAction = CardActions.ActionsDictionary[CardGameManager.Current.GameDefaultCardAction];
        }

        private static void OnAddCardModelFaceDown(CardZone cardZone, CardModel cardModel)
        {
            if (cardZone == null || cardModel == null)
                return;

            cardModel.SecondaryDragAction = cardModel.UpdateParentCardZoneScrollRect;
            cardModel.DefaultAction = CardActions.ActionsDictionary[CardGameManager.Current.GameDefaultCardAction];
            cardModel.IsFacedown = true;
        }

        private static void OnAddCardModelFaceUp(CardZone cardZone, CardModel cardModel)
        {
            if (cardZone == null || cardModel == null)
                return;

            cardModel.SecondaryDragAction = cardModel.UpdateParentCardZoneScrollRect;
            cardModel.DefaultAction = CardActions.ActionsDictionary[CardGameManager.Current.GameDefaultCardAction];
            cardModel.IsFacedown = false;
        }

        public void OnDrop(CardModel cardModel)
        {
            AddCardToPlay(playAreaCardZone, cardModel);
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

        private void StopNetworking()
        {
            if (Lobby != null && Lobby.discovery != null)
                Lobby.discovery.StopDiscovery();
            if (CgsNetManager.Instance != null)
                CgsNetManager.Instance.Stop();
        }

        private void OnDisable()
        {
            StopNetworking();
        }
    }
}
