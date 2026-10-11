/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using Cgs.Localization;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Cgs.Menu
{
    public delegate IEnumerator DownloadCoroutineDelegate(string url);

    public class DownloadMenu : Modal
    {
        public const string ClearPrompt = "Clear the URL input field?";

        [SerializeField] Text labelText;
        [SerializeField] InputField urlInputField;
        [SerializeField] Button downloadButton;
        [SerializeField] Transform gamesButton;

        private DownloadCoroutineDelegate _downloadCoroutine;

        public override bool IsBlocked => base.IsBlocked || urlInputField.isFocused;

        protected void OnEnable()
        {
            InputSystem.actions.FindAction(Tags.SubMenuMenu).performed += InputMenu;
            InputSystem.actions.FindAction(Tags.SubMenuFocusNext).performed += InputFocus;
            InputSystem.actions.FindAction(Tags.SubMenuPaste).performed += InputPaste;
            InputSystem.actions.FindAction(Tags.PlayerDelete).performed += InputDelete;
            InputSystem.actions.FindAction(Tags.PlayerSubmit).performed += InputSubmit;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed += InputCancel;
        }

        public void Show(string label, string prompt, DownloadCoroutineDelegate downloadCoroutine,
            bool showGamesButton = false)
        {
            Show();

            LocalizedUiText.SetLiteral(labelText, label);
            LocalizedUiText.SetLiteral((Text)urlInputField.placeholder, prompt);
            _downloadCoroutine = downloadCoroutine;

            if (!showGamesButton)
                gamesButton.gameObject.SetActive(false);
        }

        public void Show(UiMessage label, UiMessage prompt, DownloadCoroutineDelegate downloadCoroutine,
            bool showGamesButton = false)
        {
            Show(label.English, prompt.English, downloadCoroutine, showGamesButton);
            LocalizedUiText.Set(labelText, label);
            LocalizedUiText.Set((Text)urlInputField.placeholder, prompt);
        }

        private void InputMenu(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            if (gamesButton.gameObject.activeSelf)
                GoToCgsGamesBrowser();
        }

        [UsedImplicitly]
        public void GoToCgsGamesBrowser()
        {
            Application.OpenURL(Tags.CgsGamesBrowseUrl);
        }

        private void InputFocus(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            if (urlInputField.interactable)
                urlInputField.ActivateInputField();
        }

        private void InputPaste(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            if (urlInputField.interactable)
                Paste();
        }

        [UsedImplicitly]
        public void Paste()
        {
            if (urlInputField.interactable)
                urlInputField.text = UniClipboard.GetText();
        }

        [UsedImplicitly]
        public void CheckDownloadUrl(string url)
        {
            downloadButton.interactable = Uri.IsWellFormedUriString(url.Trim(), UriKind.Absolute);
        }

        private void InputDelete(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            if (urlInputField.interactable)
                PromptForClear();
        }

        [UsedImplicitly]
        public void PromptForClear()
        {
            CardGameManager.Instance.Messenger.Prompt(new UiMessage("downloads.clear", ClearPrompt), Clear);
        }

        private void Clear()
        {
            urlInputField.text = string.Empty;
        }

        private void InputSubmit(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            if (downloadButton.interactable)
                StartDownload();
        }

        [UsedImplicitly]
        public void StartDownload()
        {
            CardGameManager.Instance.StartCoroutine(Download());
        }

        private IEnumerator Download()
        {
            var url = urlInputField.text.Trim();

            urlInputField.text = string.Empty;
            urlInputField.interactable = false;

            Debug.Log("DownloadMenu: Download start");
            yield return _downloadCoroutine(url);
            Debug.Log("DownloadMenu: Download end");

            urlInputField.interactable = true;
            Hide();
        }

        private void InputCancel(InputAction.CallbackContext callbackContext)
        {
            if (IsBlocked)
                return;

            Hide();
        }

        protected void OnDisable()
        {
            InputSystem.actions.FindAction(Tags.SubMenuMenu).performed -= InputMenu;
            InputSystem.actions.FindAction(Tags.SubMenuFocusNext).performed -= InputFocus;
            InputSystem.actions.FindAction(Tags.SubMenuPaste).performed -= InputPaste;
            InputSystem.actions.FindAction(Tags.PlayerDelete).performed -= InputDelete;
            InputSystem.actions.FindAction(Tags.PlayerSubmit).performed -= InputSubmit;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed -= InputCancel;
        }
    }
}
