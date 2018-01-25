using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PlayMode : MonoBehaviour
{
    public const string DrawInput = "Draw";
    public const string CancelInput = "Cancel";
    public const string MainMenuPrompt = "Go back to the main menu?";

    public GameObject lobbyPrefab;
    public GameObject deckLoadMenuPrefab;
    public GameObject searchMenuPrefab;
    public GameObject diceMenuPrefab;
    public GameObject extraZonePrefab;
    public GameObject discardZonePrefab;
    public GameObject deckZonePrefab;
    public GameObject handZonePrefab;

    public ZonesViewer zones;
    public PointsCounter points;
    public Text netText;
    public RectTransform playAreaContent;

    public LobbyMenu Lobby => _lobby ?? (_lobby = Instantiate(lobbyPrefab).GetOrAddComponent<LobbyMenu>());
    private LobbyMenu _lobby;

    public DiceMenu DiceCreator => _diceCreator ?? (_diceCreator = Instantiate(diceMenuPrefab).GetOrAddComponent<DiceMenu>());
    private DiceMenu _diceCreator;

    public CardSearchMenu CardSearcher => _cardSearcher ?? (_cardSearcher = Instantiate(searchMenuPrefab).GetOrAddComponent<CardSearchMenu>());
    private CardSearchMenu _cardSearcher;

    public DeckLoadMenu DeckLoader => _deckLoader ?? (_deckLoader = Instantiate(deckLoadMenuPrefab).GetOrAddComponent<DeckLoadMenu>());
    private DeckLoadMenu _deckLoader;

    protected Deck LoadedDeck { get; private set; }
    protected StackedZone DeckZone { get; private set; }
    protected ExtensibleCardZone HandZone { get; private set; }

    void Start()
    {
        if (CardGameManager.IsMultiplayer) {
            Lobby.cancelButton.onClick.RemoveAllListeners();
            Lobby.cancelButton.onClick.AddListener(BackToMainMenu);
            Lobby.Show();
        } else {
            DeckLoader.loadCancelButton.onClick.RemoveAllListeners();
            DeckLoader.loadCancelButton.onClick.AddListener(BackToMainMenu);
            DeckLoader.Show(LoadDeck);
        }

        playAreaContent.sizeDelta = CardGameManager.Current.PlayAreaSize * CardGameManager.PixelsPerInch;
        playAreaContent.gameObject.GetOrAddComponent<CardStack>().OnAddCardActions.Add(AddCardToPlay);

        if (CardGameManager.Current.GameHasDiscardZone)
            zones.AddZone(Instantiate(discardZonePrefab, zones.ActiveScrollView.content).GetComponent<StackedZone>());
    }

    void OnRectTransformDimensionsChange()
    {
        if (!gameObject.activeInHierarchy)
            return;

        zones.ActiveScrollView = GetComponent<RectTransform>().rect.width > GetComponent<RectTransform>().rect.height ?
            zones.verticalScrollView : zones.horizontalScrollView;
    }

    void Update()
    {
        if (CardInfoViewer.Instance.IsVisible)
            return;

        if (Input.GetButtonUp(DrawInput) && CardGameManager.TopMenuCanvas == null)
            Deal(1);
        else if ((Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(CancelInput)) && CardGameManager.TopMenuCanvas == null)
            PromptBackToMainMenu();
    }

    public void ShowDiceMenu()
    {
        DiceCreator.Show(playAreaContent);
    }

    public void ShowCardsMenu()
    {
        CardSearcher.Show(null, null, AddCardsToHand);
    }

    public void ShowDeckMenu()
    {
        DeckLoader.Show(LoadDeck);
    }

    public void LoadDeck(Deck newDeck)
    {
        if (newDeck == null)
            return;
        LoadedDeck = newDeck;

        foreach (Card card in newDeck.Cards)
            foreach (GameBoardCard boardCard in CardGameManager.Current.GameBoardCards)
                if (card.Id.Equals(boardCard.Card))
                    CreateGameBoards(boardCard.Boards);

        Dictionary<string, List<Card>> extraGroups = newDeck.GetExtraGroups();
        foreach (KeyValuePair<string, List<Card>> cardGroup in extraGroups) {
            ExtensibleCardZone extraZone = Instantiate(extraZonePrefab, zones.ActiveScrollView.content).GetComponent<ExtensibleCardZone>();
            extraZone.labelText.text = cardGroup.Key;
            foreach (Card card in cardGroup.Value)
                extraZone.AddCard(card);
            zones.AddZone(extraZone);
        }

        DeckZone = Instantiate(deckZonePrefab, zones.ActiveScrollView.content).GetComponent<StackedZone>();
        zones.AddZone(DeckZone);

        if (HandZone == null)
            CreateHand();
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

    public void CreateHand()
    {
        HandZone = Instantiate(handZonePrefab, zones.ActiveScrollView.content).GetComponent<ExtensibleCardZone>();
        zones.AddZone(HandZone);
        zones.IsExtended = true;
        zones.IsVisible = true;
    }

    public IEnumerator WaitToDealDeck()
    {
        yield return null;

        zones.verticalScrollView.verticalScrollbar.value = 0;
        zones.horizontalScrollView.horizontalScrollbar.value = 0;

        List<Card> extraCards = LoadedDeck.GetExtraCards();
        foreach (Card card in LoadedDeck.Cards)
            if (!extraCards.Contains(card))
                DeckZone.AddCard(card);
        DeckZone.Shuffle();

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
        if (DeckZone == null)
            return cards;

        for (int i = 0; i < cardCount && DeckZone.Count > 0; i++)
            cards.Add(DeckZone.PopCard());
        return cards;
    }

    public void AddCardsToHand(List<Card> cards)
    {
        if (HandZone == null)
            CreateHand();

        foreach (Card card in cards)
            HandZone.AddCard(card);
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
