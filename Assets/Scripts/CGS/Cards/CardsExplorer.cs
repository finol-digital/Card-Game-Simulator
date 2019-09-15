/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
            CardViewer.Instance.Mode = CardViewerMode.Expanded;
            CardGameManager.Instance.OnSceneActions.Add(ResetBannerAndResizeCards);
        }

        void Update()
        {
            if (CardViewer.Instance.IsVisible || CardViewer.Instance.Zoom || CardGameManager.Instance.ModalCanvas != null || searchResults.inputField.isFocused)
                return;

            if (Input.GetButtonDown(Inputs.FocusBack) || Input.GetAxis(Inputs.FocusBack) != 0
                    || Input.GetButtonDown(Inputs.FocusNext) || Input.GetAxis(Inputs.FocusNext) != 0)
                searchResults.inputField.ActivateInputField();
            else if (Input.GetButtonDown(Inputs.Filter))
                searchResults.ShowSearchMenu();
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(Inputs.Cancel))
                BackToMainMenu();
        }

        public void ResetBannerAndResizeCards()
        {
            bannerImage.sprite = CardGameManager.Current.BannerImageSprite;
            ((GridLayoutGroup)searchResults.layoutGroup).cellSize = CardGameManager.Current.CardSize * CardGameManager.PixelsPerInch;
        }

        public void BackToMainMenu()
        {
            SceneManager.LoadScene(CGS.Menu.MainMenu.MainMenuSceneIndex);
        }
    }
}
