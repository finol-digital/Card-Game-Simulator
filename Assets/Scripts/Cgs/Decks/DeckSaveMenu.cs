/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.IO;
using Cgs.Menu;
using FinolDigital.Cgs.Json;
using FinolDigital.Cgs.Json.Unity;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
#if UNITY_ANDROID && !UNITY_EDITOR
using System.Collections;
using UnityEngine.Networking;
#endif

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

        public override bool IsBlocked => base.IsBlocked || nameInputField.isFocused;

        private void OnEnable()
        {
            InputSystem.actions.FindAction(Tags.SubMenuFocusNext).performed += InputFocus;
            InputSystem.actions.FindAction(Tags.SubMenuPaste).performed += InputPaste;
            InputSystem.actions.FindAction(Tags.SubMenuCopyShare).performed += InputCopyShare;
            InputSystem.actions.FindAction(Tags.PlayerSubmit).performed += InputSubmit;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed += InputCancel;
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

        private void InputFocus(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            nameInputField.ActivateInputField();
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
                deck.Add((UnityCard)card);
            textOutputArea.text = deck.ToString();
        }

        private void InputPaste(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            if (EventSystem.current.currentSelectedGameObject == null)
                PrintPdf();
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
#elif UNITY_ANDROID && !UNITY_EDITOR
            StartCoroutine(OpenPdf(pdfUri));
#elif UNITY_IOS && !UNITY_EDITOR
            new NativeShare().AddFile(pdfUri.AbsoluteUri).Share();
#else
            Application.OpenURL(pdfUri.AbsoluteUri);
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        public IEnumerator OpenPdf(Uri uri)
        {
            var uwr = new UnityWebRequest( uri, UnityWebRequest.kHttpVerbGET );
            var path = Path.Combine( Application.temporaryCachePath, "temp.pdf" );
            uwr.downloadHandler = new DownloadHandlerFile( path );
            yield return uwr.SendWebRequest();
            new NativeShare().AddFile(path, "application/pdf").Share();
        }
#endif

        private void InputCopyShare(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            if (EventSystem.current.currentSelectedGameObject == null)
                Share();
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
        public void EnableSubmit()
        {
            if (!EventSystem.current.alreadySelecting)
                EventSystem.current.SetSelectedGameObject(null);
        }

        private void InputSubmit(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            if (EventSystem.current.currentSelectedGameObject == null)
                AttemptSaveAndHide();
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

        private void InputCancel(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            CancelAndHide();
        }

        [UsedImplicitly]
        public void CancelAndHide()
        {
            _nameChangeCallback?.Invoke(_currentDeck.Name);
            Hide();
        }

        private void OnDisable()
        {
            InputSystem.actions.FindAction(Tags.SubMenuFocusNext).performed -= InputFocus;
            InputSystem.actions.FindAction(Tags.SubMenuPaste).performed -= InputPaste;
            InputSystem.actions.FindAction(Tags.SubMenuCopyShare).performed -= InputCopyShare;
            InputSystem.actions.FindAction(Tags.PlayerSubmit).performed -= InputSubmit;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed -= InputCancel;
        }
    }
}
