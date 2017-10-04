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

    public InputField nameInputField;
    public TMPro.TMP_Text textOutputArea;

    public Deck CurrentDeck { get; private set; }

    public DeckNameChangeDelegate NameChangeCallback { get; private set; }

    public void Show(Deck deckToShow, DeckNameChangeDelegate nameChangeCallback)
    {
        this.gameObject.SetActive(true);
        this.transform.SetAsLastSibling();
        CurrentDeck = deckToShow;
        NameChangeCallback = nameChangeCallback;
        nameInputField.text = deckToShow.Name;
        textOutputArea.text = CurrentDeck.ToString();
    }

    public void ChangeName(string newName)
    {
        newName = NameChangeCallback(newName);
        if (!string.IsNullOrEmpty(newName))
            nameInputField.text = newName;
        Deck newDeck = new Deck(newName);
        newDeck.Cards = new List<Card>(CurrentDeck.Cards);
        textOutputArea.text = newDeck.ToString();
    }

    public void CopyTextToClipboard()
    {
        UniClipboard.SetText(textOutputArea.text);
        CardGameManager.Instance.Popup.Show(DeckCopiedMessage);
    }

    public void AttemptSaveAndHide()
    {
        Deck filePathFinder = new Deck(nameInputField.text);
        if (File.Exists(filePathFinder.FilePath))
            CardGameManager.Instance.Popup.Prompt(OverWriteDeckPrompt, SaveToFile);
        else
            SaveToFile();

        Hide();
    }

    public void SaveToFile()
    {
        CurrentDeck.Name = nameInputField.text;
        SaveToFile(CurrentDeck);
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
        NameChangeCallback(CurrentDeck.Name);
        Hide();
    }

    public void Hide()
    {
        this.gameObject.SetActive(false);
    }
}
