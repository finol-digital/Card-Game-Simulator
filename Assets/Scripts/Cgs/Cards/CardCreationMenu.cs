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
using UnityExtensionMethods;

namespace Cgs.Cards
{
    public class CardCreationMenu : Modal
    {
        public const string DownloadCardImage = "Download Card Image";
        public const string DownloadCardImagePrompt = "Enter card image url...";
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        public const string ImportImage = "Import Image";
#endif
        public const string ImportImageWarningMessage = "No image file selected for import!";
        public const string ImageCreationFailedWarningMessage = "Failed to get the image! Unable to create the card.";

        public GameObject downloadMenuPrefab;
        public List<InputField> inputFields;
        public Image cardImage;
        public Button createButton;

        [UsedImplicitly] public string CardName { get; set; }

        private Uri CardImageUri
        {
            get => _cardImageUri;
            set
            {
                _cardImageUri = value;
                ValidateCreateButton();
            }
        }

        private Uri _cardImageUri;

        private DownloadMenu Downloader => _downloader
            ? _downloader
            : (_downloader = Instantiate(downloadMenuPrefab)
                .GetOrAddComponent<DownloadMenu>());

        private DownloadMenu _downloader;

        private UnityAction _onCreationCallback;

        private void Update()
        {
            if (!IsFocused || inputFields.Any(inputField => inputField.isFocused))
                return;

            if ((Inputs.IsSubmit || Inputs.IsNew) && createButton.interactable)
                StartCreation();
            if (Inputs.IsLoad && createButton.interactable)
                DownloadCardImageFromWeb();
            if (Inputs.IsSave && createButton.interactable)
                ImportCardImageFromFile();
            else if (Inputs.IsCancel || Inputs.IsOption)
                Hide();
        }

        public void Show(UnityAction onCreationCallback)
        {
            Show();
            _onCreationCallback = onCreationCallback;
        }

        [UsedImplicitly]
        public void DownloadCardImageFromWeb()
        {
            Downloader.Show(DownloadCardImage, DownloadCardImagePrompt, DownloadCardImageFromWeb);
        }

        private IEnumerator DownloadCardImageFromWeb(string url)
        {
            CardImageUri = new Uri(url);
            yield return UpdateCardImage();
        }

        [UsedImplicitly]
        public void ImportCardImageFromFile()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            NativeGallery.GetImageFromGallery(ImportCardImageFromFile, ImportImage);
#else
            ImportCardImageFromFile(FileBrowser.OpenSingleFile());
#endif
        }

#if ENABLE_WINMD_SUPPORT
        private async void ImportCardImageFromFile(string uri)
#else
        private void ImportCardImageFromFile(string uri)
#endif
        {
            if (string.IsNullOrEmpty(uri))
            {
                Debug.LogWarning(ImportImageWarningMessage);
                return;
            }
#if ENABLE_WINMD_SUPPORT
            CardImageUri = new Uri(await UnityFileMethods.CacheFileAsync(uri));
#elif UNITY_STANDALONE
            CardImageUri = new Uri(UnityFileMethods.CacheFile(uri));
#else
            CardImageUri = new Uri(uri);
#endif
            StartCoroutine(UpdateCardImage());
        }

        private IEnumerator UpdateCardImage()
        {
            // NOTE: Memory Leak Potential
            Sprite newSprite = null;
            yield return UnityFileMethods.RunOutputCoroutine<Sprite>(
                UnityFileMethods.CreateAndOutputSpriteFromImageFile(CardImageUri?.AbsoluteUri)
                , output => newSprite = output);
            if (newSprite != null)
                cardImage.sprite = newSprite;
            else
                Debug.LogWarning(ImageCreationFailedWarningMessage);
        }

        private void ValidateCreateButton()
        {
            createButton.interactable =
                !string.IsNullOrEmpty(CardName) && CardImageUri != null && CardImageUri.IsAbsoluteUri;
        }

        [UsedImplicitly]
        public void StartCreation()
        {
            StartCoroutine(CreateCard());
        }

        private IEnumerator CreateCard()
        {
            ValidateCreateButton();
            if (!createButton.interactable)
                yield break;

            createButton.interactable = false;

            var card = new UnityCard(CardGameManager.Current, Guid.NewGuid().ToString().ToUpper(), CardName,
                Set.DefaultCode, null, false) {ImageWebUrl = CardImageUri.AbsoluteUri};
            yield return UnityFileMethods.SaveUrlToFile(CardImageUri.AbsoluteUri, card.ImageFilePath);

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
    }
}
