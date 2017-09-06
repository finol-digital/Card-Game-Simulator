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
    public const string OverWriteDeckPrompt = "A deck with that name already exists. Overwrite?";

    public InputField saveDeckNameInputField;
    public TMPro.TMP_Text textOutputArea;

    private DeckNameChangeDelegate deckNameChangeCallback;
    private Deck deck;

    public void SetDeckNameChangeCallback(DeckNameChangeDelegate callbackNameChange)
    {
        deckNameChangeCallback = callbackNameChange;
    }

    public void Show(Deck deckToShow)
    {
        this.gameObject.SetActive(true);
        Debug.Log("Showing Deck Save Menu");
        this.transform.SetAsLastSibling();
        deck = deckToShow;
        saveDeckNameInputField.text = deckToShow.Name;
        textOutputArea.text = deck.ToString();
    }

    public void ChangeDeckName(string newName)
    {
        saveDeckNameInputField.text = deckNameChangeCallback(newName);
    }

    public void CopyDeckTextToClipboard()
    {
        UniClipboard.SetText(textOutputArea.text);
    }

    public void AttemptSaveAndHide()
    {
        Deck filePathFinder = new Deck(saveDeckNameInputField.text);
        if (File.Exists(filePathFinder.FilePath)) {
            Debug.Log("Attempted to save deck, but it already exists. Prompting user if they wish to overwrite: " + filePathFinder.FilePath);
            CardGameManager.Instance.PromptAction(OverWriteDeckPrompt, SaveDeckToFile);
        } else
            SaveDeckToFile();

        Hide();
    }

    public void SaveDeckToFile()
    {
        deck.Name = saveDeckNameInputField.text;
        Debug.Log("Saving deck to: " + deck.FilePath);
        try {
            if (!Directory.Exists(CardGameManager.Current.DecksFilePath)) {
                Debug.Log(CardGameManager.Current.DecksFilePath + " deck file directory does not exist, so creating it");
                Directory.CreateDirectory(CardGameManager.Current.DecksFilePath);
            }
            File.WriteAllText(deck.FilePath, textOutputArea.text);
            Debug.Log("Deck saved at: " + deck.FilePath);
        } catch (Exception e) {
            Debug.LogError("Failed to save deck!: " + e.Message);
            CardGameManager.Instance.ShowMessage("There was an error saving the deck to file: " + e.Message);
        }
    }

    public void Hide()
    {
        Debug.Log("Hiding the Deck Save Menu");
        this.gameObject.SetActive(false);
    }

    public void CancelAndHide()
    {
        deckNameChangeCallback(deck.Name);
        Hide();
    }
}
