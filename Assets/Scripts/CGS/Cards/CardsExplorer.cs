/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using CardGameDef;
using CardGameView;

namespace CGS.Cards
{
    public class CardsExplorer : MonoBehaviour
    {
        public GameObject cardViewerPrefab;
        public Image bannerImage;
        public SearchResults searchResults;

        void OnEnable()
        {
            Instantiate(cardViewerPrefab);
            CardGameManager.Instance.OnSceneActions.Add(ResetBanner);
        }

        void Update()
        {
            if (CardInfoViewer.Instance.IsVisible || CardGameManager.Instance.TopMenuCanvas != null)
                return;

            if (Input.GetButtonDown(Inputs.FocusName) || Input.GetAxis(Inputs.FocusName) != 0)
                searchResults.inputField.ActivateInputField();
            else if (Input.GetButtonDown(Inputs.Filter))
                searchResults.ShowSearchMenu();
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(Inputs.Cancel))
                BackToMainMenu();
        }

        public void ResetBanner()
        {
            bannerImage.sprite = CardGameManager.Current.BannerImageSprite;
        }

        public void BackToMainMenu()
        {
            SceneManager.LoadScene(CGS.Menu.MainMenu.MainMenuSceneIndex);
        }
    }
}
