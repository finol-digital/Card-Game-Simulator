/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using CardGameView;
using Cgs.Menu;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Cgs.Cards
{
    public class CardsExplorer : MonoBehaviour
    {
        public GameObject cardCreationMenuPrefab;
        public GameObject cardViewerPrefab;
        public Image bannerImage;
        public List<GameObject> editButtons;
        public SearchResults searchResults;

        public CardCreationMenu CardCreator => _cardCreator ??
                                               (_cardCreator = Instantiate(cardCreationMenuPrefab)
                                                   .GetOrAddComponent<CardCreationMenu>());

        private CardCreationMenu _cardCreator;

        void OnEnable()
        {
            Instantiate(cardViewerPrefab);
            CardViewer.Instance.Mode = CardViewerMode.Expanded;
            CardGameManager.Instance.OnSceneActions.Add(ResetBannerCardsAndButtons);
        }

        void Update()
        {
            if (CardViewer.Instance.IsVisible || CardViewer.Instance.Zoom ||
                CardGameManager.Instance.ModalCanvas != null || searchResults.inputField.isFocused)
                return;

            if (Input.GetButtonDown(Inputs.FocusBack) || Math.Abs(Input.GetAxis(Inputs.FocusBack)) > Inputs.Tolerance
                                                      || Input.GetButtonDown(Inputs.FocusNext) ||
                                                      Math.Abs(Input.GetAxis(Inputs.FocusNext)) > Inputs.Tolerance)
                searchResults.inputField.ActivateInputField();
            else if (Input.GetButtonDown(Inputs.Filter))
                searchResults.ShowSearchMenu();
            else if (Input.GetButtonDown(Inputs.New))
                ShowCardCreationMenu();
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown(Inputs.Cancel))
                BackToMainMenu();
        }

        public void ResetBannerCardsAndButtons()
        {
            bannerImage.sprite = CardGameManager.Current.BannerImageSprite;
            ((GridLayoutGroup) searchResults.layoutGroup).cellSize =
                CardGameManager.Current.CardSize * CardGameManager.PixelsPerInch;
            foreach (GameObject button in editButtons)
                button.SetActive(!CardGameManager.Current.IsExternal);
        }

        public void ShowCardCreationMenu()
        {
            CardCreator.Show(searchResults.Search);
        }

        public void BackToMainMenu()
        {
            SceneManager.LoadScene(MainMenu.MainMenuSceneIndex);
        }
    }
}
