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
    public const int CardStackSize = 8;

    public int CardStackCount {
        get {
            return Mathf.CeilToInt((float)CardGameManager.Current.DeckMaxSize / CardStackSize);
        }
    }

    public Deck CurrentDeck {
        get { 
            Deck deck = new Deck(nameText.text, CardGameManager.Current.DeckFileType);
            foreach (CardStack stack in CardStacks)
                foreach (CardModel card in stack.GetComponentsInChildren<CardModel>())
                    deck.Cards.Add(card.Card);
            return deck;
        }
    }

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
        UpdateDeckSize();
    }

    public void OnRemoveCardModel(CardStack cardStack, CardModel cardModel)
    {
        UpdateDeckSize();
    }

    public void AddCardModel(CardModel cardModelToAdd)
    {
        if (cardModelToAdd == null || CardStacks.Count < 1)
            return;
        
        EventSystem.current.SetSelectedGameObject(null, cardModelToAdd.RecentPointerEventData);

        AddCard(cardModelToAdd.Card);
    }

    public void AddCard(Card cardToAdd)
    {
        if (cardToAdd == null || CardStacks.Count < 1)
            return;
        
        int maxCopiesInStack = CardStackSize;
        bool added = false;
        while (!added) {
            if (CardStacks [RecentCardStackIndex].transform.childCount < maxCopiesInStack) {
                CardModel newCardModel = Instantiate(cardModelPrefab, CardStacks [RecentCardStackIndex].transform).GetOrAddComponent<CardModel>();
                newCardModel.Card = cardToAdd;
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

        UpdateDeckSize();
    }

    public void DestroyCardModel(CardModel cardModel)
    {
        if (cardModel == null)
            return;

        cardModel.transform.SetParent(null);
        GameObject.Destroy(cardModel.gameObject);
        CardInfoViewer.Instance.IsVisible = false;
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
        nameText.text = Deck.DefaultName;

        CardInfoViewer.Instance.IsVisible = false;
        UpdateDeckSize();
    }

    public string UpdateDeckName(string newName)
    {
        if (string.IsNullOrEmpty(newName))
            newName = Deck.DefaultName;
        nameText.text = UnityExtensionMethods.GetSafeFileName(newName);
        return nameText.text;
    }

    public void UpdateDeckSize()
    {
        sizeText.text = CurrentDeck.Cards.Count.ToString();
    }

    public void ShowDeckLoadMenu()
    {
        DeckLoader.Show(LoadDeck, UpdateDeckName, nameText.text);
    }

    public void LoadDeck(Deck newDeck)
    {
        if (newDeck == null)
            return;

        Clear();
        UpdateDeckName(newDeck.Name);
        foreach (Card card in newDeck.Cards)
            AddCard(card);
        UpdateDeckSize();
    }

    public void ShowDeckSaveMenu()
    {
        DeckSaver.Show(CurrentDeck, UpdateDeckName);
    }

    public void BackToMainMenu()
    {
        // TODO: CHECK IF WE HAD ANY UNSAVED CHANGES
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
