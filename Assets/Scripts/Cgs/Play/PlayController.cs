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
using Cgs.Cards;
using Cgs.Decks;
using Cgs.Menu;
using Cgs.Play.Multiplayer;
using JetBrains.Annotations;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityExtensionMethods;

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

        public GameObject cardStackPrefab;
        public GameObject cardModelPrefab;
        public GameObject diePrefab;

        public Transform stackViewers;

        public CardZone playArea;
        public HandController hand;
        public PlayMenu menu;
        public PointsCounter scoreboard;

        public Vector2 NextDeckPosition { get; private set; } = Vector2.zero;

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

        private CardStack _soloDeckStack;

        private void OnEnable()
        {
            Instantiate(cardViewerPrefab);
            CardGameManager.Instance.OnSceneActions.Add(ResetPlayArea);
        }

        private void Start()
        {
            CardGameManager.Instance.CardCanvases.Add(GetComponent<Canvas>());

            playArea.OnAddCardActions.Add(AddCardToPlay);

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
                _soloDeckStack = null;
                DeckLoader.Show(LoadDeck);
            }
        }

        public void ResetPlayArea()
        {
            var rectTransform = (RectTransform) playArea.transform;
            rectTransform.DestroyAllChildren();
            var size = new Vector2(CardGameManager.Current.PlayMatSize.X,
                CardGameManager.Current.PlayMatSize.Y);
            rectTransform.sizeDelta = size * CardGameManager.PixelsPerInch;
            playArea.GetComponent<Image>().sprite = CardGameManager.Current.PlayMatImageSprite;
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

            string deckName = !string.IsNullOrEmpty(CardGameManager.Current.GamePlayDeckName)
                ? CardGameManager.Current.GamePlayDeckName
                : deck.Name;
            if (CgsNetManager.Instance.isNetworkActive && CgsNetManager.Instance.LocalPlayer != null)
            {
                CgsNetManager.Instance.LocalPlayer.RequestNewDeck(deckName, deckCards);
                var i = 1;
                foreach (KeyValuePair<string, List<Card>> cardGroup in extraGroups)
                {
                    Vector2 position = NextDeckPosition + Vector2.right * CardGameManager.PixelsPerInch * i *
                        CardGameManager.Current.CardSize.X;
                    CgsNetManager.Instance.LocalPlayer.RequestNewCardStack(cardGroup.Key,
                        cardGroup.Value.Cast<UnityCard>(), position);
                    i++;
                }
            }
            else
            {
                _soloDeckStack = CreateCardStack(deckName, deckCards, NextDeckPosition);
                var i = 1;
                foreach (KeyValuePair<string, List<Card>> cardGroup in extraGroups)
                {
                    Vector2 position = NextDeckPosition + Vector2.right * CardGameManager.PixelsPerInch * i *
                        CardGameManager.Current.CardSize.X;
                    CreateCardStack(cardGroup.Key, (List<UnityCard>) cardGroup.Value.Cast<UnityCard>(), position);
                    i++;
                }
            }

            NextDeckPosition += Vector2.down * CardGameManager.PixelsPerInch * CardGameManager.Current.CardSize.Y;

            PromptForHand();
        }

        private void CreateGameBoards(IEnumerable<GameBoard> boards)
        {
            foreach (GameBoard board in boards)
                CreateBoard(board);
        }

        private void CreateBoard(GameBoard board)
        {
            var newBoard = new GameObject(board.Id, typeof(RectTransform));
            var boardRectTransform = (RectTransform) newBoard.transform;
            boardRectTransform.SetParent(playArea.transform);
            boardRectTransform.anchorMin = Vector2.zero;
            boardRectTransform.anchorMax = Vector2.zero;
            boardRectTransform.offsetMin =
                new Vector2(board.OffsetMin.X, board.OffsetMin.Y) * CardGameManager.PixelsPerInch;
            boardRectTransform.offsetMax =
                new Vector2(board.OffsetMin.X, board.OffsetMin.Y) * CardGameManager.PixelsPerInch +
                boardRectTransform.offsetMin;

            string boardFilepath = CardGameManager.Current.GameBoardsDirectoryPath + "/" + board.Id + "." +
                                   CardGameManager.Current.GameBoardImageFileType;
            Sprite boardImageSprite = File.Exists(boardFilepath)
                ? UnityFileMethods.CreateSprite(boardFilepath)
                : null;
            if (boardImageSprite != null)
                newBoard.AddComponent<Image>().sprite = boardImageSprite;

            boardRectTransform.localScale = Vector3.one;
        }

        public CardStack CreateCardStack(string stackName, IReadOnlyList<UnityCard> cards, Vector2 position)
        {
            Transform target = playArea.transform;
            var cardStack = Instantiate(cardStackPrefab, target.parent).GetComponent<CardStack>();
            if (!string.IsNullOrEmpty(stackName))
                cardStack.Name = stackName;
            if (cards != null)
                cardStack.Cards = cards;
            var rectTransform = (RectTransform) cardStack.transform;
            rectTransform.SetParent(target);
            if (!Vector2.zero.Equals(position))
                rectTransform.anchoredPosition = position;
            cardStack.position = rectTransform.anchoredPosition;
            return cardStack;
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
            if (CgsNetManager.Instance.isNetworkActive && CgsNetManager.Instance.LocalPlayer != null &&
                CgsNetManager.Instance.LocalPlayer.DeckZone != null)
                CgsNetManager.Instance.LocalPlayer.RequestDeal(CgsNetManager.Instance.LocalPlayer.DeckZone, cardCount);
            else
                AddCardsToHand(PopSoloDeckCards(cardCount));
        }

        public void AddCardsToHand(IEnumerable<UnityCard> cards)
        {
            foreach (UnityCard card in cards)
                hand.AddCard(card);
        }

        private IEnumerable<UnityCard> PopSoloDeckCards(int count)
        {
            List<UnityCard> cards = new List<UnityCard>(count);
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
            Vector2 position = Vector2.left * CardGameManager.PixelsPerInch * CardGameManager.Current.CardSize.X;
            if (CgsNetManager.Instance.isNetworkActive && CgsNetManager.Instance.LocalPlayer != null)
                CgsNetManager.Instance.LocalPlayer.RequestNewCardStack(filters, cards, position);
            else
                CreateCardStack(filters, cards, position);
        }

        public Die CreateDie(int min, int max)
        {
            Transform target = playArea.transform;
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
