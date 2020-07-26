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
using Crosstales.FB;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;
using UnityEngine.UI;

namespace Cgs.Menu
{
    public class CreateMenu : Modal
    {
        public const string DownloadBannerImage = "Download Banner Image";
        public const string DownloadBannerImagePrompt = "Enter banner image url...";
        public const string DownloadCardBackImage = "Download Card Back Image";
        public const string DownloadCardBackImagePrompt = "Enter card back image url...";
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        public const string ImportImage = "Import Image";
#endif
        public const string ImportImageWarningMessage = "No image file selected for import!";
        public const string CreateWarningMessage = "A game with that name already exists!";
        public const string CreationWarningMessage = "Failed to create the custom card game! ";
        public const string CreationCleanupErrorMessage = "Failed to both create and cleanup during creation! ";

        public GameObject downloadMenuPrefab;
        public List<InputField> inputFields;
        public Image bannerImage;
        public Image cardBackImage;
        public Button createButton;

        private DownloadMenu Downloader => _downloader
            ? _downloader
            : _downloader = Instantiate(downloadMenuPrefab).GetOrAddComponent<DownloadMenu>();

        private DownloadMenu _downloader;

        [UsedImplicitly]
        public string GameName
        {
            get => _gameName;
            set
            {
                _gameName = value != null ? value.Replace("@", "") : string.Empty;
                if (!_gameName.Equals(inputFields[0].text))
                    inputFields[0].text = _gameName;
            }
        }

        private string _gameName = string.Empty;

        private readonly CardGame _game = new CardGame(null);

        private void Update()
        {
            if (!IsFocused || inputFields.Any(inputField => inputField.isFocused))
                return;

            if ((Inputs.IsSubmit || Inputs.IsNew) && createButton.interactable)
                StartCreation();
            if (Inputs.IsSort)
                DownloadBannerImageFromWeb();
            if (Inputs.IsFilter)
                ImportBannerImageFromFile();
            if (Inputs.IsLoad)
                DownloadCardBackImageFromWeb();
            if (Inputs.IsSave)
                ImportCardBackImageFromFile();
            else if (Inputs.IsCancel || Inputs.IsOption)
                Hide();
        }

        [UsedImplicitly]
        public void DownloadBannerImageFromWeb()
        {
            Downloader.Show(DownloadBannerImage, DownloadBannerImagePrompt, DownloadBannerImageFromWeb);
        }

        private IEnumerator DownloadBannerImageFromWeb(string url)
        {
            _game.BannerImageUrl = new Uri(url);
            yield return UpdateBannerImage();
        }

        [UsedImplicitly]
        public void ImportBannerImageFromFile()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            NativeGallery.GetImageFromGallery(ImportBannerImageFromFile, ImportImage);
#else
            ImportBannerImageFromFile(FileBrowser.OpenSingleFile());
#endif
        }
#if ENABLE_WINMD_SUPPORT
        private async void ImportBannerImageFromFile(string uri)
#else
        private void ImportBannerImageFromFile(string uri)
#endif
        {
            if (string.IsNullOrEmpty(uri))
            {
                Debug.LogWarning(ImportImageWarningMessage);
                return;
            }
#if ENABLE_WINMD_SUPPORT
            _game.BannerImageUrl = new Uri(await UnityExtensionMethods.CacheFileAsync(uri));
#elif UNITY_STANDALONE
            _game.BannerImageUrl = new Uri(UnityExtensionMethods.CacheFile(uri));
#else
            _game.BannerImageUrl = new Uri(uri);
#endif
            StartCoroutine(UpdateBannerImage());
        }

        private IEnumerator UpdateBannerImage()
        {
            // NOTE: Memory Leak Potential
            Sprite newSprite = null;
            yield return UnityExtensionMethods.RunOutputCoroutine<Sprite>(
                UnityExtensionMethods.CreateAndOutputSpriteFromImageFile(_game.BannerImageUrl?.AbsoluteUri)
                , output => newSprite = output);
            if (newSprite != null)
                bannerImage.sprite = newSprite;
            else
                Debug.LogWarning(ImportImageWarningMessage);
        }

