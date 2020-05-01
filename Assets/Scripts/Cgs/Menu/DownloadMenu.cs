/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Cgs.Menu
{
    public delegate IEnumerator DownloadCoroutineDelegate(string url);

    public class DownloadMenu : Modal
    {
        public Text labelText;
        public InputField urlInputField;
        public Button downloadButton;

        public DownloadCoroutineDelegate DownloadCoroutine { get; private set; }

        void Update()
        {
            if (!IsFocused || urlInputField.isFocused)
                return;

            if ((Input.GetKeyDown(Inputs.BluetoothReturn) || Input.GetButtonDown(Inputs.Submit) ||
                 Input.GetButtonDown(Inputs.New))
                && downloadButton.interactable)
                StartDownload();
            else if ((Input.GetButtonDown(Inputs.Sort) || Input.GetButtonDown(Inputs.Load)) &&
                     urlInputField.interactable)
                Clear();
            else if ((Input.GetButtonDown(Inputs.Filter) || Input.GetButtonDown(Inputs.Save)) &&
                     urlInputField.interactable)
                Paste();
            else if (((Input.GetButtonDown(Inputs.FocusBack) ||
                       Math.Abs(Input.GetAxis(Inputs.FocusBack)) > Inputs.Tolerance)
                      || (Input.GetButtonDown(Inputs.FocusNext) ||
                          Math.Abs(Input.GetAxis(Inputs.FocusNext)) > Inputs.Tolerance)) &&
                     urlInputField.interactable)
                urlInputField.ActivateInputField();
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(Inputs.Cancel) ||
                     Input.GetButtonDown(Inputs.Option))
                Hide();
        }

        public void Show(string label, string prompt, DownloadCoroutineDelegate downloadCoroutine)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            labelText.text = label;
            ((Text) urlInputField.placeholder).text = prompt;
            DownloadCoroutine = downloadCoroutine;
        }

        public void Clear()
        {
            urlInputField.text = string.Empty;
        }

        public void Paste()
        {
            if (urlInputField.interactable)
                urlInputField.text = UniClipboard.GetText();
        }

        public void CheckDownloadUrl(string url)
        {
            downloadButton.interactable = Uri.IsWellFormedUriString(url.Trim(), UriKind.Absolute);
        }

        public void StartDownload()
        {
            CardGameManager.Instance.StartCoroutine(Download());
        }

        public IEnumerator Download()
        {
            string url = urlInputField.text.Trim();

            urlInputField.text = string.Empty;
            urlInputField.interactable = false;

            yield return DownloadCoroutine(url);

            // ReSharper disable once Unity.InefficientPropertyAccess
            urlInputField.interactable = true;
            Hide();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
