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
    public class PlayController : MonoBehaviour
    {
        public const string MainMenuPrompt = "Go back to the main menu?";
        public const string RestartPrompt = "Restart?";

        public GameObject cardViewerPrefab;
        public GameObject lobbyMenuPrefab;
        public GameObject deckLoadMenuPrefab;
        public GameObject diceMenuPrefab;
        public GameObject searchMenuPrefab;
        public GameObject handDealerPrefab;

        public GameObject cardZonePrefab;
        public GameObject cardModelPrefab;
        public GameObject diePrefab;

        public CardStack playAreaCardStack;
        public ZoneViewer hand;
        public PlayMenu menu;
        public PointsCounter scoreboard;

        private LobbyMenu Lobby =>
            _lobby ? _lobby : _lobby = Instantiate(lobbyMenuPrefab).GetOrAddComponent<LobbyMenu>();

        private LobbyMenu _lobby;

        private DeckLoadMenu DeckLoader => _deckLoader
            ? _deckLoader
            : _deckLoader = Instantiate(deckLoadMenuPrefab).GetOrAddComponent<DeckLoadMenu>();

        private DeckLoadMenu _deckLoader;

        private DiceMenu DiceManager => _diceManager
            ? _diceManager
            : _diceManager = Instantiate(diceMenuPrefab).GetOrAddComponent<DiceMenu>();

        private DiceMenu _diceManager;

        private CardSearchMenu CardSearcher => _cardSearcher
            ? _cardSearcher
            : _cardSearcher = Instantiate(searchMenuPrefab).GetOrAddComponent<CardSearchMenu>();

        private CardSearchMenu _cardSearcher;

        private HandDealer Dealer => _dealer
            ? _dealer
            : _dealer = Instantiate(handDealerPrefab).GetOrAddComponent<HandDealer>();

        private HandDealer _dealer;

        private CardZone _soloDeckZone;

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
            if (CgsNetManager.Instance.isNetworkActive && CgsNetManager.Instance.LocalPlayer != null)
                CgsNetManager.Instance.LocalPlayer.RequestRestart();
            else
            {
                ResetPlayArea();
                hand.Clear();
                _soloDeckZone = null;
                DeckLoader.Show(LoadDeck);
            }
        }

        public void ResetPlayArea()
        {
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

        private void LoadDeck(UnityDeck deck)
        {
            foreach (Card card in deck.Cards)
            foreach (GameBoardCard boardCard in CardGameManager.Current.GameBoardCards.Where(boardCard =>
                card.Id.Equals(boardCard.Card)))
                CreateGameBoards(boardCard.Boards);

            Dictionary<string, List<Card>> extraGroups = deck.GetExtraGroups();
            List<Card> extraCards = deck.GetExtraCards();
            List<UnityCard> deckCards = deck.Cards.Where(card => !extraCards.Contains(card)).Cast<UnityCard>().ToList();
            deckCards.Shuffle();

            if (CgsNetManager.Instance.isNetworkActive && CgsNetManager.Instance.LocalPlayer != null)
            {
                CgsNetManager.Instance.LocalPlayer.RequestNewDeck(CardGameManager.Current.GamePlayDeckName, deckCards);
                foreach (KeyValuePair<string, List<Card>> cardGroup in extraGroups)
                    CgsNetManager.Instance.LocalPlayer.RequestNewZone(cardGroup.Key, cardGroup.Value.Cast<UnityCard>());
            }
            else
            {
                _soloDeckZone = CreateZone(Vector2.zero);
                _soloDeckZone.Name = CardGameManager.Current.GamePlayDeckName;
                _soloDeckZone.Cards = deckCards;

                foreach (KeyValuePair<string, List<Card>> cardGroup in extraGroups)
                {
                    CardZone zone = CreateZone(Vector2.zero); // TODO: DYNAMIC LOCATION
                    zone.Name = cardGroup.Key;
                    zone.Cards = (List<UnityCard>) cardGroup.Value.Cast<UnityCard>();
                }
            }

            PromptForHand();
        }

        private void CreateGameBoards(IReadOnlyCollection<GameBoard> boards)
        {
            foreach (GameBoard board in boards)
                CreateBoard(board);
        }

        private void CreateBoard(GameBoard board)
        {
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

        public CardZone CreateZone(Vector2 position)
        {
            Transform target = playAreaCardStack.transform;
            var cardZone = Instantiate(cardZonePrefab, target.parent).GetComponent<CardZone>();
            var rectTransform = (RectTransform) cardZone.transform;
            rectTransform.SetParent(target);
            if (!Vector2.zero.Equals(position))
                rectTransform.anchoredPosition = position;
            return cardZone;
        }

        private void PromptForHand()
        {
            if (CardGameManager.Current.GameStartHandCount > 0)
                Dealer.Show(DealStartingHand);
        }

        private void DealStartingHand()
        {
            hand.Show();
            Deal(Dealer.Count);
        }

        private void Deal(int cardCount)
        {
            AddCardsToHand(PopDeckCards(cardCount));
        }

        private IEnumerable<UnityCard> PopDeckCards(int cardCount)
        {
            List<UnityCard> cards = new List<UnityCard>(cardCount);
            CardZone deckZone = _soloDeckZone;
            if (CgsNetManager.Instance.isNetworkActive && CgsNetManager.Instance.LocalPlayer != null &&
                CgsNetManager.Instance.LocalPlayer.DeckZone != null)
                deckZone = CgsNetManager.Instance.LocalPlayer.DeckZone.GetComponent<CardZone>();
            if (deckZone == null)
                return cards;

            for (var i = 0; i < cardCount && deckZone.Cards.Count > 0; i++)
                cards.Add(deckZone.PopCard());
            return cards;
        }

        private void AddCardsToHand(IEnumerable<UnityCard> cards)
        {
            foreach (UnityCard card in cards)
                hand.AddCard(card);
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

        private void DisplayResults(string filters, List<UnityCard> cards)
        {
            // TODO: CreateZone(CardGameManager.Current.Name, cards);
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
