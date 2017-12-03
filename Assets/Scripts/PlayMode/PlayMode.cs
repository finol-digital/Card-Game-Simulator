using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

public class PlayMode : MonoBehaviour
{
    public const string MainMenuPrompt = "Go back to the main menu?";

    public GameObject deckLoadMenuPrefab;
    public GameObject searchMenuPrefab;
    public GameObject diceZonePrefab;
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

    private DeckLoadMenu _deckLoader;
    private CardSearchMenu _cardSearcher;

    IEnumerator Start()
    {
        // TODO: BETTER MANAGEMENT OF ONLINE VS OFFLINE
        if (CardGameManager.IsMultiplayer) {
            ((LocalNetManager)NetworkManager.singleton).SearchForHost();
            yield return new WaitForSecondsRealtime(3.0f);
            if (!NetworkManager.singleton.isNetworkActive)
                NetworkManager.singleton.StartHost();
        }

        DeckLoader.loadCancelButton.onClick.RemoveAllListeners();
        DeckLoader.loadCancelButton.onClick.AddListener(BackToMainMenu);
        DeckLoader.Show(LoadDeck);

        playAreaContent.gameObject.GetOrAddComponent<CardStack>().OnAddCardActions.Add(AddCardToPlay);
    }

    void OnRectTransformDimensionsChange()
    {
        if (!this.gameObject.activeInHierarchy)
            return;

        zones.ActiveScrollView = GetComponent<RectTransform>().rect.width > GetComponent<RectTransform>().rect.height ? zones.verticalScrollView : zones.horizontalScrollView;
    }

    void Update()
    {
        if (Input.GetButtonDown("Draw"))
            Deal(1);
    }

    public void LoadDeck(Deck newDeck)
    {
        LoadedDeck = newDeck;

        Dictionary<string, List<Card>> extraGroups = newDeck.GetExtraGroups();
        foreach (KeyValuePair<string, List<Card>> cardGroup in extraGroups) {
            ExtensibleCardZone extraZone = Instantiate(extraZonePrefab, zones.ActiveScrollView.content).GetComponent<ExtensibleCardZone>();
            extraZone.labelText.text = cardGroup.Key;
            foreach (Card card in cardGroup.Value)
                extraZone.AddCard(card);
            zones.AddZone(extraZone);
        }

        StackedZone discardZone = Instantiate(discardZonePrefab, zones.ActiveScrollView.content).GetComponent<StackedZone>();
        DeckZone = Instantiate(deckZonePrefab, zones.ActiveScrollView.content).GetComponent<StackedZone>();
        HandZone = Instantiate(handZonePrefab, zones.ActiveScrollView.content).GetComponent<ExtensibleCardZone>();

        zones.AddZone(discardZone);
        zones.AddZone(DeckZone);
        zones.AddZone(HandZone);

        points.Count = CardGameManager.Current.GameStartPointsCount;
        StartCoroutine(WaitToDealDeck());
    }

    public IEnumerator WaitToDealDeck()
    {
        yield return null;

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

    public void AddCardToPlay(CardStack cardStack, CardModel cardModel)
    {
        if (NetworkManager.singleton.isNetworkActive)
            ((LocalNetManager)NetworkManager.singleton).LocalPlayer.MoveCardToServer(cardModel);
        else
            ((LocalNetManager)NetworkManager.singleton).SetPlayActions(cardStack, cardModel);
    }

    public void PromptBackToMainMenu()
    {
        CardGameManager.Instance.Messenger.Prompt(MainMenuPrompt, BackToMainMenu);
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene(MainMenu.MainMenuSceneIndex);
    }

    public DeckLoadMenu DeckLoader {
        get {
            if (_deckLoader == null)
                _deckLoader = Instantiate(deckLoadMenuPrefab).GetOrAddComponent<DeckLoadMenu>();
            return _deckLoader;
        }
    }

    public CardSearchMenu CardSearcher {
        get {
            if (_cardSearcher == null)
                _cardSearcher = Instantiate(searchMenuPrefab).GetOrAddComponent<CardSearchMenu>();
            return _cardSearcher;
        }
    }
}
