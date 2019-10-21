/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

using CardGameDef;

namespace CGS.Menu
{
    public class CreateMenu : Modal
    {
        public const string CreateWarningMessage = "A game with that name already exists!";

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

        public bool ValidateCreateButton()
        {
            createButton.interactable = !string.IsNullOrEmpty(GameName) && !string.IsNullOrEmpty(BannerImageUrl) && !string.IsNullOrEmpty(CardBackImageUrl)
                && Uri.IsWellFormedUriString(BannerImageUrl, UriKind.Absolute)&& Uri.IsWellFormedUriString(CardBackImageUrl, UriKind.Absolute);
            return createButton.interactable;
        }

        public void StartCreation()
        {
            if (!ValidateCreateButton())
                return;

            string gameName = inputFields[0].text.Trim();
            if (CardGameManager.Instance.AllCardGames.ContainsKey(gameName))
            {
                CardGameManager.Instance.Messenger.Show(CreateWarningMessage);
                return;
            }

            CardGame newCardGame = new CardGame(CardGameManager.Instance, gameName);
            newCardGame.BannerImageUrl = inputFields[1].text.Trim();
            newCardGame.CardBackImageUrl = inputFields[2].text.Trim();
            CardGameManager.Instance.AllCardGames[newCardGame.Id] = newCardGame;

            string dominoesDirectory = CardGame.GamesDirectoryPath + "/" + Tags.DominoesDirectoryName;
            if (!Directory.Exists(dominoesDirectory))
                Directory.CreateDirectory(dominoesDirectory);
            File.WriteAllText(dominoesDirectory + "/" + Tags.DominoesJsonFileName, Tags.DominoesJsonFileContent);
            StartCoroutine(UnityExtensionMethods.SaveUrlToFile(Tags.DominoesCardBackUrl, dominoesDirectory + "/CardBack.png"));

            Hide();
        }

        public IEnumerator DownloadImages()
        {
            yield return null;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
