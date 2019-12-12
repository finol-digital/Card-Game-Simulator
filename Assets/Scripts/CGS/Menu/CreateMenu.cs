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

using CardGameDef;

namespace CGS.Menu
{
    public class CreateMenu : Modal
    {
        public const string CreateWarningMessage = "A game with that name already exists!";
        public const string CreationWarningMessage = "Failed to create the custom card game! ";
        public const string CreationCleanupErrorMessage = "Failed to both create and cleanup during creation! ";

        public List<InputField> inputFields;
        public Button createButton;

        public string GameName { get; set; }
        public string BannerImageUrl { get; set; }
        public string CardBackImageUrl { get; set; }

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

        public void ValidateCreateButton()
        {
            createButton.interactable = !string.IsNullOrEmpty(GameName) && !string.IsNullOrEmpty(BannerImageUrl) && !string.IsNullOrEmpty(CardBackImageUrl)
                && Uri.IsWellFormedUriString(BannerImageUrl, UriKind.Absolute) && Uri.IsWellFormedUriString(CardBackImageUrl, UriKind.Absolute);
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

            string gameName = inputFields[0].text.Trim();
            if (CardGameManager.Instance.AllCardGames.ContainsKey(gameName))
            {
                CardGameManager.Instance.Messenger.Show(CreateWarningMessage);
                yield break;
            }

            CardGame newCardGame = new CardGame(CardGameManager.Instance, gameName);
            newCardGame.AutoUpdate = -1;
            newCardGame.BannerImageUrl = inputFields[1].text.Trim();
            newCardGame.CardBackImageUrl = inputFields[2].text.Trim();

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
