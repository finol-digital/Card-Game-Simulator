using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PlayMode : MonoBehaviour
{
    public const string MainMenuPrompt = "Go back to the main menu?";
    public const string DrawString = "Draw";

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
    public RectTransform playAreaContent;

    public Deck LoadedDeck { get; private set; }
    public StackedZone DeckZone { get; private set; }
    public ExtensibleCardZone HandZone { get; private set; }

    public LobbyMenu Lobby => _lobby ?? (_lobby = Instantiate(lobbyPrefab).GetOrAddComponent<LobbyMenu>());
    private LobbyMenu _lobby;

    public DeckLoadMenu DeckLoader => _deckLoader ?? (_deckLoader = Instantiate(deckLoadMenuPrefab).GetOrAddComponent<DeckLoadMenu>());
    private DeckLoadMenu _deckLoader;

    public DiceMenu DiceCreator => _diceCreator ?? (_diceCreator = Instantiate(diceMenuPrefab).GetOrAddComponent<DiceMenu>());
    private DiceMenu _diceCreator;

    public CardSearchMenu CardSearcher => _cardSearcher ?? (_cardSearcher = Instantiate(searchMenuPrefab).GetOrAddComponent<CardSearchMenu>());
    private CardSearchMenu _cardSearcher;

    void Start()
    {
        if (CardGameManager.IsMultiplayer) {
            Lobby.cancelButton.onClick.RemoveAllListeners();
            Lobby.cancelButton.onClick.AddListener(BackToMainMenu);
            Lobby.Show(this);
        } else {
            DeckLoader.loadCancelButton.onClick.RemoveAllListeners();
            DeckLoader.loadCancelButton.onClick.AddListener(BackToMainMenu);
            DeckLoader.Show(LoadDeck);
        }

        playAreaContent.gameObject.GetOrAddComponent<CardStack>().OnAddCardActions.Add(AddCardToPlay);
    }

    void OnRectTransformDimensionsChange()
    {
        if (!gameObject.activeInHierarchy)
            return;

        zones.ActiveScrollView = GetComponent<RectTransform>().rect.width > GetComponent<RectTransform>().rect.height ? zones.verticalScrollView : zones.horizontalScrollView;
    }

#if (!UNITY_ANDROID && !UNITY_IOS) || UNITY_EDITOR
    void Update()
    {
        if (Input.GetButtonDown(DrawString))
            Deal(1);
    }
#endif

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

        if (CardGameManager.Current.GameHasDiscardZone) {
            StackedZone discardZone = Instantiate(discardZonePrefab, zones.ActiveScrollView.content).GetComponent<StackedZone>();
            zones.AddZone(discardZone);
        }

        DeckZone = Instantiate(deckZonePrefab, zones.ActiveScrollView.content).GetComponent<StackedZone>();
        zones.AddZone(DeckZone);
        HandZone = Instantiate(handZonePrefab, zones.ActiveScrollView.content).GetComponent<ExtensibleCardZone>();
        zones.AddZone(HandZone);
        zones.IsExtended = true;
        zones.IsVisible = true;

        points.Count = CardGameManager.Current.GameStartPointsCount;
        StartCoroutine(WaitToDealDeck());
    }

    public void CreateGameBoards(List<GameBoard> boards)
    {
        if (boards == null || boards.Count < 1)
            return;

        foreach (GameBoard board in boards)
            StartCoroutine(CreateBoard(board));
    }

    public IEnumerator CreateBoard(GameBoard board)
    {
        GameObject newBoard = new GameObject(board.Id, typeof(RectTransform));
        RectTransform rt = (RectTransform)newBoard.transform;
        rt.SetParent(playAreaContent);
        rt.anchorMax = Vector2.zero;
        rt.anchorMin = Vector2.zero;
        rt.offsetMax = CardGameManager.PixelsPerInch * board.OffsetMax;
        rt.offsetMin = CardGameManager.PixelsPerInch * board.OffsetMin;

        Sprite boardImageSprite = null;
        string boardFilepath = CardGameManager.Current.GameBoardsFilePath + "/" + board.Id + "." +
                               CardGameManager.Current.GameBoardFileType;
        yield return UnityExtensionMethods.RunOutputCoroutine<Sprite>(UnityExtensionMethods.CreateAndOutputSpriteFromImageFile(boardFilepath), output => boardImageSprite = output);
        if (boardImageSprite != null)
            newBoard.AddComponent<Image>().sprite = boardImageSprite;

        rt.localScale = Vector3.one;
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

        Deal(CardGameManager.Current.GameStartHandCount);
    }


    public void Deal(int cardCount)
    {
        if (DeckZone == null || HandZone == null)
            return;

        for (int i = 0; DeckZone.Count > 0 && i < cardCount; i++)
            HandZone.AddCard(DeckZone.PopCard());
    }

    public void ShowDeckMenu()
    {
        DeckLoader.Show(LoadDeck);
    }

    public void ShowDiceMenu()
    {
        DiceCreator.Show(playAreaContent);
    }

    public void ShowCardsMenu()
    {
        CardSearcher.Show(null, null, AddSearchResults);
    }

    public void AddSearchResults(List<Card> results)
    {
        foreach (Card card in results)
            HandZone.AddCard(card);
    }

    public void AddCardToPlay(CardStack cardStack, CardModel cardModel)
    {
        if (NetworkManager.singleton.isNetworkActive)
            LocalNetManager.Instance.LocalPlayer.MoveCardToServer(cardModel);
        else
            LocalNetManager.Instance.SetPlayActions(cardStack, cardModel);
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
