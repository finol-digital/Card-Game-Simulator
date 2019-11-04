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
using CGS.Menu;

namespace CGS.Cards
{
    public class CardCreationMenu : Modal
    {
        public List<InputField> inputFields;
        public Button createButton;

        public string CardName { get; set; }
        public string CardImageUri { get; set; }

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
            createButton.interactable = !string.IsNullOrEmpty(CardName) && !string.IsNullOrEmpty(CardImageUri)
                && Uri.IsWellFormedUriString(CardImageUri, UriKind.Absolute);
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

            // TODO: DYNAMICALLY ASSIGN GLOBALLY UNIQUE ID
            Card newCard = new Card(CardGameManager.Current, UnityExtensionMethods.GetSafeFileName(CardName), CardName, Set.DefaultCode, null, false);
            yield return UnityExtensionMethods.SaveUrlToFile(CardImageUri, newCard.ImageFilePath);

            if (!File.Exists(newCard.ImageFilePath))
            {
                Debug.LogWarning("");
            }
            else
            {

                Hide();
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
