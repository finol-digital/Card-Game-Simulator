/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using CardGameDef;
using CardGameDef.Unity;
using Cgs.CardGameView;
using Cgs.CardGameView.Multiplayer;
using Cgs.CardGameView.Viewer;
using Cgs.Cards;
using Cgs.Decks;
using Cgs.Menu;
using Cgs.Play.Drawer;
using Cgs.Play.Multiplayer;
using Cgs.UI.ScrollRects;
using JetBrains.Annotations;
using Mirror;
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

        private const float DeckPositionBuffer = 50;

        public GameObject cardViewerPrefab;
        public GameObject playableViewerPrefab;
        public GameObject lobbyMenuPrefab;
        public GameObject deckLoadMenuPrefab;
        public GameObject searchMenuPrefab;
        public GameObject handDealerPrefab;

        public GameObject cardStackPrefab;
        public GameObject cardModelPrefab;
        public GameObject diePrefab;
        public GameObject decisionModalPrefab;

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
                while (AllCardStacks.Any(stack =>
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

        private LobbyMenu Lobby => _lobby ??= Instantiate(lobbyMenuPrefab).GetOrAddComponent<LobbyMenu>();

        private LobbyMenu _lobby;

        private DeckLoadMenu DeckLoader =>
            _deckLoader ??= Instantiate(deckLoadMenuPrefab).GetOrAddComponent<DeckLoadMenu>();

        private DeckLoadMenu _deckLoader;

        private CardSearchMenu CardSearcher =>
            _cardSearcher ??= Instantiate(searchMenuPrefab).GetOrAddComponent<CardSearchMenu>();

        private CardSearchMenu _cardSearcher;

        private HandDealer Dealer => _dealer ??= Instantiate(handDealerPrefab).GetOrAddComponent<HandDealer>();

        private HandDealer _dealer;

        public DecisionModal Decider =>
            _decider ??= Instantiate(decisionModalPrefab).GetOrAddComponent<DecisionModal>();

        private DecisionModal _decider;

        private CardStack _soloDeckStack;

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

            if (CardGameManager.Instance.IsSearchingForServer)
                Lobby.Show();
            else
            {
                CgsNetManager.Instance.RoomName = CardGameManager.Current.Name;
                Lobby.IsLanConnectionSource = true;
                Lobby.Host();
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
            else if (Inputs.IsOption)
                menu.ToggleMenu();
            else if (Inputs.IsCancel)
                PromptBackToMainMenu();
        }

        private void Restart()
        {
            if (CgsNetManager.Instance.isNetworkActive && CgsNetManager.Instance.LocalPlayer != null)
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
            if (!NetworkManager.singleton.isNetworkActive)
                rectTransform.DestroyAllChildren();
            else if (CgsNetManager.Instance.LocalPlayer != null && CgsNetManager.Instance.LocalPlayer.isServer)
            {
                foreach (var cardStack in playMat.GetComponentsInChildren<CardStack>())
                    NetworkServer.UnSpawn(cardStack.gameObject);
                foreach (var cardModel in playMat.GetComponentsInChildren<CardModel>())
                    NetworkServer.UnSpawn(cardModel.gameObject);
                foreach (var die in playMat.GetComponentsInChildren<Die>())
                    NetworkServer.UnSpawn(die.gameObject);
                rectTransform.DestroyAllChildren();
            }

            var size = new Vector2(CardGameManager.Current.PlayMatSize.X,
                CardGameManager.Current.PlayMatSize.Y);
            rectTransform.sizeDelta = size * CardGameManager.PixelsPerInch;
            playMat.GetComponent<Image>().sprite = CardGameManager.Current.PlayMatImageSprite;
            scoreboard.ChangePoints(CardGameManager.Current.GameStartPointsCount.ToString());
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
            if (CgsNetManager.Instance.isNetworkActive && CgsNetManager.Instance.LocalPlayer != null)
            {
                CgsNetManager.Instance.LocalPlayer.RequestNewDeck(deckName, deckCards);
                var i = 1;
                foreach (var (stackName, cards) in extraGroups)
                {
                    var position = newDeckPosition + Vector2.right *
                        (CardGameManager.PixelsPerInch * i * CardGameManager.Current.CardSize.X + DeckPositionBuffer);
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
            var playAreaTransform = playMat.transform;
            var cardStack = Instantiate(cardStackPrefab, playAreaTransform.parent).GetComponent<CardStack>();
            if (!string.IsNullOrEmpty(stackName))
                cardStack.Name = stackName;
            if (cards != null)
                cardStack.Cards = cards;
            var rectTransform = (RectTransform) cardStack.transform;
            rectTransform.SetParent(playAreaTransform);
            if (!Vector2.zero.Equals(position))
                rectTransform.localPosition = position;
            cardStack.position = rectTransform.localPosition;
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
            if (CgsNetManager.Instance.isNetworkActive && CgsNetManager.Instance.LocalPlayer != null &&
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
            if (NetworkManager.singleton.isNetworkActive)
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
            if (CgsNetManager.Instance.isNetworkActive && CgsNetManager.Instance.LocalPlayer != null)
                CgsNetManager.Instance.LocalPlayer.RequestNewCardStack(filters, cards, position);
            else
                CreateCardStack(filters, cards, position);
        }

        public void CreateDefaultDie()
        {
            if (CgsNetManager.Instance.isNetworkActive && CgsNetManager.Instance.LocalPlayer != null)
                CgsNetManager.Instance.LocalPlayer.RequestNewDie(Die.DefaultMin, Die.DefaultMax);
            else
                CreateDie(Die.DefaultMin, Die.DefaultMax);
        }

        public Die CreateDie(int min, int max)
        {
            var playAreaTransform = playMat.transform;
            var die = Instantiate(diePrefab, playAreaTransform.parent).GetOrAddComponent<Die>();
            die.transform.SetParent(playAreaTransform);
            die.Min = min;
            die.Max = max;
            return die;
        }

        public void OnDrop(CardModel cardModel)
        {
            AddCardToPlay(playMat, cardModel);
        }

        [UsedImplicitly]
        public void PromptBackToMainMenu()
        {
            CardGameManager.Instance.Messenger.Ask(MainMenuPrompt, PromptRestart, BackToMainMenu);
        }

        private void PromptRestart()
        {
            CardGameManager.Instance.Messenger.Prompt(RestartPrompt, Restart);
        }

        private static void BackToMainMenu()
        {
            if (NetworkManager.singleton.isNetworkActive)
            {
                CgsNetManager.Instance.Discovery.StopDiscovery();
                if (NetworkServer.active)
                    NetworkManager.singleton.StopHost();
                else if (NetworkClient.isConnected)
                    NetworkManager.singleton.StopClient();
            }

            SceneManager.LoadScene(MainMenu.MainMenuSceneIndex);
        }
    }
}
