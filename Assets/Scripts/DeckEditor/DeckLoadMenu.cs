using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public delegate void OnDeckLoadedDelegate(Deck loadedDeck);

public class DeckLoadMenu : MonoBehaviour
{
    public RectTransform fileSelectionArea;
    public RectTransform fileSelectionTemplate;
    public Button loadDeckFromFileButton;
    public InputField deckNameInputField;
    public TMPro.TMP_InputField textInputField;

    private OnDeckLoadedDelegate _deckLoadCallback;
    private DeckNameChangeDelegate _deckNameChangeCallback;
    private string _originalDeckName;
    private string _selectedDeckFileName;

    public void Show(OnDeckLoadedDelegate callbackDeckLoad, DeckNameChangeDelegate callbackNameChange, string originalDeckName)
    {
        this.gameObject.SetActive(true);
        Debug.Log("Showing Deck Load Menu");
        this.transform.SetAsLastSibling();
        _deckLoadCallback = callbackDeckLoad;
        _deckNameChangeCallback = callbackNameChange;
        _originalDeckName = originalDeckName;
        _selectedDeckFileName = "";
        string[] deckFiles = Directory.Exists(CardGameManager.Current.DecksFilePath) ? Directory.GetFiles(CardGameManager.Current.DecksFilePath) : new string[0];

        fileSelectionArea.DestroyAllChildren();
        fileSelectionTemplate.SetParent(fileSelectionArea);
        Vector3 pos = fileSelectionTemplate.localPosition;
        pos.y = 0;
        foreach (string deckFile in deckFiles) {
            GameObject deckFileSelection = Instantiate(fileSelectionTemplate.gameObject, fileSelectionArea) as GameObject;
            deckFileSelection.SetActive(true);
            deckFileSelection.transform.localPosition = pos;
            Toggle toggle = deckFileSelection.GetComponent<Toggle>();
            toggle.isOn = false;
            UnityAction<bool> valueChange = new UnityAction<bool>(isOn => SelectDeckFileToLoad(isOn, deckFile));
            toggle.onValueChanged.AddListener(valueChange);
            Text labelText = deckFileSelection.GetComponentInChildren<Text>();
            labelText.text = deckFile.Substring(deckFile.LastIndexOf(Path.DirectorySeparatorChar) + 1);
            pos.y -= fileSelectionTemplate.rect.height;
        }
        fileSelectionTemplate.SetParent(fileSelectionArea.parent);
        fileSelectionArea.sizeDelta = new Vector2(fileSelectionArea.sizeDelta.x, fileSelectionTemplate.rect.height * deckFiles.Length);

        bool hasFiles = deckFiles.Length > 0;
        if (hasFiles)
            fileSelectionArea.GetChild(0).GetComponent<Toggle>().isOn = true;
        fileSelectionTemplate.gameObject.SetActive(!hasFiles);
        loadDeckFromFileButton.interactable = hasFiles;
    }

    public void SelectDeckFileToLoad(bool isSelected, string deckFileName)
    {
        if (!isSelected || string.IsNullOrEmpty(deckFileName))
            return;

        Debug.Log("Selected to load deck: " + deckFileName);
        _deckNameChangeCallback(GetDeckNameFromPath(deckFileName));
        if (deckFileName.Equals(_selectedDeckFileName))
            LoadDeckFromFileAndHide();
        _selectedDeckFileName = deckFileName;
    }

    public string GetDeckNameFromPath(string deckFilePath)
    {
        int startName = deckFilePath.LastIndexOf(Path.DirectorySeparatorChar) + 1;
        int endName = deckFilePath.LastIndexOf('.');
        return deckFilePath.Substring(startName, endName - startName);
    }

    public void LoadDeckFromFileAndHide()
    {
        Debug.Log("Loading Deck from file: " + _selectedDeckFileName);

        string deckText = "";
        try { 
            deckText = File.ReadAllText(_selectedDeckFileName);
        } catch (Exception e) {
            Debug.LogError("Failed to load deck!: " + e.Message);
            CardGameManager.Instance.ShowMessage("There was an error while attempting to read the deck list from file: " + e.Message);
        }

        Deck newDeck = new Deck(GetDeckNameFromPath(_selectedDeckFileName), deckText);
        _deckLoadCallback(newDeck);
        Debug.Log("Deck Loaded from file");
        Hide();
    }

    public void ChangeDeckName(string newName)
    {
        deckNameInputField.text = _deckNameChangeCallback(newName);
    }

    public void PasteClipboardIntoDeckText()
    {
        textInputField.text = UniClipboard.GetText();
    }

    public void LoadDeckFromTextAndHide()
    {
        Debug.Log("Loading Deck from text: " + textInputField.text);
        Deck newDeck = new Deck(deckNameInputField.text, textInputField.text);
        _deckLoadCallback(newDeck);
        Debug.Log("Deck Loaded from text");
        Hide();
    }

    public void CancelAndHide()
    {
        _deckNameChangeCallback(_originalDeckName);
        Hide();
    }

    public void Hide()
    {
        Debug.Log("Hiding the Deck Load Menu");
        this.gameObject.SetActive(false);
    }
}
