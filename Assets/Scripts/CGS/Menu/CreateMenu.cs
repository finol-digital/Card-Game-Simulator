/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

using CardGameDef;

namespace CGS.Menu
{
    public class CreateMenu : Modal
    {
        public List<InputField> inputFields;
        public Button createButton;

        public string GameName { get; set; }
        public string BannerImageUrl { get; set; }
        public string CardBackImageUrl { get; set; }

        void Update()
        {
            if (!IsFocused || inputFields.Any(inputField => inputField.isFocused))
                return;

            /*
            if ((Input.GetKeyDown(Inputs.BluetoothReturn) || Input.GetButtonDown(Inputs.Submit) || Input.GetButtonDown(Inputs.New))
                 && downloadButton.interactable)
                StartDownload();
            else if ((Input.GetButtonDown(Inputs.Sort) || Input.GetButtonDown(Inputs.Load)) && urlInput.interactable)
                Clear();
            else if ((Input.GetButtonDown(Inputs.Filter) ||Input.GetButtonDown(Inputs.Save)) && urlInput.interactable)
                Paste();
            else if (((Input.GetButtonDown(Inputs.FocusBack) || Input.GetAxis(Inputs.FocusBack) != 0)
                || (Input.GetButtonDown(Inputs.FocusNext) || Input.GetAxis(Inputs.FocusNext) != 0)) && urlInput.interactable)
                urlInput.ActivateInputField();
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(Inputs.Cancel) || Input.GetButtonDown(Inputs.Option))
                Hide(); */
        }

        public void Show()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void ValidateCreateButton()
        {
            createButton.interactable = !string.IsNullOrEmpty(GameName) && !string.IsNullOrEmpty(BannerImageUrl) && !string.IsNullOrEmpty(CardBackImageUrl);
        }

        public void Create()
        {
            // TODO:
            Hide();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
