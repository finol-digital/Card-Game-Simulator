using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public delegate string OnDeckNameChangeDelegate(string newName);

public class DeckEditor : MonoBehaviour, ICardDropHandler
{
    public const string NewDeckPrompt = "Clear the editor and start a new Untitled deck?";
    public const string SaveChangesPrompt = "You have unsaved changes. Would you like to save?";
    public const int CardStackSize = 8;

    public int CardStackCount {
        get {
            return Mathf.CeilToInt((float)CardGameManager.Current.DeckMaxSize / CardStackSize);
        }
    }

    public Deck CurrentDeck {
        get { 
            Deck deck = new Deck(nameText.text.Replace("*", ""), CardGameManager.Current.DeckFileType);
            foreach (CardStack stack in CardStacks)
                foreach (CardModel card in stack.GetComponentsInChildren<CardModel>())
                    deck.Cards.Add(card.Value);
            return deck;
        }
    }

    public bool HasChanged { get; private set; }

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
    private DeckLoadMenu _deckLoader;
    private DeckSaveMenu _deckSaver;

    void OnEnable()
    {
        CardGameManager.Instance.OnSelectActions.Add(ResetCardStacks);
    }

    void Start()
    {
        layoutArea.gameObject.GetOrAddComponent<CardDropZone>().dropHandler = this;
    }

    public void ResetCardStacks()
    {
        Clear();
        layoutContent.DestroyAllChildren();
        CardStacks.Clear();
        for (int i = 0; i < CardStackCount; i++) {
            CardStack newCardStack = Instantiate(cardStackPrefab, layoutContent).GetOrAddComponent<CardStack>();
            newCardStack.type = CardStackType.Vertical;
            newCardStack.scrollRectContainer = layoutArea.gameObject.GetOrAddComponent<ScrollRect>();
            newCardStack.OnAddCardActions.Add(OnAddCardModel);
            newCardStack.OnRemoveCardActions.Add(OnRemoveCardModel);
            CardStacks.Add(newCardStack);
        }
        layoutContent.sizeDelta = new Vector2(cardStackPrefab.GetComponent<RectTransform>().rect.width * CardStacks.Count, layoutContent.sizeDelta.y);
    }

    public void OnAddCardModel(CardStack cardStack, CardModel cardModel)
    {
        if (cardStack == null || cardModel == null)
            return;
        
        RecentCardStackIndex = CardStacks.IndexOf(cardStack);
        cardModel.DoubleClickEvent = DestroyCardModel;
        HasChanged = true;
        UpdateDeckName();
        UpdateDeckSize();
    }

    public void OnRemoveCardModel(CardStack cardStack, CardModel cardModel)
    {
        HasChanged = true;
        UpdateDeckName();
        UpdateDeckSize();
    }

    public void OnDrop(CardModel cardModel)
    {
        AddCardModel(cardModel);
    }

    public void AddCardModel(CardModel cardModel)
    {
        if (cardModel == null || CardStacks.Count < 1)
            return;
        
        EventSystem.current.SetSelectedGameObject(null, cardModel.RecentPointerEventData);

        AddCard(cardModel.Value);
    }

    public void AddCard(Card card)
    {
        if (card == null || CardStacks.Count < 1)
            return;
        
        int maxCopiesInStack = CardStackSize;
        bool added = false;
        while (!added) {
            if (CardStacks [RecentCardStackIndex].transform.childCount < maxCopiesInStack) {
                CardModel newCardModel = Instantiate(cardModelPrefab, CardStacks [RecentCardStackIndex].transform).GetOrAddComponent<CardModel>();
                newCardModel.Value = card;
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

        HasChanged = true;
        UpdateDeckName();
        UpdateDeckSize();
    }

    public void DestroyCardModel(CardModel cardModel)
    {
        if (cardModel == null)
            return;

        cardModel.transform.SetParent(null);
        GameObject.Destroy(cardModel.gameObject);
        CardInfoViewer.Instance.IsVisible = false;
        HasChanged = true;
        UpdateDeckName();
        UpdateDeckSize();
    }

    public void Sort()
    {
        Deck sortedDeck = CurrentDeck;
        sortedDeck.Sort();
        LoadDeck(sortedDeck);
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

        CardInfoViewer.Instance.IsVisible = false;
        HasChanged = false;
        UpdateDeckName(Deck.DefaultName);
        UpdateDeckSize();
    }

    public string UpdateDeckName(string newName)
    {
        if (string.IsNullOrEmpty(newName))
            newName = Deck.DefaultName;
        newName = UnityExtensionMethods.GetSafeFileName(newName);
        nameText.text = newName + (HasChanged ? "*" : "");
        return newName;
    }

    public string UpdateDeckName()
    {
        nameText.text = CurrentDeck.Name + (HasChanged ? "*" : "");
        return nameText.text;
    }

    public void UpdateDeckSize()
    {
        sizeText.text = CurrentDeck.Cards.Count.ToString();
    }

    public void ShowDeckLoadMenu()
    {
        DeckLoader.Show(CurrentDeck.Name, UpdateDeckName, LoadDeck);
    }

    public void LoadDeck(Deck newDeck)
    {
        if (newDeck == null)
            return;

        Clear();
        foreach (Card card in newDeck.Cards)
            AddCard(card);
        HasChanged = false;
        UpdateDeckName(newDeck.Name);
        UpdateDeckSize();
    }

    public void ShowDeckSaveMenu()
    {
        DeckSaver.Show(CurrentDeck, UpdateDeckName, OnSaveDeck);
    }

    public void OnSaveDeck(Deck savedDeck)
    {
        HasChanged = false;
        UpdateDeckName(savedDeck.Name);
        UpdateDeckSize();
    }

    public void CheckBackToMainMenu()
    {
        if (HasChanged) {
            CardGameManager.Instance.Popup.Ask(SaveChangesPrompt, BackToMainMenu, ShowDeckSaveMenu);
            return;
        }

        BackToMainMenu();
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene(0);
    }

    void OnDisable()
    {
        if (CardGameManager.HasInstance)
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
            _recentCardStackIndex = value;
        }
    }

    public DeckLoadMenu DeckLoader {
        get {
            if (_deckLoader == null)
                _deckLoader = Instantiate(deckLoadMenuPrefab, this.gameObject.FindInParents<Canvas>().transform).GetOrAddComponent<DeckLoadMenu>();
            return _deckLoader;
        }
    }

    public DeckSaveMenu DeckSaver {
        get {
            if (_deckSaver == null)
                _deckSaver = Instantiate(deckSaveMenuPrefab, this.gameObject.FindInParents<Canvas>().transform).GetOrAddComponent<DeckSaveMenu>();
            return _deckSaver;
        }
    }
}
