/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using Cgs.CardGameView.Viewer;
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
        public const string CannotEditCardsMessage =
            "This game has already been uploaded, and it is currently not possible to edit uploaded games.";

        public const string NewCardSetDecisionPrompt = "New Single Card or Set of Cards?";
        public const string SingleCard = "Single Card";
        public const string SetOfCards = "Set of Cards";

        public GameObject cardSetImportModalPrefab;
        public GameObject cardEditorMenuPrefab;
        public GameObject setImportMenuPrefab;
        public GameObject cardViewerPrefab;
        public Image bannerImage;
        public List<GameObject> editButtons;
        public SearchResults searchResults;

        private DecisionModal NewCardSetModal =>
            _newCardSetModal ??= Instantiate(cardSetImportModalPrefab).GetOrAddComponent<DecisionModal>();

        private DecisionModal _newCardSetModal;

        private CardEditorMenu CardEditor =>
            _cardEditor ??= Instantiate(cardEditorMenuPrefab).GetOrAddComponent<CardEditorMenu>();

        private CardEditorMenu _cardEditor;

        private SetImportMenu SetImporter =>
            _setImporter ??= Instantiate(setImportMenuPrefab).GetOrAddComponent<SetImportMenu>();

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
                ShowNewCardSetModal();
            else if (Inputs.IsCancel)
                BackToMainMenu();
        }

        private void ResetBannerCardsAndButtons()
        {
            bannerImage.sprite = CardGameManager.Current.BannerImageSprite;
            var cardSize = new Vector2(CardGameManager.Current.CardSize.X, CardGameManager.Current.CardSize.Y);
            ((GridLayoutGroup) searchResults.layoutGroup).cellSize = cardSize * CardGameManager.PixelsPerInch;
            foreach (var button in editButtons)
                button.SetActive(Settings.DeveloperMode);
        }

        [UsedImplicitly]
        public void ShowNewCardSetModal()
        {
            if (CardGameManager.Current.CgsDeepLink != null &&
                CardGameManager.Current.CgsDeepLink.IsWellFormedOriginalString()
                || CardGameManager.Current.AutoUpdateUrl != null &&
                CardGameManager.Current.AutoUpdateUrl.IsWellFormedOriginalString())
            {
                CardGameManager.Instance.Messenger.Show(CannotEditCardsMessage);
                return;
            }

            NewCardSetModal.Show(NewCardSetDecisionPrompt, new Tuple<string, UnityAction>(SingleCard, ShowCardEditorMenu),
                new Tuple<string, UnityAction>(SetOfCards, ShowSetImportMenu));
        }

        private void ShowCardEditorMenu()
        {
            CardEditor.Show(searchResults.Search);
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
