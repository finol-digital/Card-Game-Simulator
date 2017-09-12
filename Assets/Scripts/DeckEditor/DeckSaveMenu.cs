using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class DeckSaveMenu : MonoBehaviour
{
    public const string DeckCopiedMessage = "The text for this deck has been copied to the clipboard.";
    public const string OverWriteDeckPrompt = "A deck with that name already exists. Overwrite?";

    public InputField saveDeckNameInputField;
    public TMPro.TMP_Text textOutputArea;

    private Deck _deck;
    private DeckNameChangeDelegate _deckNameChangeCallback;

    public void Show(Deck deckToShow, DeckNameChangeDelegate callbackNameChange)
    {
        this.gameObject.SetActive(true);
        this.transform.SetAsLastSibling();
        _deck = deckToShow;
        _deckNameChangeCallback = callbackNameChange;
        saveDeckNameInputField.text = deckToShow.Name;
        textOutputArea.text = _deck.ToString();
    }

    public void ChangeDeckName(string changedName)
    {
        string newName = _deckNameChangeCallback(changedName);
        if (!string.IsNullOrEmpty(changedName))
            saveDeckNameInputField.text = newName;
        Deck newDeck = new Deck(newName);
        newDeck.Cards = new List<Card>(_deck.Cards);
        textOutputArea.text = newDeck.ToString();
    }

    public void CopyDeckTextToClipboard()
    {
        UniClipboard.SetText(textOutputArea.text);
        CardGameManager.Instance.Popup.Show(DeckCopiedMessage);
    }

    public void AttemptSaveAndHide()
    {
        Deck filePathFinder = new Deck(saveDeckNameInputField.text);
        if (File.Exists(filePathFinder.FilePath))
            CardGameManager.Instance.Popup.Prompt(OverWriteDeckPrompt, SaveDeckToFile);
        else
            SaveDeckToFile();

        Hide();
    }

    public void SaveDeckToFile()
    {
        _deck.Name = saveDeckNameInputField.text;
        SaveToFile(_deck);
    }

    public static void SaveToFile(Deck deck)
    {
        try {
            if (!Directory.Exists(CardGameManager.Current.DecksFilePath))
                Directory.CreateDirectory(CardGameManager.Current.DecksFilePath);
            File.WriteAllText(deck.FilePath, deck.ToString());
        } catch (Exception e) {
            Debug.LogError("Failed to save deck!: " + e.Message);
            CardGameManager.Instance.Popup.Show("There was an error saving the deck to file: " + e.Message);
        }
    }

    public void CancelAndHide()
    {
        _deckNameChangeCallback(_deck.Name);
        Hide();
    }

    public void Hide()
    {
        this.gameObject.SetActive(false);
    }
}
