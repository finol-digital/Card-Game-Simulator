using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public delegate string DeckNameChangeDelegate(string newName);

public class DeckEditor : MonoBehaviour
{
    public const string NewDeckPrompt = "Clear the editor and start a new Untitled deck?";

    public GameObject cardModelPrefab;
    public GameObject cardStackPrefab;
    public GameObject deckLoadMenuPrefab;
    public GameObject deckSaveMenuPrefab;
    public RectTransform deckEditorContent;
    public Scrollbar deckEditorScrollbar;
    public Text deckEditorNameText;

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
        deckEditorContent.DestroyAllChildren();
        CardStacks.Clear();
        for (int i = 0; i < CardGameManager.Current.DeckCardStackCount; i++) {
            CardStack newCardStack = Instantiate(cardStackPrefab, deckEditorContent).GetOrAddComponent<CardStack>();
            newCardStack.CardAddedActions.Add(OnAddCardModel);
            CardStacks.Add(newCardStack);
        }
        deckEditorContent.sizeDelta = new Vector2(cardStackPrefab.GetComponent<RectTransform>().rect.width * CardGameManager.Current.DeckCardStackCount, deckEditorContent.sizeDelta.y);
    }

    public void OnAddCardModel(CardStack cardStack, CardModel cardModel)
    {
        if (cardStack == null || cardModel == null)
            return;
        
        RecentCardStackIndex = CardStacks.IndexOf(cardStack);
        cardModel.DoubleClickEvent = DestroyCardModel;
    }

    public void AddCard(CardModel cardToAdd)
    {
        if (cardToAdd == null || CardStacks.Count < 1)
            return;

        // HACK: NOT SURE HOW TO MANAGE THE CARD INFO VIEWER AND CARD MODEL SELECTION/VISIBILITY
        EventSystem.current.SetSelectedGameObject(CardInfoViewer.Instance.gameObject, cardToAdd.RecentPointerEventData);
        CardInfoViewer.Instance.IsVisible = false;

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

        float newSpot = cardStackPrefab.GetComponent<RectTransform>().rect.width * ((float)RecentCardStackIndex + ((RecentCardStackIndex < CardStacks.Count / 2f) ? 0f : 1f)) / deckEditorContent.sizeDelta.x;
        deckEditorScrollbar.value = Mathf.Clamp01(newSpot);
    }

    public void DestroyCardModel(CardModel cardModel)
    {
        if (cardModel == null)
            return;

        GameObject.Destroy(cardModel.gameObject);
        // HACK: NOT SURE HOW TO MANAGE THE CARD INFO VIEWER AND CARD MODEL SELECTION/VISIBILITY
        CardInfoViewer.Instance.IsVisible = false;
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
        deckEditorNameText.text = DeckLoadMenu.DefaultDeckName;

        // HACK: NOT SURE HOW TO MANAGE THE CARD INFO VIEWER AND CARD MODEL SELECTION/VISIBILITY
        CardInfoViewer.Instance.IsVisible = false;
    }

    public string UpdateDeckName(string newName)
    {
        if (string.IsNullOrEmpty(newName))
            newName = DeckLoadMenu.DefaultDeckName;
        deckEditorNameText.text = UnityExtensionMethods.GetSafeFileName(newName);
        return deckEditorNameText.text;
    }

    public void ShowDeckLoadMenu()
    {
        DeckLoader.Show(LoadDeck, UpdateDeckName, deckEditorNameText.text);
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
        Deck deck = new Deck(deckEditorNameText.text);
        foreach (CardStack stack in CardStacks)
            foreach (CardModel card in stack.GetComponentsInChildren<CardModel>())
                deck.Cards.Add(card.RepresentedCard);
        DeckSaver.Show(deck, UpdateDeckName);
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
