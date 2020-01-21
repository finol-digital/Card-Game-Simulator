/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CGS.Menu
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

            if ((Input.GetKeyDown(Inputs.BluetoothReturn) || Input.GetButtonDown(Inputs.Submit) || Input.GetButtonDown(Inputs.New))
                 && downloadButton.interactable)
                StartDownload();
            else if ((Input.GetButtonDown(Inputs.Sort) || Input.GetButtonDown(Inputs.Load)) && urlInputField.interactable)
                Clear();
            else if ((Input.GetButtonDown(Inputs.Filter) || Input.GetButtonDown(Inputs.Save)) && urlInputField.interactable)
                Paste();
            else if (((Input.GetButtonDown(Inputs.FocusBack) || Input.GetAxis(Inputs.FocusBack) != 0)
                || (Input.GetButtonDown(Inputs.FocusNext) || Input.GetAxis(Inputs.FocusNext) != 0)) && urlInputField.interactable)
                urlInputField.ActivateInputField();
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(Inputs.Cancel) || Input.GetButtonDown(Inputs.Option))
                Hide();
        }

        public void Show(string label, string prompt, DownloadCoroutineDelegate downloadCoroutine)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            labelText.text = label;
            (urlInputField.placeholder as Text).text = prompt;
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
            downloadButton.interactable = System.Uri.IsWellFormedUriString(url.Trim(), System.UriKind.Absolute);
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

            urlInputField.interactable = true;
            Hide();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
