/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using CardGameDef;
using CardGameView;

namespace CGS.Cards
{
    public class CardsExplorer : MonoBehaviour
    {
        public GameObject cardViewerPrefab;
        public GameObject cardModelPrefab;

        void OnEnable()
        {
            Instantiate(cardViewerPrefab);
        }

        void Update()
        {
            if (CardInfoViewer.Instance.IsVisible || CardGameManager.Instance.TopMenuCanvas != null)
                return;

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(Inputs.Cancel))
                BackToMainMenu();
        }

        public void BackToMainMenu()
        {
            SceneManager.LoadScene(CGS.Menu.MainMenu.MainMenuSceneIndex);
        }
    }
}
