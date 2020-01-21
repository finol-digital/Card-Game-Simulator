/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using SFB;

using CardGameDef;

namespace CGS.Menu
{
    public class CreateMenu : Modal
    {
        public const string OpenImage = "Open Image";
        public const string OpenImageWarningMessage = "Image file not selected!";
        public const string CreateWarningMessage = "A game with that name already exists!";
        public const string CreationWarningMessage = "Failed to create the custom card game! ";
        public const string CreationCleanupErrorMessage = "Failed to both create and cleanup during creation! ";

        public static readonly ExtensionFilter[] ImageExtensions = new[] {
            new ExtensionFilter("Image Files", "png", "jpg", "jpeg" ),
            new ExtensionFilter("All Files", "*" )
        };

        public List<InputField> inputFields;
        public Image bannerImage;
        public Image cardBackImage;

        public Button createButton;

        public string GameName { get; set; }
        private CardGame _game = new CardGame(null);

        void Update()
        {
            if (!IsFocused || inputFields.Any(inputField => inputField.isFocused))
                return;

            if ((Input.GetKeyDown(Inputs.BluetoothReturn) || Input.GetButtonDown(Inputs.Submit) || Input.GetButtonDown(Inputs.New))
                 && createButton.interactable)
                StartCreation();
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(Inputs.Cancel) || Input.GetButtonDown(Inputs.Option))
                Hide();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void DownloadBannerImageFromWeb()
        {

        }
        public void OpenBannerImageFromFile()
        {
            string[] paths = StandaloneFileBrowser.OpenFilePanel(OpenImage, string.Empty, ImageExtensions, false);
            if (paths.Length < 1)
            {
                Debug.LogWarning(OpenImageWarningMessage);
                return;
            }

            _game.BannerImageUrl = new Uri(paths[0]);
            StartCoroutine(UpdateBannerImage());
        }
        private IEnumerator UpdateBannerImage()
        {
            // NOTE: Memory Leak Potential
            UnityEngine.Sprite newSprite = null;
            yield return UnityExtensionMethods.RunOutputCoroutine<UnityEngine.Sprite>(
                UnityExtensionMethods.CreateAndOutputSpriteFromImageFile(_game.BannerImageUrl?.AbsoluteUri)
                , output => newSprite = output);
            if (newSprite != null)
                bannerImage.sprite = newSprite;
        }

        public void DownloadCardBackImageFromWeb()
        {

        }
        public void OpenCardBackImageFromFile()
        {
            string[] paths = StandaloneFileBrowser.OpenFilePanel(OpenImage, string.Empty, ImageExtensions, false);
            if (paths.Length < 1)
            {
                Debug.LogWarning(OpenImageWarningMessage);
                return;
            }

            _game.CardBackImageUrl = new Uri(paths[0]);
            StartCoroutine(UpdateCardBackImage());
        }
        private IEnumerator UpdateCardBackImage()
        {
            // NOTE: Memory Leak Potential
            UnityEngine.Sprite newSprite = null;
            yield return UnityExtensionMethods.RunOutputCoroutine<UnityEngine.Sprite>(
                UnityExtensionMethods.CreateAndOutputSpriteFromImageFile(_game.CardBackImageUrl?.AbsoluteUri)
                , output => newSprite = output);
            if (newSprite != null)
                cardBackImage.sprite = newSprite;
        }

        public void ValidateCreateButton()
        {
            createButton.interactable = !string.IsNullOrEmpty(GameName);
        }

        public void StartCreation()
        {
            StartCoroutine(CreateGame());
        }

        public IEnumerator CreateGame()
        {
            ValidateCreateButton();
            if (!createButton.interactable)
                yield break;

            string gameName = GameName.Trim();
            if (CardGameManager.Instance.AllCardGames.ContainsKey(gameName))
            {
                CardGameManager.Instance.Messenger.Show(CreateWarningMessage);
                yield break;
            }

            CardGame newCardGame = new CardGame(CardGameManager.Instance, gameName);
            newCardGame.AutoUpdate = -1;
            newCardGame.BannerImageUrl = _game.BannerImageUrl;
            newCardGame.CardBackImageUrl = _game.CardBackImageUrl;

            if (!Directory.Exists(newCardGame.GameDirectoryPath))
                Directory.CreateDirectory(newCardGame.GameDirectoryPath);
            var defaultContractResolver = new DefaultContractResolver() { NamingStrategy = new CamelCaseNamingStrategy() };
            var jsonSerializerSettings = new JsonSerializerSettings()
            {
                ContractResolver = defaultContractResolver,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            File.WriteAllText(newCardGame.GameFilePath, JsonConvert.SerializeObject(newCardGame, jsonSerializerSettings));

            yield return CardGameManager.Instance.UpdateCardGame(newCardGame);

            if (!string.IsNullOrEmpty(newCardGame.Error))
            {
                Debug.LogWarning(CreationWarningMessage + newCardGame.Error);
                try { Directory.Delete(newCardGame.GameDirectoryPath, true); }
                catch (Exception ex) { Debug.LogError(CreationCleanupErrorMessage + ex.Message); }
            }
            else
            {
                CardGameManager.Instance.AllCardGames[newCardGame.Id] = newCardGame;
                CardGameManager.Instance.Select(newCardGame.Id);
                Hide();
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
