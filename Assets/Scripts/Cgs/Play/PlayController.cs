/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

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

namespace Cgs.Play
{
    [RequireComponent(typeof(Canvas))]
    public class PlayController : MonoBehaviour, ICardDropHandler
    {
        public const string MainMenuPrompt = "Go back to the main menu?";
        public const string RestartPrompt = "Restart?";
        public const string DefaultStackName = "Stack";

        private const float DeckPositionBuffer = 50;

        public static PlayController Instance { get; private set; }

        public GameObject cardViewerPrefab;
        public GameObject playableViewerPrefab;
        public GameObject lobbyMenuPrefab;
        public GameObject playSettingsMenuPrefab;
        public GameObject deckLoadMenuPrefab;
        public GameObject searchMenuPrefab;
        public GameObject handDealerPrefab;

        public GameObject cardStackPrefab;
        public GameObject cardModelPrefab;
        public GameObject diePrefab;
        public GameObject tokenPrefab;

        public Transform stackViewers;

        public RotateZoomableScrollRect playArea;
        public CardZone playMat;
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

                var up = Vector2.up * (Screen.height - cardSize.y - cardStackLabelHeight);
                var right = Vector2.right * (cardSize.x / 2f);
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform) playMat.transform, (up + right),
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

        private IEnumerable<CardStack> AllCardStacks => playMat.GetComponentsInChildren<CardStack>();

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
            CardGameManager.Instance.OnSceneActions.Add(ResetPlayArea);
        }

        private void Start()
        {
            CardGameManager.Instance.CardCanvases.Add(GetComponent<Canvas>());

            playArea.CurrentZoom = RotateZoomableScrollRect.MinZoom;
            playMat.OnAddCardActions.Add(AddCardToPlay);
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
            var rectTransform = (RectTransform) playMat.transform;
            if (!NetworkManager.Singleton.IsConnectedClient)
                rectTransform.DestroyAllChildren();
            else if (CgsNetManager.Instance.IsHost)
            {
                foreach (var cardStack in playMat.GetComponentsInChildren<CardStack>())
                    cardStack.MyNetworkObject.Despawn();
                foreach (var cardModel in playMat.GetComponentsInChildren<CardModel>())
                    cardModel.MyNetworkObject.Despawn();
                foreach (var die in playMat.GetComponentsInChildren<Die>())
                    die.MyNetworkObject.Despawn();
                rectTransform.DestroyAllChildren();
            }

            var size = new Vector2(CardGameManager.Current.PlayMatSize.X,
                CardGameManager.Current.PlayMatSize.Y);
            rectTransform.sizeDelta = size * CardGameManager.PixelsPerInch;
            playMat.GetComponent<Image>().sprite = CardGameManager.Current.PlayMatImageSprite;
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
                CgsNetManager.Instance.LocalPlayer.RequestNewDeck(deckName, deckCards);
                var i = 1;
                foreach (var (stackName, cards) in extraGroups)
                {
                    var position = newDeckPosition +
                                   Vector2.right *
                                   (CardGameManager.PixelsPerInch * i * CardGameManager.Current.CardSize.X +
                                    DeckPositionBuffer);

                    var deckCount = AllCardStacks.ToList().Count;
                    if (deckCount >= 0  && deckCount < CardGameManager.Current.GamePlayDeckPositions.Count)
                    {
                        var targetPosition = CardGameManager.Current.GamePlayDeckPositions[deckCount];
                        position = CardGameManager.PixelsPerInch * new Vector2(targetPosition.X, targetPosition.Y);
                    }

                    CgsNetManager.Instance.LocalPlayer.RequestNewCardStack(stackName, cards.Cast<UnityCard>().Reverse(),
                        position);
                    i++;
                }
            }
            else
            {
                _soloDeckStack = CreateCardStack(deckName, deckCards, newDeckPosition);
                var i = 1;
                foreach (var (groupName, cards) in extraGroups)
                {
                    var position = newDeckPosition + Vector2.right *
                        (CardGameManager.PixelsPerInch * i * CardGameManager.Current.CardSize.X);

                    var deckCount = AllCardStacks.ToList().Count;
                    if (deckCount >= 0  && deckCount < CardGameManager.Current.GamePlayDeckPositions.Count)
                    {
                        var targetPosition = CardGameManager.Current.GamePlayDeckPositions[deckCount];
                        position = CardGameManager.PixelsPerInch * new Vector2(targetPosition.X, targetPosition.Y);
                    }

                    CreateCardStack(groupName, cards.Cast<UnityCard>().Reverse().ToList(), position);
                    i++;
                }
            }

            PromptForHand();
        }

        private void CreateGameBoards(IEnumerable<GameBoard> gameBoards)
        {
            foreach (var gameBoard in gameBoards)
                CreateGameBoard(gameBoard);
        }

        private void CreateGameBoard(GameBoard board)
        {
            var newBoardGameObject = new GameObject(board.Id, typeof(RectTransform));
            var boardRectTransform = (RectTransform) newBoardGameObject.transform;
            boardRectTransform.SetParent(playMat.transform);
            boardRectTransform.anchorMin = Vector2.zero;
            boardRectTransform.anchorMax = Vector2.zero;
            boardRectTransform.offsetMin =
                new Vector2(board.OffsetMin.X, board.OffsetMin.Y) * CardGameManager.PixelsPerInch;
            boardRectTransform.offsetMax =
                new Vector2(board.OffsetMin.X, board.OffsetMin.Y) * CardGameManager.PixelsPerInch +
                boardRectTransform.offsetMin;

            var boardFilepath = CardGameManager.Current.GameBoardsDirectoryPath + "/" + board.Id + "." +
                                CardGameManager.Current.GameBoardImageFileType;
            var boardImageSprite = File.Exists(boardFilepath)
                ? UnityFileMethods.CreateSprite(boardFilepath)
                : null;
            if (boardImageSprite != null)
                newBoardGameObject.AddComponent<Image>().sprite = boardImageSprite;

            boardRectTransform.localScale = Vector3.one;
        }

        public CardStack CreateCardStack(string stackName, IReadOnlyList<UnityCard> cards, Vector2 position)
        {
            var cardStack = Instantiate(cardStackPrefab, playMat.transform).GetComponent<CardStack>();
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
                CgsNetManager.Instance.LocalPlayer.RequestNewCardStack(filters, cards, position);
            else
                CreateCardStack(filters, cards, position);
        }

        public void CreateCardModel(string cardId, Vector3 position, Quaternion rotation, bool isFacedown)
        {
            var cardModel = Instantiate(cardModelPrefab, playMat.transform).GetComponent<CardModel>();
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
            var die = Instantiate(diePrefab, playMat.transform).GetOrAddComponent<Die>();
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
            var token = Instantiate(tokenPrefab, playMat.transform).GetOrAddComponent<Token>();
            if (CgsNetManager.Instance.IsOnline)
                token.MyNetworkObject.Spawn();
            var rectTransform = (RectTransform) token.transform;
            rectTransform.localPosition = Vector2.zero;
            token.Position = rectTransform.localPosition;
            return token;
        }

        private void CreateZone(GamePlayZone gamePlayZone)
        {

        }

        public void OnDrop(CardModel cardModel)
        {
            AddCardToPlay(playMat, cardModel);
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
