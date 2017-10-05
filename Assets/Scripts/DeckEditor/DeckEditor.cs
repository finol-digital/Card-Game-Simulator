using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public delegate string NameChangeDelegate(string newName);

public class DeckEditor : MonoBehaviour
{
    public const string NewDeckPrompt = "Clear the editor and start a new Untitled deck?";

    public GameObject cardModelPrefab;
    public GameObject cardStackPrefab;
    public GameObject deckLoadMenuPrefab;
    public GameObject deckSaveMenuPrefab;
    public RectTransform layoutArea;
    public RectTransform layoutContent;
    public Scrollbar horizontalScrollbar;
    public Text nameText;
    public Text sizeText;

    private List<CardStack> _cardStacks;
    private int _recentCardStackIndex;
    private DeckLoadMenu _deckLoadMenu;
    private DeckSaveMenu _deckSaveMenu;

    void OnEnable()
    {
        CardGameManager.Instance.OnSelectActions.Add(ResetCardStacks);
    }

    public void ResetCardStacks()
    {
        Clear();
        layoutContent.DestroyAllChildren();
        CardStacks.Clear();
        for (int i = 0; i < CardGameManager.Current.DeckCardStackCount; i++) {
            CardStack newCardStack = Instantiate(cardStackPrefab, layoutContent).GetOrAddComponent<CardStack>();
            newCardStack.type = CardStackType.Vertical;
            newCardStack.scrollRectContainer = layoutArea.gameObject.GetOrAddComponent<ScrollRect>();
            newCardStack.OnCardDropActions.Add(OnAddCardModel);
            CardStacks.Add(newCardStack);
        }
        layoutContent.sizeDelta = new Vector2(cardStackPrefab.GetComponent<RectTransform>().rect.width * CardGameManager.Current.DeckCardStackCount, layoutContent.sizeDelta.y);
    }

    public void OnAddCardModel(CardStack cardStack, CardModel cardModel)
    {
        if (cardStack == null || cardModel == null)
            return;
        
        RecentCardStackIndex = CardStacks.IndexOf(cardStack);
        cardModel.DoubleClickEvent = DestroyCardModel;
        Deck deck = GetDeck();
        UpdateDeckSize(deck.Cards.Count + 1);
    }

    public void AddCard(CardModel cardToAdd)
    {
        if (cardToAdd == null || CardStacks.Count < 1)
            return;
        
        EventSystem.current.SetSelectedGameObject(null, cardToAdd.RecentPointerEventData);

        AddCard(cardToAdd.RepresentedCard);
    }

    public void AddCard(Card cardToAdd)
    {
        if (cardToAdd == null || CardStacks.Count < 1)
            return;

        int maxCopiesInStack = CardGameManager.Current.CopiesOfCardPerDeck;
        bool added = false;
        while (!added) {
            if (CardStacks [RecentCardStackIndex].transform.childCount < maxCopiesInStack) {
                CardModel newCardModel = Instantiate(cardModelPrefab, CardStacks [RecentCardStackIndex].transform).GetOrAddComponent<CardModel>();
                newCardModel.RepresentedCard = cardToAdd;
                newCardModel.DoubleClickEvent = DestroyCardModel;
                added = true;
            } else {
                RecentCardStackIndex++;
                if (RecentCardStackIndex == 0)
                    maxCopiesInStack++;
            }
        }

        float newSpot = cardStackPrefab.GetComponent<RectTransform>().rect.width * ((float)RecentCardStackIndex + ((RecentCardStackIndex < CardStacks.Count / 2f) ? 0f : 1f)) / layoutContent.sizeDelta.x;
        horizontalScrollbar.value = Mathf.Clamp01(newSpot);

        Deck deck = GetDeck();
        UpdateDeckSize(deck.Cards.Count);
    }

    public void DestroyCardModel(CardModel cardModel)
    {
        if (cardModel == null)
            return;

        GameObject.Destroy(cardModel.gameObject);
        CardInfoViewer.Instance.IsVisible = false;
        Deck deck = GetDeck();
        UpdateDeckSize(deck.Cards.Count - 1);
    }

    public Deck GetDeck()
    {
        Deck deck = new Deck(nameText.text);
        foreach (CardStack stack in CardStacks)
            foreach (CardModel card in stack.GetComponentsInChildren<CardModel>())
                deck.Cards.Add(card.RepresentedCard);
        return deck;
    }

    public void Sort()
    {
        Deck deck = GetDeck();
        deck.Sort();
        LoadDeck(deck);
    }

    public void PromptForClear()
    {
        CardGameManager.Instance.Popup.Prompt(NewDeckPrompt, Clear);
    }

    public void Clear()
    {
        foreach (CardStack stack in CardStacks)
            stack.transform.DestroyAllChildren();
        RecentCardStackIndex = 0;
        nameText.text = DeckLoadMenu.DefaultName;

        CardInfoViewer.Instance.IsVisible = false;
    }

    public string UpdateDeckName(string newName)
    {
        if (string.IsNullOrEmpty(newName))
            newName = DeckLoadMenu.DefaultName;
        nameText.text = UnityExtensionMethods.GetSafeFileName(newName);
        return nameText.text;
    }

    public void UpdateDeckSize(int newSize)
    {
        sizeText.text = newSize.ToString();
    }

    public void ShowDeckLoadMenu()
    {
        DeckLoadMenu.Show(LoadDeck, UpdateDeckName, nameText.text);
    }

    public void LoadDeck(Deck newDeck)
    {
        if (newDeck == null)
            return;

        Clear();
        UpdateDeckName(newDeck.Name);
        foreach (Card card in newDeck.Cards)
            AddCard(card);
    }

    public void ShowDeckSaveMenu()
    {
        Deck deck = GetDeck();
        DeckSaveMenu.Show(deck, UpdateDeckName);
    }

    public void BackToMainMenu()
    {
        // TODO: CHECK IF WE HAD ANY UNSAVED CHANGES
        SceneManager.LoadScene(0);
    }

    void OnDisable()
    {
        CardGameManager.Instance.OnSelectActions.Remove(ResetCardStacks);
    }

    public List<CardStack> CardStacks {
        get {
            if (_cardStacks == null)
                _cardStacks = new List<CardStack>();
            return _cardStacks;
        }
    }

    public int RecentCardStackIndex {
        get {
            if (_recentCardStackIndex < 0 || _recentCardStackIndex >= CardStacks.Count)
                _recentCardStackIndex = 0;
            return _recentCardStackIndex;
        }
        set {
            if (value < 0 || value >= CardStacks.Count)
                _recentCardStackIndex = 0;
            _recentCardStackIndex = value;
        }
    }

    public DeckLoadMenu DeckLoadMenu {
        get {
            if (_deckLoadMenu == null)
                _deckLoadMenu = Instantiate(deckLoadMenuPrefab, this.gameObject.FindInParents<Canvas>().transform).GetOrAddComponent<DeckLoadMenu>();
            return _deckLoadMenu;
        }
    }

    public DeckSaveMenu DeckSaveMenu {
        get {
            if (_deckSaveMenu == null)
                _deckSaveMenu = Instantiate(deckSaveMenuPrefab, this.gameObject.FindInParents<Canvas>().transform).GetOrAddComponent<DeckSaveMenu>();
            return _deckSaveMenu;
        }
    }
}