        [UsedImplicitly]
        public void DownloadCardBackImageFromWeb()
        {
            Downloader.Show(DownloadCardBackImage, DownloadCardBackImagePrompt, DownloadCardBackImageFromWeb);
        }

        private IEnumerator DownloadCardBackImageFromWeb(string url)
        {
            _game.CardBackImageUrl = new Uri(url);
            yield return UpdateCardBackImage();
        }

        [UsedImplicitly]
        public void ImportCardBackImageFromFile()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            NativeGallery.GetImageFromGallery(ImportCardBackImageFromFile, ImportImage);
#else
            ImportCardBackImageFromFile(FileBrowser.OpenSingleFile());
#endif
        }
#if ENABLE_WINMD_SUPPORT
        private async void ImportCardBackImageFromFile(string uri)
#else
        private void ImportCardBackImageFromFile(string uri)
#endif
        {
            if (string.IsNullOrEmpty(uri))
            {
                Debug.LogWarning(ImportImageWarningMessage);
                return;
            }
#if ENABLE_WINMD_SUPPORT
            _game.CardBackImageUrl = new Uri(await UnityExtensionMethods.CacheFileAsync(uri));
#elif UNITY_STANDALONE
            _game.CardBackImageUrl = new Uri(UnityExtensionMethods.CacheFile(uri));
#else
            _game.CardBackImageUrl = new Uri(uri);
#endif
            StartCoroutine(UpdateCardBackImage());
        }

        private IEnumerator UpdateCardBackImage()
        {
            // NOTE: Memory Leak Potential
            Sprite newSprite = null;
            yield return UnityExtensionMethods.RunOutputCoroutine<Sprite>(
                UnityExtensionMethods.CreateAndOutputSpriteFromImageFile(_game.CardBackImageUrl?.AbsoluteUri)
                , output => newSprite = output);
            if (newSprite != null)
                cardBackImage.sprite = newSprite;
            else
                Debug.LogWarning(ImportImageWarningMessage);
        }

        [UsedImplicitly]
        public void ValidateCreateButton()
        {
            createButton.interactable = !string.IsNullOrEmpty(GameName);
        }

        [UsedImplicitly]
        public void StartCreation()
        {
            StartCoroutine(CreateGame());
        }

        private IEnumerator CreateGame()
        {
            ValidateCreateButton();
            if (!createButton.interactable)
                yield break;

            string gameName = GameName.Trim().Replace("@", "");
            if (CardGameManager.Instance.AllCardGames.ContainsKey(gameName))
            {
                CardGameManager.Instance.Messenger.Show(CreateWarningMessage);
                yield break;
            }

            var newCardGame = new UnityCardGame(CardGameManager.Instance, gameName)
            {
                AutoUpdate = -1, BannerImageUrl = _game.BannerImageUrl, CardBackImageUrl = _game.CardBackImageUrl
            };

            if (!Directory.Exists(newCardGame.GameDirectoryPath))
                Directory.CreateDirectory(newCardGame.GameDirectoryPath);
            var defaultContractResolver = new DefaultContractResolver()
                {NamingStrategy = new CamelCaseNamingStrategy()};
            var jsonSerializerSettings = new JsonSerializerSettings()
            {
                ContractResolver = defaultContractResolver,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            File.WriteAllText(newCardGame.GameFilePath,
                JsonConvert.SerializeObject(newCardGame, jsonSerializerSettings));

            yield return CardGameManager.Instance.UpdateCardGame(newCardGame);

            if (!string.IsNullOrEmpty(newCardGame.Error))
            {
                Debug.LogWarning(CreationWarningMessage + newCardGame.Error);
                try
                {
                    Directory.Delete(newCardGame.GameDirectoryPath, true);
                }
                catch (Exception ex)
                {
                    Debug.LogError(CreationCleanupErrorMessage + ex.Message);
                }
            }
            else
            {
                CardGameManager.Instance.AllCardGames[newCardGame.Id] = newCardGame;
                CardGameManager.Instance.Select(newCardGame.Id);
                Hide();
            }
        }
    }
}
