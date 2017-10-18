using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class PlayModeManager : MonoBehaviour
{
    public RectTransform playArea;
    public GameObject deckLoadMenuPrefab;
    public GameObject searchMenuPrefab;
    public ExtraZone extraZone;
    public DeckZone deckZone;
    public HandZone handZone;

    private DeckLoadMenu _deckLoader;
    private CardSearchMenu _cardSearcher;

    void Start()
    {
        playArea.gameObject.GetOrAddComponent<CardStack>().OnAddCardActions.Add(SetPlayActions);
    }

    public void ShowDeckLoader()
    {
        DeckLoader.Show(LoadDeck, UnityExtensionMethods.GetSafeFileName);
    }

    public void LoadDeck(Deck newDeck)
    {
        List<Card> extraCards = newDeck.GetExtraCards();
        foreach (Card card in extraCards)
            extraZone.AddCard(card);

        deckZone.Cards = newDeck.Cards;
        deckZone.Cards.RemoveAll((card) => extraCards.Contains(card));
        deckZone.Shuffle();

        // TODO: SEPARATE FUNCTION FOR DEALING OUT CARDS
        List<Card> handCards = new List<Card>();
        for (int i = 0; deckZone.Cards.Count > 0 && i < CardGameManager.Current.HandStartSize; i++) {
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
