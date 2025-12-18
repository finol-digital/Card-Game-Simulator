/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using Cgs.CardGameView.Viewer;
using Cgs.Menu;
using FinolDigital.Cgs.Json.Unity;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
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

        public GameObject gamesManagementMenuPrefab;

        public GameObject cardSetImportModalPrefab;
        public GameObject cardEditorMenuPrefab;
        public GameObject setImportMenuPrefab;
        public GameObject cardViewerPrefab;
        public Image bannerImage;
        public List<GameObject> editButtons;
        public SearchResults searchResults;

#if !UNITY_WEBGL
        private GamesManagementMenu GamesManagement =>
            _gamesManagement ??= Instantiate(gamesManagementMenuPrefab).GetOrAddComponent<GamesManagementMenu>();

        private GamesManagementMenu _gamesManagement;
#endif

        private DecisionModal NewCardSetModal =>
            _newCardSetModal ??= Instantiate(cardSetImportModalPrefab).GetOrAddComponent<DecisionModal>();

        private DecisionModal _newCardSetModal;

        private CardEditorMenu CardEditor =>
            _cardEditor ??= Instantiate(cardEditorMenuPrefab).GetOrAddComponent<CardEditorMenu>();

        private CardEditorMenu _cardEditor;

        private SetImportMenu SetImporter =>
            _setImporter ??= Instantiate(setImportMenuPrefab).GetOrAddComponent<SetImportMenu>();

        private SetImportMenu _setImporter;

        private bool IsBlocked => CardViewer.Instance.IsVisible || CardViewer.Instance.WasVisible || CardViewer.Instance.Zoom ||
                                  CardGameManager.Instance.ModalCanvas != null || searchResults.inputField.isFocused;

        private void OnEnable()
        {
            Instantiate(cardViewerPrefab);
            CardViewer.Instance.Mode = CardViewerMode.Expanded;
            CardGameManager.Instance.OnSceneActions.Add(ResetBannerCardsAndButtons);

            InputSystem.actions.FindAction(Tags.CardsSort).performed += InputCardsSort;
            InputSystem.actions.FindAction(Tags.CardsFilter).performed += InputCardsFilter;
            InputSystem.actions.FindAction(Tags.SubMenuFocusNext).performed += InputFocusNext;
            InputSystem.actions.FindAction(Tags.CardsNew).performed += InputNewCard;
            InputSystem.actions.FindAction(Tags.CardsEdit).performed += InputEditCard;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed += InputCancel;
        }

        private void ResetBannerCardsAndButtons()
        {
            bannerImage.sprite = CardGameManager.Current.BannerImageSprite;
            var cardSize = new Vector2(CardGameManager.Current.CardSize.X, CardGameManager.Current.CardSize.Y);
            ((GridLayoutGroup)searchResults.layoutGroup).cellSize = cardSize * CardGameManager.PixelsPerInch;
            foreach (var button in editButtons)
                button.SetActive(Settings.DeveloperMode && !CardGameManager.Current.IsUploaded);
        }

        private void InputCardsSort(InputAction.CallbackContext context)
        {
            if (IsBlocked)
                return;

            ShowGamesManagementMenu();
        }

        [UsedImplicitly]
        public void ShowGamesManagementMenu()
        {
#if !UNITY_WEBGL
            GamesManagement.Show();
#endif
        }

        private void InputCardsFilter(InputAction.CallbackContext context)
        {
            if (IsBlocked)
                return;

            searchResults.ShowSearchMenu();
        }

        private void InputFocusNext(InputAction.CallbackContext context)
        {
            if (IsBlocked)
                return;

            searchResults.inputField.ActivateInputField();
        }

        private void InputNewCard(InputAction.CallbackContext context)
        {
            if (IsBlocked)
                return;

            ShowNewCardSetModal();
        }

        [UsedImplicitly]
        public void ShowNewCardSetModal()
        {
            if (CardGameManager.Current.IsUploaded)
            {
                CardGameManager.Instance.Messenger.Show(CannotEditCardsMessage);
                return;
            }

            NewCardSetModal.Show(NewCardSetDecisionPrompt,
                new Tuple<string, UnityAction>(SingleCard, ShowCardEditorMenu),
                new Tuple<string, UnityAction>(SetOfCards, ShowSetImportMenu));
        }

        private void InputEditCard(InputAction.CallbackContext context)
        {
            if (IsBlocked)
                return;

            ShowEditCard();
        }

        public void ShowEditCard()
        {
            if (CardViewer.Instance == null || CardViewer.Instance.SelectedCardModel == null)
            {
                Debug.LogWarning("CardsExplorer::ShowEditCard:No card selected to edit.");
                return;
            }

            var unityCard = CardViewer.Instance.SelectedCardModel.Value;
            if (unityCard == null)
            {
                Debug.LogWarning("CardsExplorer::ShowEditCard:No card selected to edit.");
                return;
            }

            ShowCardEditorMenuFor(unityCard);
        }

        private void ShowCardEditorMenuFor(UnityCard unityCard)
        {
            CardEditor.ShowFor(unityCard, searchResults.Search);
        }

        private void ShowCardEditorMenu()
        {
            CardEditor.Show(searchResults.Search);
        }

        private void ShowSetImportMenu()
        {
            SetImporter.Show(searchResults.Search);
        }

        private void InputCancel(InputAction.CallbackContext context)
        {
            if (IsBlocked)
                return;

            BackToMainMenu();
        }

        [UsedImplicitly]
        public void BackToMainMenu()
        {
            SceneManager.LoadScene(Tags.MainMenuSceneIndex);
        }

        private void OnDisable()
        {
            InputSystem.actions.FindAction(Tags.CardsFilter).performed -= InputCardsFilter;
            InputSystem.actions.FindAction(Tags.SubMenuFocusNext).performed -= InputFocusNext;
            InputSystem.actions.FindAction(Tags.CardsNew).performed -= InputNewCard;
            InputSystem.actions.FindAction(Tags.CardsEdit).performed -= InputEditCard;
            InputSystem.actions.FindAction(Tags.PlayerCancel).performed -= InputCancel;
        }
    }
}
