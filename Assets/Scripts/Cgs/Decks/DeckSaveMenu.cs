/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
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
        public Button printPdfButton;

        private UnityDeck _currentDeck;
        private OnNameChangeDelegate _nameChangeCallback;
        private OnDeckSavedDelegate _deckSaveCallback;
        private bool _doesAutoOverwrite;

        private void Update()
        {
            if (!IsFocused || nameInputField.isFocused || !Input.anyKeyDown)
                return;

            if (Inputs.IsSubmit && EventSystem.current.currentSelectedGameObject == null)
                AttemptSaveAndHide();
            else if (Inputs.IsFocus)
                nameInputField.ActivateInputField();
            else if (Inputs.IsLoad && EventSystem.current.currentSelectedGameObject == null)
                Share();
            else if (Inputs.IsSave && EventSystem.current.currentSelectedGameObject == null)
                PrintPdf();
            else if (Inputs.IsCancel)
                CancelAndHide();
        }

        public void Show(UnityDeck deckToShow, OnNameChangeDelegate nameChangeCallback = null,
            OnDeckSavedDelegate deckSaveCallback = null, bool overwrite = false)
        {
            Show();
            _currentDeck = deckToShow ?? new UnityDeck(CardGameManager.Current);
            _nameChangeCallback = nameChangeCallback;
            _deckSaveCallback = deckSaveCallback;
            _doesAutoOverwrite = overwrite;
            nameInputField.text = _currentDeck.Name;
            textOutputArea.text = _currentDeck.ToString();
#if UNITY_WEBGL
            printPdfButton.interactable = false;
#endif
        }

        [UsedImplicitly]
        public void ChangeName(string newName)
        {
            if (_nameChangeCallback != null)
                newName = _nameChangeCallback(newName);
            if (!string.IsNullOrEmpty(newName))
                nameInputField.text = newName;
            var deck = new UnityDeck(CardGameManager.Current, newName, CardGameManager.Current.DeckFileType);
            foreach (var card in _currentDeck.Cards)
                deck.Add((UnityCard) card);
            textOutputArea.text = deck.ToString();
        }

        [UsedImplicitly]
        public void Share()
        {
            var shareText = textOutputArea.text;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            (new NativeShare()).SetText(shareText).Share();
#else
            UniClipboard.SetText(shareText);
            CardGameManager.Instance.Messenger.Show(DeckCopiedMessage);
#endif
        }

        [UsedImplicitly]
        public void PrintPdf()
        {
            _currentDeck.Name = nameInputField.text;
            var deck = _currentDeck;
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
                return;
            }

#if ENABLE_WINMD_SUPPORT
            UnityEngine.WSA.Application.InvokeOnUIThread(async () => {
                try
                {
                    var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(pdfUri.LocalPath);
                    if (file != null)
                    {
                        // Launch the retrieved file
                        var success = await Windows.System.Launcher.LaunchFileAsync(file);
                        if (!success)
                        {
                            Debug.LogError(DeckPrintOpenPathErrorMessage + pdfUri.LocalPath);
                            CardGameManager.Instance.Messenger.Show(DeckPrintOpenPathErrorMessage + pdfUri.LocalPath);
                        }
                    }
                    else
                    {
                        Debug.LogError(DeckPrintOpenPathErrorMessage + pdfUri.LocalPath);
                        CardGameManager.Instance.Messenger.Show(DeckPrintOpenPathErrorMessage + pdfUri.LocalPath);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message + e.StackTrace);
                    CardGameManager.Instance.Messenger.Show(DeckPrintOpenPathErrorMessage + pdfUri.LocalPath);
                }
            }, false);
#elif (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            var nativeShare = new NativeShare();
            nativeShare.AddFile(pdfUri.AbsoluteUri, "application/pdf").Share();
#else
            Application.OpenURL(pdfUri.AbsoluteUri);
#endif
        }

        [UsedImplicitly]
        public void EnableSubmit()
        {
            if (!EventSystem.current.alreadySelecting)
                EventSystem.current.SetSelectedGameObject(null);
        }

        [UsedImplicitly]
        public void AttemptSaveAndHide()
        {
            var filePathFinder = new UnityDeck(CardGameManager.Current, nameInputField.text,
                CardGameManager.Current.DeckFileType);
            if (!_doesAutoOverwrite && File.Exists(filePathFinder.FilePath))
                CardGameManager.Instance.Messenger.Prompt(OverWriteDeckPrompt, SaveToFile);
            else
                SaveToFile();

            Hide();
        }

        [UsedImplicitly]
        public void SaveToFile()
        {
            _currentDeck.Name = nameInputField.text;
            SaveToFile(_currentDeck, _deckSaveCallback);
        }

        private static void SaveToFile(UnityDeck deck, OnDeckSavedDelegate deckSaveCallback = null)
        {
            try
            {
                if (!Directory.Exists(CardGameManager.Current.DecksDirectoryPath))
                    Directory.CreateDirectory(CardGameManager.Current.DecksDirectoryPath);
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
            _nameChangeCallback?.Invoke(_currentDeck.Name);
            Hide();
        }
    }
}
