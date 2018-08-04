using System;
using System.IO;
using CardGameDef;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CGS.Menus
{
    public delegate void OnDeckSavedDelegate(Deck savedDeck);

    public class DeckSaveMenu : MonoBehaviour
    {
        public const string DeckCopiedMessage = "The text for this deck has been copied to the clipboard.";
        public const string OverWriteDeckPrompt = "A deck with that name already exists. Overwrite?";
        public const string DeckSaveErrorMessage = "There was an error saving the deck to file: ";

        public InputField nameInputField;
        public TMPro.TMP_Text textOutputArea;

        public Deck CurrentDeck { get; private set; }
        public OnNameChangeDelegate NameChangeCallback { get; private set; }
        public OnDeckSavedDelegate DeckSaveCallback { get; private set; }
        public bool DoesAutoOverwrite { get; private set; }

        void LateUpdate()
        {
            if (nameInputField.isFocused || !Input.anyKeyDown || gameObject != CardGameManager.TopMenuCanvas?.gameObject)
                return;

            if ((Input.GetKeyDown(Inputs.BluetoothReturn) || Input.GetButtonDown(Inputs.Submit)) && EventSystem.current.currentSelectedGameObject == null)
                AttemptSaveAndHide();
            else if (Input.GetButtonDown(Inputs.FocusName))
                nameInputField.ActivateInputField();
            else if (Input.GetButtonDown(Inputs.Load) && EventSystem.current.currentSelectedGameObject == null)
                CopyTextToClipboard();
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(Inputs.Cancel))
                Hide();
        }

        public void Show(Deck deckToShow, OnNameChangeDelegate nameChangeCallback = null, OnDeckSavedDelegate deckSaveCallback = null, bool overwrite = false)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            CurrentDeck = deckToShow ?? new Deck(CardGameManager.Current);
            NameChangeCallback = nameChangeCallback;
            DeckSaveCallback = deckSaveCallback;
            DoesAutoOverwrite = overwrite;
            nameInputField.text = CurrentDeck.Name;
            textOutputArea.text = CurrentDeck.ToString();
        }

        public void ChangeName(string newName)
        {
            if (NameChangeCallback != null)
                newName = NameChangeCallback(newName);
            if (!string.IsNullOrEmpty(newName))
                nameInputField.text = newName;
            Deck newDeck = new Deck(CardGameManager.Current, newName, CardGameManager.Current.DeckFileType);
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
            Deck filePathFinder = new Deck(CardGameManager.Current, nameInputField.text, CardGameManager.Current.DeckFileType);
            if (!DoesAutoOverwrite && File.Exists(filePathFinder.FilePath))
                CardGameManager.Instance.Messenger.Prompt(OverWriteDeckPrompt, SaveToFile);
            else
                SaveToFile();

            Hide();
        }

        public void SaveToFile()
        {
            CurrentDeck.Name = nameInputField.text;
            SaveToFile(CurrentDeck, DeckSaveCallback);
        }

        public static void SaveToFile(Deck deck, OnDeckSavedDelegate deckSaveCallback = null)
        {
            try
            {
                if (!Directory.Exists(CardGameManager.Current.DecksFilePath))
                    Directory.CreateDirectory(CardGameManager.Current.DecksFilePath);
                File.WriteAllText(deck.FilePath, deck.ToString());
            }
            catch (Exception e)
            {
                Debug.LogError(DeckSaveErrorMessage + e.Message);
            }
            deckSaveCallback?.Invoke(deck);
        }

        public void CancelAndHide()
        {
            NameChangeCallback?.Invoke(CurrentDeck.Name);
            Hide();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
