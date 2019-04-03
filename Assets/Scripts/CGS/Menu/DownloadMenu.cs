/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using CardGameDef;

namespace CGS.Menu
{
    public class DownloadMenu : MonoBehaviour
    {
        public InputField urlInput;
        public Button downloadButton;

        void Update()
        {
            if (urlInput.isFocused || gameObject != CardGameManager.Instance.TopMenuCanvas?.gameObject)
                return;

            if ((Input.GetKeyDown(Inputs.BluetoothReturn) || Input.GetButtonDown(Inputs.Submit) || Input.GetButtonDown(Inputs.New))
                 && downloadButton.interactable)
                StartDownload();
            else if ((Input.GetButtonDown(Inputs.Sort) || Input.GetButtonDown(Inputs.Load)) && urlInput.interactable)
                Clear();
            else if ((Input.GetButtonDown(Inputs.Filter) ||Input.GetButtonDown(Inputs.Save)) && urlInput.interactable)
                Paste();
            else if (((Input.GetButtonDown(Inputs.FocusName) || Input.GetAxis(Inputs.FocusName) != 0)
                || (Input.GetButtonDown(Inputs.FocusText) || Input.GetAxis(Inputs.FocusText) != 0)) && urlInput.interactable)
                urlInput.ActivateInputField();
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(Inputs.Cancel) || Input.GetButtonDown(Inputs.Option))
                Hide();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Clear()
        {
            urlInput.text = string.Empty;
        }

        public void Paste()
        {
            if (urlInput.interactable)
                urlInput.text = UniClipboard.GetText();
        }

        public void CheckDownloadUrl(string url)
        {
            downloadButton.interactable = System.Uri.IsWellFormedUriString(url.Trim(), System.UriKind.Absolute);
        }

        public void StartDownload()
        {
            CardGameManager.Instance.StartCoroutine(DownloadGame());
        }

        public IEnumerator DownloadGame()
        {
            string gameUrl = urlInput.text.Trim();

            urlInput.text = string.Empty;
            urlInput.interactable = false;

            // If user attempts to download a game they already have, we should just update that game
            CardGame existingGame = null;
            foreach (CardGame cardGame in CardGameManager.Instance.AllCardGames.Values)
                if (gameUrl.Equals(cardGame.AutoUpdateUrl))
                    existingGame = cardGame;
            if (existingGame != null)
            {
                yield return CardGameManager.Instance.UpdateCardGame(existingGame);
                if (string.IsNullOrEmpty(existingGame.Error))
                    CardGameManager.Instance.Select(existingGame.Id);
            }
            else
                yield return CardGameManager.Instance.DownloadCardGame(gameUrl);

            urlInput.interactable = true;
            Hide();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
