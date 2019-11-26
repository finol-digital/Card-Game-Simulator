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

using CardGameDef;
using CGS.Menu;

namespace CGS.Cards
{
    public class CardCreationMenu : Modal
    {
        public const string ImageCreationFailedWarningMessage = "Failed to get the image! Unable to create the card.";
        public List<InputField> inputFields;
        public Button createButton;

        public string CardName { get; set; }
        public string CardImageUri { get; set; }

        private UnityAction _onCreationCallback;

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

        public void Show(UnityAction onCreationCallback)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            _onCreationCallback = onCreationCallback;
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

            createButton.interactable = false;

            Card newCard = new Card(CardGameManager.Current, Guid.NewGuid().ToString().ToUpper(), CardName, Set.DefaultCode, null, false);
            newCard.ImageWebUrl = CardImageUri;
            yield return UnityExtensionMethods.SaveUrlToFile(CardImageUri, newCard.ImageFilePath);

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
