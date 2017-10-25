using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class PlayMode : MonoBehaviour
{
    public RectTransform playArea;
    public GameObject deckLoadMenuPrefab;
    public GameObject searchMenuPrefab;
    public ExtensibleCardZone extraZone;
    public DeckZone deckZone;
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
            break; // TODO: ALLOW MULTIPE CARD GROUPS
        }

        deckZone.Cards = newDeck.Cards;
        deckZone.Cards.RemoveAll(card => extraCards.Contains(card));
        deckZone.Shuffle();

        Deal(CardGameManager.Current.HandStartSize);
    }

    public void Deal(int cardCount)
    {
        List<Card> handCards = new List<Card>();
        for (int i = 0; deckZone.Cards.Count > 0 && i < cardCount; i++) {
            handCards.Add(deckZone.Cards.Last());
            deckZone.Cards.RemoveAt(deckZone.Cards.Count - 1);
        }
        foreach (Card card in handCards)
            handZone.AddCard(card);

        deckZone.Display();
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

    public void BackToMainMenu()
    {
        SceneManager.LoadScene(0);
    }

    void OnDisable()
    {
        if (CardGameManager.HasInstance)
            CardGameManager.Instance.OnSelectActions.Remove(ShowDeckLoader);
    }

    public DeckLoadMenu DeckLoader {
        get {
            if (_deckLoader == null)
                _deckLoader = Instantiate(deckLoadMenuPrefab, this.gameObject.FindInParents<Canvas>().transform).GetOrAddComponent<DeckLoadMenu>();
            return _deckLoader;
        }
    }

    public CardSearchMenu CardSearcher {
        get {
            if (_cardSearcher == null)
                _cardSearcher = Instantiate(searchMenuPrefab, this.gameObject.FindInParents<Canvas>().transform).GetOrAddComponent<CardSearchMenu>();
            return _cardSearcher;
        }
    }
}
