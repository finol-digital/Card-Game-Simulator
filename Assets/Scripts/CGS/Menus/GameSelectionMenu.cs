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

namespace CGS.Menus
{
    public class GameSelectionMenu : SelectionPanel
    {
        public const string DeleteMessage = "Please download additional card games before deleting.";
        public const string DeletePrompt = "Deleting a card game also deletes all decks saved for that card game. Are you sure you would like to delete this card game?";
        public const string ShareTitle = "Card Game Simulator - {0}";
        public const string ShareDescription = "Play {0} on CGS!";
        public const string ShareBranchMessage = "Get CGS for {0}: {1}";
        public const string ShareUrlMessage = "The CGS AutoUpdate Url for {0} has been copied to the clipboard: {1}";
        public const string DominoesUrl = "https://cardgamesim.finoldigital.com/games/Dominoes/Dominoes.json";
        public const string StandardUrl = "https://cardgamesim.finoldigital.com/games/Standard/Standard.json";
        public const string MahjongUrl = "https://cardgamesim.finoldigital.com/games/Mahjong/Mahjong.json";

        public RectTransform downloadPanel;
        public InputField urlInput;
        public Button downloadButton;

        private bool _wasDown;
        private bool _wasUp;
        private bool _wasLeft;
        private bool _wasRight;
        private bool _wasPage;

        void Update()
        {
            if (urlInput.isFocused || gameObject != CardGameManager.Instance.TopMenuCanvas?.gameObject)
                return;

            if (downloadPanel.gameObject.activeSelf)
            {
                if ((Input.GetKeyDown(Inputs.BluetoothReturn) || Input.GetButtonDown(Inputs.Submit)) && downloadButton.interactable)
                    StartDownload();
                else if ((Input.GetButtonDown(Inputs.New) || Input.GetButtonDown(Inputs.Load)) && urlInput.interactable)
                    Clear();
                else if (Input.GetButtonDown(Inputs.Save) && urlInput.interactable)
                    Paste();
                else if ((Input.GetButtonDown(Inputs.FocusName) || Input.GetAxis(Inputs.FocusName) != 0) && urlInput.interactable)
                    urlInput.ActivateInputField();
                else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(Inputs.Cancel))
                    HideDownloadPanel();
            }
            else
            {
                if (Input.GetButtonDown(Inputs.Vertical) || Input.GetAxis(Inputs.Vertical) != 0)
                {
                    if (Input.GetAxis(Inputs.Vertical) > 0 && !_wasUp)
                        SelectPrevious();
                    else if (Input.GetAxis(Inputs.Vertical) < 0 && !_wasDown)
                        SelectNext();
                }
                else if (Input.GetButtonDown(Inputs.Horizontal) || Input.GetAxis(Inputs.Horizontal) != 0)
                {
                    if (Input.GetAxis(Inputs.Horizontal) < 0 && !_wasLeft)
                        CardGameManager.Instance.Select(CardGameManager.Instance.Previous.Id);
                    else if (Input.GetAxis(Inputs.Horizontal) > 0 && !_wasRight)
                        CardGameManager.Instance.Select(CardGameManager.Instance.Next.Id);
                    Rebuild(CardGameManager.Instance.GamesListing, SelectGame, CardGameManager.Current.Id);
                }

                if (Input.GetKeyDown(Inputs.BluetoothReturn) && Toggles.Contains(EventSystem.current.currentSelectedGameObject))
                    EventSystem.current.currentSelectedGameObject.GetComponent<Toggle>().isOn = true;
                else if (Input.GetKeyDown(Inputs.BluetoothReturn) || Input.GetButtonDown(Inputs.Submit))
                    Hide();
                else if (Input.GetButtonDown(Inputs.Sort))
                    Share();
                else if (Input.GetButtonDown(Inputs.New) || Input.GetButtonDown(Inputs.Load))
                    ShowDownloadPanel();
                else if (Input.GetButtonDown(Inputs.Delete))
                    Delete();
                else if ((Input.GetButtonDown(Inputs.PageVertical) || Input.GetAxis(Inputs.PageVertical) != 0) && !_wasPage)
                    ScrollPage(Input.GetAxis(Inputs.PageVertical));
                else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(Inputs.Cancel))
                    Hide();
            }

