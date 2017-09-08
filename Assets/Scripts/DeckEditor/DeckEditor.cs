using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public delegate string DeckNameChangeDelegate(string newName);

public class DeckEditor : MonoBehaviour
{
    public const string DefaultDeckName = "Untitled";
    public const string NewDeckPrompt = "Clear the editor and start a new Untitled deck?";

    public GameObject cardModelPrefab;
    public GameObject cardStackPrefab;
    public GameObject deckLoadMenuPrefab;
    public GameObject deckSaveMenuPrefab;
    public RectTransform deckEditorContent;
    public Text deckEditorNameText;

    private List<CardStack> _cardStacks;
    private DeckLoadMenu _deckLoader;
    private DeckSaveMenu _deckSaver;

    void Start()
    {
        CardGameManager.Instance.AddOnSelectAction(UpdateDeckEditor);
    }

    public void UpdateDeckEditor()
    {
        Debug.Log("Creating card stacks in the deck editor");
        deckEditorContent.DestroyAllChildren();
        int numCardStacks = CardGameManager.Current.DeckCardStackCount;
        for (int i = 0; i < numCardStacks; i++) {
            GameObject newCardStack = Instantiate(cardStackPrefab, deckEditorContent);
            CardStacks.Add(newCardStack.transform.GetOrAddComponent<CardStack>());
        }
        deckEditorContent.sizeDelta = new Vector2(cardStackPrefab.GetComponent<RectTransform>().rect.width * numCardStacks, deckEditorContent.sizeDelta.y);
    }

    public void AddCard(CardModel cardToAdd)
    {
        if (cardToAdd == null) {
            Debug.LogWarning("Attempted to add a null card model to the Deck Editor! Ignoring");
            return;
        }

        // HACK: NOT SURE HOW TO MANAGE THE CARD INFO VIEWER AND CARD MODEL SELECTION/VISIBILITY
        EventSystem.current.SetSelectedGameObject(CardInfoViewer.Instance.gameObject, cardToAdd.RecentEventData);
        CardInfoViewer.Instance.IsVisible = false;
        AddCard(cardToAdd.RepresentedCard);
    }

    public void AddCard(Card cardToAdd)
    {
        if (cardToAdd == null) {
            Debug.LogWarning("Attempted to add a null card to the Deck Editor! Ignoring");
            return;
        }

        Debug.Log("Adding to the deck editor: " + cardToAdd.Name);
        // TODO: KEEP TRACK OF PREVIOUSLY USED STACK, AND ADD TO THE LAST STACK; WHEN ADDED, MOVE THE VIEW SO THAT THE ADDED CARD IS VISIBLE
        foreach (CardStack stack in _cardStacks) {
            if (stack.transform.childCount < CardGameManager.Current.CopiesOfCardPerDeck) {
                GameObject cardCopy = Instantiate(cardModelPrefab, stack.transform);
                CardModel copyModel = cardCopy.transform.GetOrAddComponent<CardModel>();
                copyModel.SetAsCard(cardToAdd, false, DestroyCardModel);
                return;
            }
        }
        Debug.LogWarning("Failed to find an open stack to which we could add a card! Card not added.");
    }

    public void DestroyCardModel(CardModel cardModel)
    {
        GameObject.Destroy(cardModel.gameObject);
        CardInfoViewer.Instance.IsVisible = false;
    }

    public void PromptForClear()
    {
        CardGameManager.Instance.PromptAction(NewDeckPrompt, Clear);
    }

    public void Clear()
    {
        Debug.Log("Clearing the deck editor");
        foreach (CardStack stack in _cardStacks)
            stack.transform.DestroyAllChildren();
        deckEditorNameText.text = DefaultDeckName;
        Debug.Log("Deck Editor cleared");
    }

    public string UpdateDeckName(string newName)
    {
        if (string.IsNullOrEmpty(newName))
            newName = DefaultDeckName;
        deckEditorNameText.text = UnityExtensionMethods.GetSafeFilename(newName);
        return deckEditorNameText.text;
    }

    public void ShowDeckLoadMenu()
    {
        DeckLoader.Show(LoadDeck, UpdateDeckName, deckEditorNameText.text);
    }

    public void LoadDeck(Deck newDeck)
    {
        if (newDeck == null) {
            Debug.LogWarning("Attempted to load a null deck into the deck editor! Ignoring");
            return;
        }

        Clear();
        UpdateDeckName(newDeck.Name);
        foreach (Card card in newDeck.Cards)
            AddCard(card);
    }

    public void ShowDeckSaveMenu()
    {
        Deck deck = new Deck(deckEditorNameText.text);
        foreach (CardStack stack in _cardStacks)
            foreach (CardModel card in stack.GetComponentsInChildren<CardModel>())
                deck.Cards.Add(card.RepresentedCard);
        DeckSaver.Show(deck, UpdateDeckName);
    }

    public List<CardStack> CardStacks {
        get {
            if (_cardStacks == null)
                _cardStacks = new List<CardStack>();
            return _cardStacks;
        }
    }

    public DeckLoadMenu DeckLoader {
        get {
            if (_deckLoader == null)
                _deckLoader = Instantiate(deckLoadMenuPrefab, UnityExtensionMethods.FindInParents<Canvas>(this.gameObject).transform).transform.GetOrAddComponent<DeckLoadMenu>();
            return _deckLoader;
        }
    }

    public DeckSaveMenu DeckSaver {
        get {
            if (_deckSaver == null)
                _deckSaver = Instantiate(deckSaveMenuPrefab, UnityExtensionMethods.FindInParents<Canvas>(this.gameObject).transform).transform.GetOrAddComponent<DeckSaveMenu>();
            return _deckSaver;
        }
    }
}
