using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public delegate void OnDeckSavedDelegate(Deck savedDeck);

public class DeckSaveMenu : MonoBehaviour
{
    public const string DeckCopiedMessage = "The text for this deck has been copied to the clipboard.";
    public const string OverWriteDeckPrompt = "A deck with that name already exists. Overwrite?";

    public InputField nameInputField;
    public TMPro.TMP_Text textOutputArea;

    public Deck CurrentDeck { get; private set; }

    public OnDeckNameChangeDelegate NameChangeCallback { get; private set; }

    public OnDeckSavedDelegate DeckSaveCallback { get; private set; }

    public void Show(Deck deckToShow, OnDeckNameChangeDelegate nameChangeCallback = null, OnDeckSavedDelegate deckSaveCallback = null)
    {
        this.gameObject.SetActive(true);
        this.transform.SetAsLastSibling();
        CurrentDeck = deckToShow ?? new Deck();
        NameChangeCallback = nameChangeCallback;
        DeckSaveCallback = deckSaveCallback;
        nameInputField.text = CurrentDeck.Name;
        textOutputArea.text = CurrentDeck.ToString();
    }

    public void ChangeName(string newName)
    {
        if (NameChangeCallback != null)
            newName = NameChangeCallback(newName);
        if (!string.IsNullOrEmpty(newName))
            nameInputField.text = newName;
        Deck newDeck = new Deck(newName, CardGameManager.Current.DeckFileType);
        newDeck.Cards.AddRange(CurrentDeck.Cards);
        textOutputArea.text = newDeck.ToString();
    }

    public void CopyTextToClipboard()
    {
        UniClipboard.SetText(textOutputArea.text);
        CardGameManager.Instance.Messenger.Show(DeckCopiedMessage);
    }

    public void AttemptSaveAndHide()
    {
        Deck filePathFinder = new Deck(nameInputField.text, CardGameManager.Current.DeckFileType);
        if (File.Exists(filePathFinder.FilePath))
            CardGameManager.Instance.Messenger.Prompt(OverWriteDeckPrompt, SaveToFile);
        else
            SaveToFile();

        Hide();
    }

    public void SaveToFile()
    {
        CurrentDeck.Name = nameInputField.text;
        DeckSaveMenu.SaveToFile(CurrentDeck, DeckSaveCallback);
    }

    public static void SaveToFile(Deck deck, OnDeckSavedDelegate deckSaveCallback = null)
    {
        try {
            if (!Directory.Exists(CardGameManager.Current.DecksFilePath))
                Directory.CreateDirectory(CardGameManager.Current.DecksFilePath);
            File.WriteAllText(deck.FilePath, deck.ToString());
        } catch (Exception e) {
            Debug.LogError("Failed to save deck!: " + e.Message);
            CardGameManager.Instance.Messenger.Show("There was an error saving the deck to file: " + e.Message);
        }
        if (deckSaveCallback != null)
            deckSaveCallback(deck);
    }

    public void CancelAndHide()
    {
        if (NameChangeCallback != null)
            NameChangeCallback(CurrentDeck.Name);
        Hide();
    }

    public void Hide()
    {
        this.gameObject.SetActive(false);
    }
}
