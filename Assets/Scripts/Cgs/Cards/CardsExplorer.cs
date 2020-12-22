/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using Cgs.CardGameView;
using Cgs.Menu;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.Cards
{
    public class CardsExplorer : MonoBehaviour
    {
        public GameObject cardCreationMenuPrefab;
        public GameObject cardViewerPrefab;
        public Image bannerImage;
        public List<GameObject> editButtons;
        public SearchResults searchResults;

        private CardCreationMenu CardCreator => _cardCreator
            ? _cardCreator
            : _cardCreator = Instantiate(cardCreationMenuPrefab).GetOrAddComponent<CardCreationMenu>();

        private CardCreationMenu _cardCreator;

        private void OnEnable()
        {
            Instantiate(cardViewerPrefab);
            CardViewer.Instance.Mode = CardViewerMode.Expanded;
            CardGameManager.Instance.OnSceneActions.Add(ResetBannerCardsAndButtons);
        }

        private void Update()
        {
            if (CardViewer.Instance.IsVisible || CardViewer.Instance.Zoom ||
                CardGameManager.Instance.ModalCanvas != null || searchResults.inputField.isFocused)
                return;

            if (Inputs.IsFocus)
                searchResults.inputField.ActivateInputField();
            else if (Inputs.IsFilter)
                searchResults.ShowSearchMenu();
            else if (Inputs.IsNew)
                ShowCardCreationMenu();
            else if (Inputs.IsCancel)
                BackToMainMenu();
        }

        private void ResetBannerCardsAndButtons()
        {
            bannerImage.sprite = CardGameManager.Current.BannerImageSprite;
            var cardSize = new Vector2(CardGameManager.Current.CardSize.X, CardGameManager.Current.CardSize.Y);
            ((GridLayoutGroup) searchResults.layoutGroup).cellSize = cardSize * CardGameManager.PixelsPerInch;
            foreach (GameObject button in editButtons)
                button.SetActive(!CardGameManager.Current.IsExternal);
        }

        [UsedImplicitly]
        public void ShowCardCreationMenu()
        {
            CardCreator.Show(searchResults.Search);
        }

        [UsedImplicitly]
        public void BackToMainMenu()
        {
            SceneManager.LoadScene(MainMenu.MainMenuSceneIndex);
        }
    }
}
