/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using CardGameDef;
using CardGameDef.Unity;
using CardGameView;
using Cgs.Cards;
using Cgs.Decks;
using Cgs.Menu;
using Cgs.Play.Multiplayer;
using JetBrains.Annotations;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Cgs.Play
{
    [RequireComponent(typeof(Canvas))]
    public class PlayMode : MonoBehaviour
    {
        public const string MainMenuPrompt = "Go back to the main menu?";
        public const string RestartPrompt = "Restart?";

        public GameObject cardViewerPrefab;
        public GameObject lobbyMenuPrefab;
        public GameObject deckLoadMenuPrefab;
        public GameObject diceMenuPrefab;
        public GameObject searchMenuPrefab;
        public GameObject handDealerPrefab;

        public GameObject cardModelPrefab;
        public GameObject diePrefab;

        public CardStack playAreaCardStack;
        public PlayMenu menu;
        public PointsCounter scoreboard;

        private LobbyMenu Lobby =>
            _lobby ? _lobby : (_lobby = Instantiate(lobbyMenuPrefab).GetOrAddComponent<LobbyMenu>());

        private LobbyMenu _lobby;

        public DeckLoadMenu DeckLoader => _deckLoader
            ? _deckLoader
            : (_deckLoader = Instantiate(deckLoadMenuPrefab).GetOrAddComponent<DeckLoadMenu>());

        private DeckLoadMenu _deckLoader;

        public DiceMenu DiceManager => _diceManager
            ? _diceManager
            : (_diceManager = Instantiate(diceMenuPrefab).GetOrAddComponent<DiceMenu>());

        private DiceMenu _diceManager;

        public CardSearchMenu CardSearcher => _cardSearcher
            ? _cardSearcher
            : (_cardSearcher = Instantiate(searchMenuPrefab).GetOrAddComponent<CardSearchMenu>());

        private CardSearchMenu _cardSearcher;

        private HandDealer Dealer => _dealer
            ? _dealer
            : (_dealer = Instantiate(handDealerPrefab).GetOrAddComponent<HandDealer>());

        private HandDealer _dealer;

        private void OnEnable()
        {
            Instantiate(cardViewerPrefab);
            CardGameManager.Instance.OnSceneActions.Add(ResetPlayArea);
        }

        private void Start()
        {
            CardGameManager.Instance.CardCanvases.Add(GetComponent<Canvas>());

            playAreaCardStack.OnAddCardActions.Add(AddCardToPlay);

            if (CardGameManager.Instance.IsSearchingForServer)
                Lobby.Show();
            else
            {
                DeckLoader.Show(LoadDeck);
                CgsNetManager.Instance.GameName = CardGameManager.Current.Name;
                Lobby.Host();
            }
        }

        private void Update()
        {
            if (CardViewer.Instance.IsVisible || CardViewer.Instance.Zoom || !Input.anyKeyDown ||
                CardGameManager.Instance.ModalCanvas != null)
                return;

            if (Inputs.IsOption)
                ToggleMenu();
            else if (Inputs.IsCancel)
                PromptBackToMainMenu();
        }

        private void Restart()
        {
#if !UNITY_WEBGL
            if (CgsNetManager.Instance != null && CgsNetManager.Instance.isNetworkActive &&
                CgsNetManager.Instance.LocalPlayer != null)
                CgsNetManager.Instance.LocalPlayer.RequestRestart();
            else
            {
                ResetPlayArea();
                DeckLoader.Show(LoadDeck);
            }
#else
            ResetPlayArea();
            ShowDeckMenu();
#endif
        }

        public void ResetPlayArea()
        {
            // TODO: zones.Clear();
            var playAreaRectTransform = (RectTransform) playAreaCardStack.transform;
            playAreaRectTransform.DestroyAllChildren();
            var playAreaSize = new Vector2(CardGameManager.Current.PlayAreaSize.X,
                CardGameManager.Current.PlayAreaSize.Y);
            playAreaRectTransform.sizeDelta = playAreaSize * CardGameManager.PixelsPerInch;
        }

        [UsedImplicitly]
        public void ToggleMenu()
        {
            if (menu.gameObject.activeSelf)
                menu.Hide();
            else
                menu.Show();
        }

        public void ShowDeckMenu()
        {
            DeckLoader.Show(LoadDeck);
        }

        public void ShowCardsMenu()
        {
            CardSearcher.Show(DisplayResults);
        }

        public void ShowDiceMenu()
        {
            DiceManager.Show(CreateDie);
        }

        public void LoadDeck(UnityDeck deck)
        {
            if (deck == null)
                return;

            // TODO:
            /*
            Dictionary<string, List<Card>> extraGroups = deck.GetExtraGroups();
            foreach (KeyValuePair<string, List<Card>> cardGroup in extraGroups)
                zones.CreateExtraZone(cardGroup.Key, cardGroup.Value);*/

            List<Card> extraCards = deck.GetExtraCards();
            List<UnityCard> deckCards = deck.Cards.Where(card => !extraCards.Contains(card)).Cast<UnityCard>().ToList();
            deckCards.Shuffle();

            LoadDeckCards(deckCards);

            foreach (Card card in deck.Cards)
            foreach (GameBoardCard boardCard in CardGameManager.Current.GameBoardCards.Where(boardCard =>
                card.Id.Equals(boardCard.Card)))
                CreateGameBoards(boardCard.Boards);
        }

        public void LoadDeckCards(IEnumerable<Card> deckCards, bool isShared = false)
        {
            // TODO:zones.CreateDeck();
            // TODO:zones.scrollView.verticalScrollbar.value = 0;
            IEnumerable<Card> enumerable = deckCards.ToList();
#if !UNITY_WEBGL
            if (!isShared && CgsNetManager.Instance.LocalPlayer != null)
                CgsNetManager.Instance.LocalPlayer.RequestNewDeck(enumerable);
#endif
            // TODO:zones.CurrentDeck.Sync(enumerable);
            // TODO:StartCoroutine(zones.CurrentDeck.WaitForLoad(CreateHand));
        }

        private void CreateGameBoards(IReadOnlyCollection<GameBoard> boards)
        {
            if (boards == null || boards.Count < 1)
                return;

            foreach (GameBoard board in boards)
                CreateBoard(board);
        }

        private void CreateBoard(GameBoard board)
        {
            if (board == null)
                return;

            var newBoard = new GameObject(board.Id, typeof(RectTransform));
            var boardRectTransform = (RectTransform) newBoard.transform;
            boardRectTransform.SetParent(playAreaCardStack.transform);
            boardRectTransform.anchorMin = Vector2.zero;
            boardRectTransform.anchorMax = Vector2.zero;
            boardRectTransform.offsetMin =
                new Vector2(board.OffsetMin.X, board.OffsetMin.Y) * CardGameManager.PixelsPerInch;
            boardRectTransform.offsetMax =
                new Vector2(board.OffsetMin.X, board.OffsetMin.Y) * CardGameManager.PixelsPerInch +
                boardRectTransform.offsetMin;

            string boardFilepath = CardGameManager.Current.GameBoardsFilePath + "/" + board.Id + "." +
                                   CardGameManager.Current.GameBoardFileType;
            Sprite boardImageSprite = File.Exists(boardFilepath)
                ? UnityExtensionMethods.CreateSprite(boardFilepath)
                : null;
            if (boardImageSprite != null)
                newBoard.AddComponent<Image>().sprite = boardImageSprite;

            boardRectTransform.localScale = Vector3.one;
        }

        public void CreateHand()
        {
            // TODO: if (zones.Hand == null)
            // TODO:     zones.CreateHand();

            if (CardGameManager.Current.GameStartHandCount > 0)
                Dealer.Show(DealStartingHand);
        }

        private void DealStartingHand()
        {
            // TODO: if (zones.Hand == null)
            // TODO:    return;

            // TODO: if (!zones.Hand.IsExtended)
            // TODO:     zones.Hand.ToggleExtension();

            Deal(Dealer.Count);
        }

        private void Deal(int cardCount)
        {
            AddCardsToHand(PopDeckCards(cardCount));
        }

        private IEnumerable<UnityCard> PopDeckCards(int cardCount)
        {
            List<UnityCard> cards = new List<UnityCard>(cardCount);
            // TODO: if (zones.CurrentDeck == null)
            // TODO:    return cards;

            // TODO: for (var i = 0; i < cardCount && zones.CurrentDeck.Cards.Count > 0; i++)
            // TODO:    cards.Add(zones.CurrentDeck.PopCard());
#if !UNITY_WEBGL
            // TODO: if (CgsNetManager.Instance != null && CgsNetManager.Instance.LocalPlayer != null)
            // TODO:    CgsNetManager.Instance.LocalPlayer.RequestDeckUpdate(zones.CurrentDeck.Cards);
#endif
            return cards;
        }

        public void AddCardsToHand(IEnumerable<UnityCard> cards)
        {
            // TODO: if (zones.Hand == null)
            // TODO:    zones.CreateHand();

            // TODO: foreach (UnityCard card in cards)
            // TODO:    zones.Hand.AddCard(card);
        }

        private static void AddCardToPlay(CardStack cardStack, CardModel cardModel)
        {
            if (NetworkManager.singleton.isNetworkActive)
                CgsNetManager.Instance.LocalPlayer.MoveCardToServer(cardStack, cardModel);
            else
                SetPlayActions(cardModel);
        }

        public static void SetPlayActions(CardModel cardModel)
        {
            cardModel.DoubleClickAction = CardActions.Rotate90;
            cardModel.SecondaryDragAction = cardModel.Rotate;
        }

        public void CatchDiscard(UnityCard card)
        {
            // TODO: if (zones.Discard == null)
            // TODO:     zones.CreateDiscard();
            // TODO: zones.Discard.AddCard(card);
        }

        public void DisplayResults(string filters, List<UnityCard> cards)
        {
            // TODO: if (zones.Results == null)
            // TODO:    zones.CreateResults();
            // TODO: zones.Results.Sync(cards);
        }

        public Die CreateDie(int min, int max)
        {
            Transform target = playAreaCardStack.transform;
            var die = Instantiate(diePrefab, target.parent).GetOrAddComponent<Die>();
            die.transform.SetParent(target);
            die.Min = min;
            die.Max = max;
            return die;
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
                CgsNetManager.Instance.ListServer.Stop();
                if (NetworkServer.active)
                    NetworkManager.singleton.StopHost();
                else if (NetworkClient.isConnected)
                    NetworkManager.singleton.StopClient();
            }

            SceneManager.LoadScene(MainMenu.MainMenuSceneIndex);
        }
    }
}
