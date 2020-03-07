/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

using CardGameDef;
using CGS.Menu;

namespace CGS.Decks
{
    public delegate void OnDeckSavedDelegate(Deck savedDeck);

    public class DeckSaveMenu : Modal
    {
        public const string DeckCopiedMessage = "The text for this deck has been copied to the clipboard.";
        public const string OverWriteDeckPrompt = "A deck with that name already exists. Overwrite?";
        public const string DeckSaveErrorMessage = "There was an error saving the deck to file: ";
        public const string DeckPrintErrorMessage = "There was an error printing the deck as pdf: ";
        public const string DeckPrintOpenErrorMessage = "Unable to open the deck pdf! ";
        public const string DeckPrintOpenPathErrorMessage = "Please check: ";

        public InputField nameInputField;
        public TMP_Text textOutputArea;

        public Deck CurrentDeck { get; private set; }
        public OnNameChangeDelegate NameChangeCallback { get; private set; }
        public OnDeckSavedDelegate DeckSaveCallback { get; private set; }
        public bool DoesAutoOverwrite { get; private set; }

        void Update()
        {
            if (!IsFocused || nameInputField.isFocused || !Input.anyKeyDown)
                return;

            if ((Input.GetKeyDown(Inputs.BluetoothReturn) || Input.GetButtonDown(Inputs.Submit)) && EventSystem.current.currentSelectedGameObject == null)
                AttemptSaveAndHide();
            else if (Input.GetButtonDown(Inputs.FocusBack) || Input.GetAxis(Inputs.FocusBack) != 0
                        || Input.GetButtonDown(Inputs.FocusNext) || Input.GetAxis(Inputs.FocusNext) != 0)
                nameInputField.ActivateInputField();
            else if (Input.GetButtonDown(Inputs.Load) && EventSystem.current.currentSelectedGameObject == null)
                Share();
            else if (Input.GetButtonDown(Inputs.Save) && EventSystem.current.currentSelectedGameObject == null)
                PrintPdf();
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

        public void Share()
        {
            string shareText = textOutputArea.text;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            (new NativeShare()).SetText(shareText).Share();
#else
            UniClipboard.SetText(shareText);
            CardGameManager.Instance.Messenger.Show(DeckCopiedMessage);
#endif
        }

        public void PrintPdf()
        {
            CurrentDeck.Name = nameInputField.text;
            Deck deck = CurrentDeck;
            Uri pdfUri = null;
            try
            {
                pdfUri = deck.PrintPdf();
            }
            catch (Exception e)
            {
                Debug.LogError(DeckPrintErrorMessage + e.Message + e.StackTrace);
                CardGameManager.Instance.Messenger.Show(DeckPrintErrorMessage + e.Message);
            }
            if (pdfUri == null || !pdfUri.IsAbsoluteUri)
            {
                Debug.LogError(DeckPrintOpenErrorMessage);
                CardGameManager.Instance.Messenger.Show(DeckPrintOpenErrorMessage);
            }
            else
            {
                StartCoroutine(OpenPdf(pdfUri));
            }
        }
        private IEnumerator OpenPdf(Uri pdfUri)
        {
            yield return null;
#if ENABLE_WINMD_SUPPORT
            bool success = false;
            try
            {
                success = Windows.System.Launcher.LaunchUriAsync(pdfUri).GetAwaiter().GetResult();
                if (!success)
                {
                    Debug.LogError(DeckPrintOpenPathErrorMessage + pdfUri.AbsoluteUri);
                    CardGameManager.Instance.Messenger.Show(DeckPrintOpenPathErrorMessage + pdfUri.AbsoluteUri);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message + e.StackTrace);
                CardGameManager.Instance.Messenger.Show(DeckPrintOpenPathErrorMessage + pdfUri.AbsoluteUri);
            }
#else
            Application.OpenURL(pdfUri.AbsoluteUri);
#endif
        }

        public void EnableSubmit()
        {
            if (!EventSystem.current.alreadySelecting)
                EventSystem.current.SetSelectedGameObject(null);
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