            _wasDown = Input.GetAxis(Inputs.Vertical) < 0;
            _wasUp = Input.GetAxis(Inputs.Vertical) > 0;
            _wasLeft = Input.GetAxis(Inputs.Horizontal) < 0;
            _wasRight = Input.GetAxis(Inputs.Horizontal) > 0;
            _wasPage = Input.GetAxis(Inputs.PageVertical) != 0;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Rebuild(CardGameManager.Instance.GamesListing, SelectGame, CardGameManager.Current.Id);
        }

        public void SelectGame(Toggle toggle, string gameId)
        {
            if (toggle.isOn)
                CardGameManager.Instance.Select(gameId);
            else if (!toggle.group.AnyTogglesOn())
                Hide();
        }

        public void Delete()
        {
            if (CardGameManager.Instance.AllCardGames.Count > 1)
                CardGameManager.Instance.Messenger.Prompt(DeletePrompt, CardGameManager.Instance.DeleteCurrent);
            else
                CardGameManager.Instance.Messenger.Show(DeleteMessage);
        }

        public void Share()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            ShareBranch();
#else
            ShareUrl();
#endif
        }

        public void ShareBranch()
        {
            BranchUniversalObject universalObject = new BranchUniversalObject();
            universalObject.contentIndexMode = 1;
            universalObject.canonicalIdentifier = CardGameManager.Current.Id;
            universalObject.title = string.Format(ShareTitle, CardGameManager.Current.Name);
            universalObject.contentDescription = string.Format(ShareDescription, CardGameManager.Current.Name);
            universalObject.imageUrl = CardGameManager.Current.BannerImageUrl;
            universalObject.metadata.AddCustomMetadata(CardGameManager.GameId, CardGameManager.Current.Id);
            BranchLinkProperties linkProperties = new BranchLinkProperties();
            linkProperties.controlParams.Add(CardGameManager.GameId, CardGameManager.Current.Id);
            Branch.getShortURL(universalObject, linkProperties, BranchCallbackWithUrl);
        }

        public void BranchCallbackWithUrl(string url, string error)
        {
            if (error != null)
            {
                Debug.LogError(error);
                return;
            }

            NativeShare nativeShare = new NativeShare();
            nativeShare.SetText(string.Format(ShareBranchMessage, CardGameManager.Current.Name, url)).Share();
        }

        public void ShareUrl()
        {
            UniClipboard.SetText(CardGameManager.Current.AutoUpdateUrl);
            CardGameManager.Instance.Messenger.Show(string.Format(ShareUrlMessage, CardGameManager.Current.Name, CardGameManager.Current.AutoUpdateUrl));
        }

        public void ShowNewPanel()
        {
            CardGameManager.Instance.Messenger.Show("Custom Card Game Creator Coming Soon!");
        }

        public void ShowDownloadPanel()
        {
            downloadPanel.gameObject.SetActive(true);
        }

        public void ApplyDominoes()
        {
            if (urlInput.interactable)
                urlInput.text = DominoesUrl;
        }

        public void ApplyStandard()
        {
            if (urlInput.interactable)
                urlInput.text = StandardUrl;
        }

        public void ApplyMahjong()
        {
            if (urlInput.interactable)
                urlInput.text = MahjongUrl;
        }

        public void Paste()
        {
            if (urlInput.interactable)
                urlInput.text = UniClipboard.GetText();
        }

        public void Clear()
        {
            urlInput.text = string.Empty;
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
            cancelButton.interactable = false;

            yield return CardGameManager.Instance.DownloadCardGame(gameUrl);

            cancelButton.interactable = true;
            urlInput.interactable = true;
            HideDownloadPanel();
        }

        public void HideDownloadPanel()
        {
            Show();
            downloadPanel.gameObject.SetActive(false);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
