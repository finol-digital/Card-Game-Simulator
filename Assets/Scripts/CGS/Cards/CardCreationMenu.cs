/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using SFB;

using CardGameDef;
using CGS.Menu;

namespace CGS.Cards
{
    public class CardCreationMenu : Modal
    {
        public const string DownloadCardImage = "Download Card Image";
        public const string DownloadCardImagePrompt = "Enter card image url...";
        public const string ImportImage = "Import Image";
        public const string ImportImageWarningMessage = "No image file selected for import!";
        public const string ImageCreationFailedWarningMessage = "Failed to get the image! Unable to create the card.";

        public GameObject downloadMenuPrefab;
        public List<InputField> inputFields;
        public Image cardImage;
        public Button createButton;

        public string CardName { get; set; }
        public Uri CardImageUri { get { return _cardImageUri; } private set { _cardImageUri = value; ValidateCreateButton(); } }
        private Uri _cardImageUri;

        public DownloadMenu Downloader => _downloader ??
                                              (_downloader = Instantiate(downloadMenuPrefab).GetOrAddComponent<DownloadMenu>());
        private DownloadMenu _downloader;

        private UnityAction _onCreationCallback;

        void Update()
        {
            if (!IsFocused || inputFields.Any(inputField => inputField.isFocused))
                return;

            if ((Input.GetKeyDown(Inputs.BluetoothReturn) || Input.GetButtonDown(Inputs.Submit) || Input.GetButtonDown(Inputs.New))
                 && createButton.interactable)
                StartCreation();
            if (Input.GetButtonDown(Inputs.Load) && createButton.interactable)
                DownloadCardImageFromWeb();
            if (Input.GetButtonDown(Inputs.Save) && createButton.interactable)
                ImportCardImageFromFile();
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(Inputs.Cancel) || Input.GetButtonDown(Inputs.Option))
                Hide();
        }

        public void Show(UnityAction onCreationCallback)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            _onCreationCallback = onCreationCallback;
        }

        public void DownloadCardImageFromWeb()
        {
            Downloader.Show(DownloadCardImage, DownloadCardImagePrompt, DownloadCardImageFromWeb);
        }
        public IEnumerator DownloadCardImageFromWeb(string url)
        {
            CardImageUri = new Uri(url);
            yield return UpdateCardImage();
        }
        public void ImportCardImageFromFile()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            NativeGallery.GetImageFromGallery(ImportCardBackImageFromFile, ImportImage);
#else
            string[] paths = StandaloneFileBrowser.OpenFilePanel(ImportImage, string.Empty, UnityExtensionMethods.ImageExtensions, false);
            if (paths.Length > 0)
                ImportCardImageFromFile(paths[0]);
            else
                Debug.LogWarning(ImportImageWarningMessage);
#endif
        }
        public void ImportCardImageFromFile(string uri)
        {
            if (string.IsNullOrEmpty(uri))
            {
                Debug.LogWarning(ImportImageWarningMessage);
                return;
            }
            CardImageUri = new Uri(uri);
            StartCoroutine(UpdateCardImage());
        }
        private IEnumerator UpdateCardImage()
        {
            // NOTE: Memory Leak Potential
            UnityEngine.Sprite newSprite = null;
            yield return UnityExtensionMethods.RunOutputCoroutine<UnityEngine.Sprite>(
                UnityExtensionMethods.CreateAndOutputSpriteFromImageFile(CardImageUri?.AbsoluteUri)
                , output => newSprite = output);
            if (newSprite != null)
                cardImage.sprite = newSprite;
            else
                Debug.LogWarning(ImageCreationFailedWarningMessage);
        }

        public void ValidateCreateButton()
        {
            createButton.interactable = !string.IsNullOrEmpty(CardName) && CardImageUri != null && CardImageUri.IsAbsoluteUri;
        }

        public void StartCreation()
        {
            StartCoroutine(CreateCard());
        }

        public IEnumerator CreateCard()
        {
            ValidateCreateButton();
            if (!createButton.interactable)
                yield break;

            createButton.interactable = false;

            Card newCard = new Card(CardGameManager.Current, Guid.NewGuid().ToString().ToUpper(), CardName, Set.DefaultCode, null, false);
            newCard.ImageWebUrl = CardImageUri.AbsoluteUri;
            yield return UnityExtensionMethods.SaveUrlToFile(CardImageUri.AbsoluteUri, newCard.ImageFilePath);

            if (!File.Exists(newCard.ImageFilePath))
            {
                Debug.LogWarning(ImageCreationFailedWarningMessage);
                yield break;
            }

            CardGameManager.Current.Add(newCard);
            if (_onCreationCallback != null)
                _onCreationCallback();

            ValidateCreateButton();
            Hide();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
