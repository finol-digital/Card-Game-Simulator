using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PlayMode : MonoBehaviour
{
    public const string MainMenuPrompt = "Go back to the main menu?";

    public GameObject cardViewerPrefab;
    public GameObject lobbyPrefab;
    public GameObject deckLoadMenuPrefab;
    public GameObject searchMenuPrefab;
    public GameObject diceMenuPrefab;

    public Text netText;
    public PointsCounter points;
    public ZonesViewer zones;
    public RectTransform playAreaContent;

    public LobbyMenu Lobby => _lobby ?? (_lobby = Instantiate(lobbyPrefab).GetOrAddComponent<LobbyMenu>());
    private LobbyMenu _lobby;

    public DeckLoadMenu DeckLoader => _deckLoader ?? (_deckLoader = Instantiate(deckLoadMenuPrefab).GetOrAddComponent<DeckLoadMenu>());
    private DeckLoadMenu _deckLoader;

    public CardSearchMenu CardSearcher => _cardSearcher ?? (_cardSearcher = Instantiate(searchMenuPrefab).GetOrAddComponent<CardSearchMenu>());
    private CardSearchMenu _cardSearcher;

    public DiceMenu DiceManager => _diceManager ?? (_diceManager = Instantiate(diceMenuPrefab).GetOrAddComponent<DiceMenu>());
    private DiceMenu _diceManager;

    protected Deck LoadedDeck { get; private set; }

    void Start()
    {
        Instantiate(cardViewerPrefab);

        if (CardGameManager.IsMultiplayer) {
            Lobby.cancelButton.onClick.RemoveAllListeners();
            Lobby.cancelButton.onClick.AddListener(BackToMainMenu);
            Lobby.Show();
        } else
            Lobby.Host();

        playAreaContent.sizeDelta = CardGameManager.Current.PlayAreaSize * CardGameManager.PixelsPerInch;
        playAreaContent.gameObject.GetOrAddComponent<CardStack>().OnAddCardActions.Add(AddCardToPlay);
    }

    void Update()
    {
        if (CardInfoViewer.Instance.IsVisible || !Input.anyKeyDown || CardGameManager.TopMenuCanvas != null)
            return;

        if (Input.GetButtonDown(CardIn.DrawInput))
            Deal(1);
        else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(CardIn.CancelInput))
            PromptBackToMainMenu();
    }

    public void ShowDeckMenu()
    {
        DeckLoader.Show(LoadDeck);
    }

    public void ShowCardsMenu()
    {
        CardSearcher.Show(null, null, AddCardsToHand);
    }

    public void ShowDiceMenu()
    {
        DiceManager.Show(playAreaContent);
    }

    public void LoadDeck(Deck deck)
    {
        if (deck == null)
            return;
        LoadedDeck = deck;

        foreach (Card card in deck.Cards)
            foreach (GameBoardCard boardCard in CardGameManager.Current.GameBoardCards)
                if (card.Id.Equals(boardCard.Card))
                    CreateGameBoards(boardCard.Boards);

        Dictionary<string, List<Card>> extraGroups = deck.GetExtraGroups();
        foreach (KeyValuePair<string, List<Card>> cardGroup in extraGroups)
            zones.CreateExtraZone(cardGroup.Key, cardGroup.Value);

        zones.CreateDeck();
        if (zones.HandZone == null)
            zones.CreateHand();
        StartCoroutine(WaitToDealDeck());
    }

    public void CreateGameBoards(List<GameBoard> boards)
    {
        if (boards == null || boards.Count < 1)
            return;

        foreach (GameBoard board in boards)
            CreateBoard(board);
    }

    public void CreateBoard(GameBoard board)
    {
        if (board == null)
            return;

        GameObject newBoard = new GameObject(board.Id, typeof(RectTransform));
        RectTransform rt = (RectTransform)newBoard.transform;
        rt.SetParent(playAreaContent);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.offsetMin = board.OffsetMin * CardGameManager.PixelsPerInch;
        rt.offsetMax = board.Size * CardGameManager.PixelsPerInch + rt.offsetMin;

        string boardFilepath = CardGameManager.Current.GameBoardsFilePath + "/" + board.Id + "." +
                               CardGameManager.Current.GameBoardFileType;
        Sprite boardImageSprite = UnityExtensionMethods.CreateSprite(boardFilepath);
        if (boardImageSprite != null)
            newBoard.AddComponent<Image>().sprite = boardImageSprite;

        rt.localScale = Vector3.one;
    }

    public IEnumerator WaitToDealDeck()
    {
        yield return null;

        zones.scrollView.verticalScrollbar.value = 0;

        List<Card> extraCards = LoadedDeck.GetExtraCards();
        foreach (Card card in LoadedDeck.Cards)
            if (!extraCards.Contains(card))
                zones.CurrentDeckZone.AddCard(card);
        zones.CurrentDeckZone.Shuffle();

        if (!NetworkManager.singleton.isNetworkActive)
            Deal(CardGameManager.Current.GameStartHandCount);
    }

    public void Deal(int cardCount)
    {
        AddCardsToHand(PopDeckCards(cardCount));
    }

    public List<Card> PopDeckCards(int cardCount)
    {
        List<Card> cards = new List<Card>(cardCount);
        if (zones.CurrentDeckZone == null)
            return cards;

        for (int i = 0; i < cardCount && zones.CurrentDeckZone.Count > 0; i++)
            cards.Add(zones.CurrentDeckZone.PopCard());
        return cards;
    }

    public void AddCardsToHand(List<Card> cards)
    {
        if (zones.HandZone == null)
            zones.CreateHand();

        foreach (Card card in cards)
            zones.HandZone.AddCard(card);
    }

    public void AddCardToPlay(CardStack cardStack, CardModel cardModel)
    {
        if (NetworkManager.singleton.isNetworkActive)
            LocalNetManager.Instance.LocalPlayer.MoveCardToServer(cardStack, cardModel);
        else
            SetPlayActions(cardStack, cardModel);
    }

    public void SetPlayActions(CardStack cardStack, CardModel cardModel)
    {
        cardModel.DoubleClickAction = CardModel.ToggleFacedown;
        cardModel.SecondaryDragAction = cardModel.Rotate;
    }

    public void PromptBackToMainMenu()
    {
        CardGameManager.Instance.Messenger.Prompt(MainMenuPrompt, BackToMainMenu);
    }

    public void BackToMainMenu()
    {
        if (NetworkManager.singleton.isNetworkActive || LocalNetManager.Instance.Discovery.running) {
            if (NetworkServer.active)
                NetworkManager.singleton.StopHost();
            else if (NetworkManager.singleton.IsClientConnected())
                NetworkManager.singleton.StopClient();
            LocalNetManager.Instance.Discovery.StopBroadcast();
        }

        SceneManager.LoadScene(MainMenu.MainMenuSceneIndex);
    }
}
