using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    private List<CardStack> cardStacks;
    private DeckLoadMenu deckLoader;
    private DeckSaveMenu deckSaver;

    IEnumerator Start()
    {
        Debug.Log("Deck Editor is waiting for the card game manager to finish loading");
        while (!CardGameManager.IsLoaded)
            yield return null;

        Debug.Log("Creating card stacks in the deck editor");
        cardStacks = new List<CardStack>();
        int numCardStacks = CardGameManager.Current.DeckCardStackCount;
        for (int i = 0; i < numCardStacks; i++) {
            GameObject newCardStack = Instantiate(cardStackPrefab, deckEditorContent);
            cardStacks.Add(newCardStack.transform.GetOrAddComponent<CardStack>());
        }
        deckEditorContent.sizeDelta = new Vector2(cardStackPrefab.GetComponent<RectTransform>().rect.width * numCardStacks, deckEditorContent.sizeDelta.y);
    }

    public void AddCard(Card cardToAdd)
    {
        Debug.Log("Adding to the deck: " + cardToAdd.Name);
        CardInfoViewer.Instance.DeselectCard();
        // TODO: KEEP TRACK OF PREVIOUSLY USED STACK, AND ADD TO THE LAST STACK; WHEN ADDED, MOVE THE VIEW SO THAT THE ADDED CARD IS VISIBLE
        foreach (CardStack stack in cardStacks) {
            if (stack.transform.childCount < CardGameManager.Current.CopiesOfCardPerDeck) {
                GameObject cardCopy = Instantiate(cardModelPrefab, stack.transform);
                CardModel copyModel = cardCopy.transform.GetOrAddComponent<CardModel>();
                copyModel.SetAsCard(cardToAdd, false);
                return;
            }
        }
        Debug.LogWarning("Failed to find an open stack to which we could add a card! Card not added.");
    }

    public void PromptForClear()
    {
        CardGameManager.Instance.PromptAction(NewDeckPrompt, Clear);
    }

    public void Clear()
    {
        Debug.Log("Clearing the deck editor");
        foreach (CardStack stack in cardStacks)
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
        DeckLoader.Show(deckEditorNameText.text);
    }

    public void LoadDeck(Deck newDeck)
    {
        Clear();
        UpdateDeckName(newDeck.Name);
        foreach (Card card in newDeck.Cards)
            AddCard(card);
    }

    public void ShowDeckSaveMenu()
    {
        Deck deck = new Deck(deckEditorNameText.text);
        foreach (CardStack stack in cardStacks)
            foreach (CardModel card in stack.GetComponentsInChildren<CardModel>())
                deck.Cards.Add(card.RepresentedCard);
        DeckSaver.Show(deck);
    }

    public DeckLoadMenu DeckLoader {
        get {
            if (deckLoader == null) {
                deckLoader = Instantiate(deckLoadMenuPrefab, UnityExtensionMethods.FindInParents<Canvas>(this.gameObject).transform).transform.GetOrAddComponent<DeckLoadMenu>();
                deckLoader.SetCallBacks(LoadDeck, UpdateDeckName);
            }
            return deckLoader;
        }
    }

    public DeckSaveMenu DeckSaver {
        get {
            if (deckSaver == null) {
                deckSaver = Instantiate(deckSaveMenuPrefab, UnityExtensionMethods.FindInParents<Canvas>(this.gameObject).transform).transform.GetOrAddComponent<DeckSaveMenu>();
                deckSaver.SetDeckNameChangeCallback(UpdateDeckName);
            }
            return deckSaver;
        }
    }
}
