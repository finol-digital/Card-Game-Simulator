/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CardGameDef;
using CardGameDef.Unity;
using Cgs.Menu;
using Crosstales.FB;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Cgs.Cards
{
    public class CardCreationMenu : Modal
    {
        public const string DownloadCardImage = "Download Card Image";
        public const string DownloadCardImagePrompt = "Enter card image url...";
        public const string ImportImageWarningMessage = "No image file selected for import!";
        public const string ImageCreationFailedWarningMessage = "Failed to get the image! Unable to create the card.";

        public GameObject downloadMenuPrefab;
        public List<InputField> inputFields;
        public Image cardImage;
        public Button createButton;

        public string CardName { get; [UsedImplicitly] set; }

        public Uri CardImageUri
        {
            get => _cardImageUri;
            private set
            {
                _cardImageUri = value;
                ValidateCreateButton();
            }
        }

        private Uri _cardImageUri;

        public DownloadMenu Downloader => _downloader ??
                                          (_downloader = Instantiate(downloadMenuPrefab)
                                              .GetOrAddComponent<DownloadMenu>());

        private DownloadMenu _downloader;

        private UnityAction _onCreationCallback;

        void Update()
        {
            if (!IsFocused || inputFields.Any(inputField => inputField.isFocused))
                return;

            if ((Input.GetKeyDown(Inputs.BluetoothReturn) || Input.GetButtonDown(Inputs.Submit) ||
                 Input.GetButtonDown(Inputs.New))
                && createButton.interactable)
                StartCreation();
            if (Input.GetButtonDown(Inputs.Load) && createButton.interactable)
                DownloadCardImageFromWeb();
            if (Input.GetButtonDown(Inputs.Save) && createButton.interactable)
                ImportCardImageFromFile();
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(Inputs.Cancel) ||
                     Input.GetButtonDown(Inputs.Option))
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
            NativeGallery.GetImageFromGallery(ImportCardImageFromFile, ImportImage);
#else
            ImportCardImageFromFile(FileBrowser.OpenSingleFile());
#endif
        }
#if ENABLE_WINMD_SUPPORT
        public async void ImportCardImageFromFile(string uri)
#else
        public void ImportCardImageFromFile(string uri)
#endif
        {
            if (string.IsNullOrEmpty(uri))
            {
                Debug.LogWarning(ImportImageWarningMessage);
                return;
            }
#if ENABLE_WINMD_SUPPORT
            CardImageUri = new Uri(await UnityExtensionMethods.CacheFileAsync(uri));
#elif UNITY_STANDALONE
            CardImageUri = new Uri(UnityExtensionMethods.CacheFile(uri));
#else
            CardImageUri = new Uri(uri);
#endif
            StartCoroutine(UpdateCardImage());
        }

        private IEnumerator UpdateCardImage()
        {
            // NOTE: Memory Leak Potential
            Sprite newSprite = null;
            yield return UnityExtensionMethods.RunOutputCoroutine<Sprite>(
                UnityExtensionMethods.CreateAndOutputSpriteFromImageFile(CardImageUri?.AbsoluteUri)
                , output => newSprite = output);
            if (newSprite != null)
                cardImage.sprite = newSprite;
            else
                Debug.LogWarning(ImageCreationFailedWarningMessage);
        }

        public void ValidateCreateButton()
        {
            createButton.interactable =
                !string.IsNullOrEmpty(CardName) && CardImageUri != null && CardImageUri.IsAbsoluteUri;
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

            var card = new UnityCard(CardGameManager.Current, Guid.NewGuid().ToString().ToUpper(), CardName,
                Set.DefaultCode, null, false) {ImageWebUrl = CardImageUri.AbsoluteUri};
            yield return UnityExtensionMethods.SaveUrlToFile(CardImageUri.AbsoluteUri, card.ImageFilePath);

            if (!File.Exists(card.ImageFilePath))
            {
                Debug.LogWarning(ImageCreationFailedWarningMessage);
                yield break;
            }

            CardGameManager.Current.Add(card);
            _onCreationCallback?.Invoke();

            ValidateCreateButton();
            Hide();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
