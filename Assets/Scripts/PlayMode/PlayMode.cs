using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class PlayMode : MonoBehaviour
{
    public const string MainMenuPrompt = "Go back to the main menu?";

    public GameObject deckLoadMenuPrefab;
    public GameObject searchMenuPrefab;
    public RectTransform playArea;
    public ExtensibleCardZone extraZone;
    public StackedZone discardZone;
    public StackedZone deckZone;
    public ExtensibleCardZone handZone;

    private DeckLoadMenu _deckLoader;
    private CardSearchMenu _cardSearcher;

    void OnEnable()
    {
        CardGameManager.Instance.OnSelectActions.Add(ShowDeckLoader);
    }

    void Start()
    {
        DeckLoader.fileCancelButton.onClick.RemoveAllListeners();
        DeckLoader.fileCancelButton.onClick.AddListener(BackToMainMenu);
        DeckLoader.textCancelButton.onClick.RemoveAllListeners();
        DeckLoader.textCancelButton.onClick.AddListener(BackToMainMenu);
        playArea.gameObject.GetOrAddComponent<CardStack>().OnAddCardActions.Add(SetPlayActions);
    }

    void Update()
    {
        if (Input.GetButtonDown("Draw"))
            Deal(1);
    }

    public void ShowDeckLoader()
    {
        DeckLoader.Show(Deck.DefaultName, UnityExtensionMethods.GetSafeFileName, LoadDeck);
    }

    public void LoadDeck(Deck newDeck)
    {
        List<Card> extraCards = newDeck.GetExtraCards();
        Dictionary<string, List<Card>> extraGroups = newDeck.GetExtraGroups();
        foreach (KeyValuePair<string, List<Card>> cardGroup in extraGroups) {
            extraZone.labelText.text = cardGroup.Key;
            foreach (Card card in cardGroup.Value)
                extraZone.AddCard(card);
            // TODO: ALLOW MULTIPLE CARD GROUPS
        }

        foreach (Card card in newDeck.Cards)
            if (!extraCards.Contains(card))
                deckZone.AddCard(card);
        deckZone.Shuffle();

        Deal(CardGameManager.Current.HandStartSize);
    }

    public void Deal(int cardCount)
    {
        for (int i = 0; deckZone.Count > 0 && i < cardCount; i++)
            handZone.AddCard(deckZone.PopCard());
    }

    public void ShowCardSearcher()
    {
        CardSearcher.Show(null, null, AddCard);
    }

    public void AddCard(List<Card> results)
    {
        if (results == null || results.Count < 1)
            return;
        
        handZone.AddCard(results [0]);
    }

    public void SetPlayActions(CardStack cardStack, CardModel cardModel)
    {
        cardModel.DoubleClickEvent = CardModel.ToggleFacedown;
        cardModel.SecondaryDragAction = cardModel.Rotate;
    }

    public void PromptBackToMainMenu()
    {
        CardGameManager.Instance.Popup.Prompt(MainMenuPrompt, BackToMainMenu);
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene(0);
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
