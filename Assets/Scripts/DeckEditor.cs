using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class DeckEditor : MonoBehaviour
{
    public const string DefaultDeckName = "Untitled";

    public GameObject cardPrefab;
    public GameObject cardStackPrefab;
    public RectTransform deckEditorContent;
    public Text deckEditorNameText;
    public RectTransform loadDeckSelectionContent;
    public RectTransform loadDeckSelectionTemplate;
    public Button loadDeckFromFileButton;
    public InputField loadDeckNameInputField;
    public InputField saveDeckNameInputField;
    public TMPro.TMP_InputField textInputField;
    public TMPro.TMP_Text textOutputArea;

    private List<CardStack> cardStacks;
    private Deck deck;
    private string selectedDeckFileName;

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
        deck = new Deck(DefaultDeckName);
    }

    public void AddCard(Card cardToAdd)
    {
        Debug.Log("Adding to the deck: " + cardToAdd.Name);
        CardInfoViewer.Instance.DeselectCard();
        foreach (CardStack stack in cardStacks) {
            if (stack.transform.childCount < CardGameManager.Current.CopiesOfCardPerDeck) {
                GameObject cardCopy = Instantiate(cardPrefab, stack.transform);
                CardModel copyModel = cardCopy.transform.GetOrAddComponent<CardModel>();
                copyModel.SetAsCard(cardToAdd, false);
                return;
            }
        }
        Debug.LogWarning("Failed to find an open stack to which we could add a card! Card not added.");
    }

    public void Clear()
    {
        Debug.Log("Clearing the deck editor");
        deck = new Deck(DefaultDeckName);
        foreach (CardStack stack in cardStacks)
            stack.transform.DestroyAllChildren();
        Debug.Log("Deck Editor cleared");
    }

    public void SetDeckEditorName(string name)
    {
        if (string.IsNullOrEmpty(name))
            name = DefaultDeckName;
        deckEditorNameText.text = name;
    }

    public void RevertDeckName()
    {
        deckEditorNameText.text = deck.Name;
    }

    public void PasteClipboardIntoDeckText()
    {
        textInputField.text = UniClipboard.GetText();
    }

    public void CopyTextToClipboard()
    {
        UniClipboard.SetText(textOutputArea.text);
    }

    public void PopulateLoadFilePanel()
    {
        Debug.Log("Populating File Load Panel");
        string[] deckFiles = Directory.Exists(CardGameManager.Current.DecksFilePath) ? Directory.GetFiles(CardGameManager.Current.DecksFilePath) : new string[0];

        loadDeckSelectionContent.DestroyAllChildren();
        loadDeckSelectionTemplate.SetParent(loadDeckSelectionContent);
        loadDeckSelectionContent.sizeDelta = new Vector2(loadDeckSelectionContent.sizeDelta.x, loadDeckSelectionTemplate.rect.height * deckFiles.Length);
        Vector3 pos = loadDeckSelectionTemplate.localPosition;
        pos.y = 0;
        foreach (string deckFile in deckFiles) {
            GameObject deckFileSelection = Instantiate(loadDeckSelectionTemplate.gameObject, loadDeckSelectionContent) as GameObject;
            deckFileSelection.SetActive(true);
            deckFileSelection.transform.localPosition = pos;
            Toggle toggle = deckFileSelection.GetComponent<Toggle>();
            toggle.isOn = false;
            UnityAction<bool> valueChange = new UnityAction<bool>(isOn => SelectDeckFileToLoad(deckFile, isOn));
            toggle.onValueChanged.AddListener(valueChange);
            Text labelText = deckFileSelection.GetComponentInChildren<Text>();
            labelText.text = deckFile.Substring(deckFile.LastIndexOf(Path.DirectorySeparatorChar) + 1);
            pos.y -= loadDeckSelectionTemplate.rect.height;
        }
        loadDeckSelectionTemplate.SetParent(loadDeckSelectionContent.parent);

        bool hasFiles = deckFiles.Length > 0;
        if (hasFiles)
            loadDeckSelectionContent.GetChild(0).GetComponent<Toggle>().isOn = true;
        loadDeckSelectionTemplate.gameObject.SetActive(!hasFiles);
        loadDeckFromFileButton.interactable = hasFiles;

        Debug.Log("Populated File Load Panel");
    }

    public void SelectDeckFileToLoad(string deckFileName, bool isOn)
    {
        if (!isOn)
            return;
        selectedDeckFileName = deckFileName;
        Debug.Log("Selected to load deck: " + selectedDeckFileName);
    }

    public void LoadDeckFromFile()
    {
        Debug.Log("Loading Deck from file: " + selectedDeckFileName);

        int startName = selectedDeckFileName.LastIndexOf(Path.DirectorySeparatorChar) + 1;
        int endName = selectedDeckFileName.LastIndexOf('.');
        string deckName = selectedDeckFileName.Substring(startName, endName - startName);

        string deckText = "";
        try { 
            deckText = File.ReadAllText(selectedDeckFileName);
        } catch (Exception e) {
            // TODO: BETTER ERROR HANDLING
            Debug.LogError("Failed to load deck!: " + e.Message);
        }

        Deck newDeck = new Deck(deckName, deckText);
        LoadDeck(newDeck);

        Debug.Log("Deck Loaded from file");
    }

    public void LoadDeckFromText()
    {
        Debug.Log("Loading Deck from text: " + textInputField.text);
        Deck newDeck = new Deck(deckEditorNameText.text, textInputField.text);
        LoadDeck(newDeck);
        Debug.Log("Deck Loaded from text");
    }

    public void LoadDeck(Deck newDeck)
    {
        Clear();
        deck = newDeck;
        SetDeckEditorName(deck.Name);
        foreach (Card card in deck.Cards)
            AddCard(card);
    }

    public void PopulateSavePanel()
    {
        Debug.Log("Populating Save Panel");
        saveDeckNameInputField.text = deckEditorNameText.text;
        deck = new Deck(deckEditorNameText.text);
        foreach (CardStack stack in cardStacks)
            foreach (CardModel card in stack.GetComponentsInChildren<CardModel>())
                deck.Cards.Add(card.RepresentedCard);
        textOutputArea.text = deck.ToString();
        Debug.Log("Save Panel Populated");
    }

    public void SaveDeckToFile()
    {
        if (!Directory.Exists(CardGameManager.Current.FilePathBase)) {
            Debug.LogWarning("Attempted to save deck when the folder for the card game does not exist. Ignoring the request for: " + CardGameManager.Current.Name);
            return;
        }

        deck.Name = deckEditorNameText.text;
        string deckFilePath = CardGameManager.Current.DecksFilePath + "/" + deck.Name + "." + CardGameManager.Current.DeckFileType;
        Debug.Log("Saving deck to: " + deckFilePath);
        if (File.Exists(deckFilePath)) {
            // TODO: WARN THE USER THAT THE DECK ALREADY EXISTS, AND CONFIRM IF THEY WANT TO OVERWRITE
        }

        try {
            if (!Directory.Exists(CardGameManager.Current.DecksFilePath)) {
                Debug.Log(CardGameManager.Current.DecksFilePath + " deck file directory does not exist, so creating it");
                Directory.CreateDirectory(CardGameManager.Current.DecksFilePath);
            }
            File.WriteAllText(deckFilePath, textOutputArea.text);
            Debug.Log("Deck saved at: " + deckFilePath);
        } catch (Exception e) {
            // TODO: BETTER ERROR HANDLING
            Debug.LogError("Failed to save deck: " + e.ToString());
        }
    }

}
