/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.IO;
using CardGameDef;
using CardGameDef.Unity;
using Cgs.Menu;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cgs.Decks
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

        public UnityDeck CurrentDeck { get; private set; }
        public OnNameChangeDelegate NameChangeCallback { get; private set; }
        public OnDeckSavedDelegate DeckSaveCallback { get; private set; }
        public bool DoesAutoOverwrite { get; private set; }

        void Update()
        {
            if (!IsFocused || nameInputField.isFocused || !Input.anyKeyDown)
                return;

            if ((Input.GetKeyDown(Inputs.BluetoothReturn) || Input.GetButtonDown(Inputs.Submit)) &&
                EventSystem.current.currentSelectedGameObject == null)
                AttemptSaveAndHide();
            else if (Input.GetButtonDown(Inputs.FocusBack) || Math.Abs(Input.GetAxis(Inputs.FocusBack)) >
                                                           Inputs.Tolerance
                                                           || Input.GetButtonDown(Inputs.FocusNext) ||
                                                           Math.Abs(Input.GetAxis(Inputs.FocusNext)) > Inputs.Tolerance)
                nameInputField.ActivateInputField();
            else if (Input.GetButtonDown(Inputs.Load) && EventSystem.current.currentSelectedGameObject == null)
                Share();
            else if (Input.GetButtonDown(Inputs.Save) && EventSystem.current.currentSelectedGameObject == null)
                PrintPdf();
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(Inputs.Cancel))
                Hide();
        }

        public void Show(UnityDeck deckToShow, OnNameChangeDelegate nameChangeCallback = null,
            OnDeckSavedDelegate deckSaveCallback = null, bool overwrite = false)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            CurrentDeck = deckToShow ?? new UnityDeck(CardGameManager.Current);
            NameChangeCallback = nameChangeCallback;
            DeckSaveCallback = deckSaveCallback;
            DoesAutoOverwrite = overwrite;
            nameInputField.text = CurrentDeck.Name;
            textOutputArea.text = CurrentDeck.ToString();
        }

        [UsedImplicitly]
        public void ChangeName(string newName)
        {
            if (NameChangeCallback != null)
                newName = NameChangeCallback(newName);
            if (!string.IsNullOrEmpty(newName))
                nameInputField.text = newName;
            var deck = new UnityDeck(CardGameManager.Current, newName, CardGameManager.Current.DeckFileType);
            foreach (Card card in deck.Cards)
                deck.Add((UnityCard) card);
            textOutputArea.text = deck.ToString();
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
            UnityDeck deck = CurrentDeck;
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
            CardGameManager.Instance.Messenger.Show(DeckPrintOpenPathErrorMessage + pdfUri.AbsoluteUri);
            Application.OpenURL(pdfUri.AbsoluteUri); // This will likely fail, so its wrapped with the path as backup
            CardGameManager.Instance.Messenger.Show(DeckPrintOpenPathErrorMessage + pdfUri.AbsoluteUri);
#endif
        }

        [UsedImplicitly]
        public void EnableSubmit()
        {
            if (!EventSystem.current.alreadySelecting)
                EventSystem.current.SetSelectedGameObject(null);
        }

        public void AttemptSaveAndHide()
        {
            UnityDeck filePathFinder = new UnityDeck(CardGameManager.Current, nameInputField.text,
                CardGameManager.Current.DeckFileType);
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

        public static void SaveToFile(UnityDeck deck, OnDeckSavedDelegate deckSaveCallback = null)
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

        [UsedImplicitly]
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
