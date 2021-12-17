/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using Cgs.CardGameView;
using Cgs.Menu;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.Cards
{
    public class CardsExplorer : MonoBehaviour
    {
        public const string CannotImportMessage =
            "This game has already been uploaded, and it is currently not possible to edit uploaded games.";

        public const string CardSetDecisionPrompt = "Import Single Card or Set of Cards?";
        public const string SingleCard = "Single Card";
        public const string SetOfCards = "Set of Cards";

        public GameObject cardSetImportModalPrefab;
        public GameObject cardImportMenuPrefab;
        public GameObject setImportMenuPrefab;
        public GameObject cardViewerPrefab;
        public Image bannerImage;
        public List<GameObject> editButtons;
        public SearchResults searchResults;

        private DecisionModal ImportModal => _importModal
            ? _importModal
            : _importModal = Instantiate(cardSetImportModalPrefab).GetOrAddComponent<DecisionModal>();

        private DecisionModal _importModal;

        private CardImportMenu CardImporter => _cardImporter
            ? _cardImporter
            : _cardImporter = Instantiate(cardImportMenuPrefab).GetOrAddComponent<CardImportMenu>();

        private CardImportMenu _cardImporter;

        private SetImportMenu SetImporter => _setImporter
            ? _setImporter
            : _setImporter = Instantiate(setImportMenuPrefab).GetOrAddComponent<SetImportMenu>();

        private SetImportMenu _setImporter;

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
                ShowImportModal();
            else if (Inputs.IsCancel)
                BackToMainMenu();
        }

        private void ResetBannerCardsAndButtons()
        {
            bannerImage.sprite = CardGameManager.Current.BannerImageSprite;
            var cardSize = new Vector2(CardGameManager.Current.CardSize.X, CardGameManager.Current.CardSize.Y);
            ((GridLayoutGroup) searchResults.layoutGroup).cellSize = cardSize * CardGameManager.PixelsPerInch;
            foreach (GameObject button in editButtons)
                button.SetActive(Settings.DeveloperMode);
        }

        [UsedImplicitly]
        public void ShowImportModal()
        {
            if (CardGameManager.Current.CgsDeepLink != null &&
                CardGameManager.Current.CgsDeepLink.IsWellFormedOriginalString()
                || CardGameManager.Current.AutoUpdateUrl != null &&
                CardGameManager.Current.AutoUpdateUrl.IsWellFormedOriginalString())
            {
                CardGameManager.Instance.Messenger.Show(CannotImportMessage);
                return;
            }

            ImportModal.Show(CardSetDecisionPrompt, new Tuple<string, UnityAction>(SingleCard, ShowCardImportMenu),
                new Tuple<string, UnityAction>(SetOfCards, ShowSetImportMenu));
        }

        private void ShowCardImportMenu()
        {
            CardImporter.Show(searchResults.Search);
        }

        private void ShowSetImportMenu()
        {
            SetImporter.Show(searchResults.Search);
        }

        [UsedImplicitly]
        public void BackToMainMenu()
        {
            SceneManager.LoadScene(MainMenu.MainMenuSceneIndex);
        }
    }
}
