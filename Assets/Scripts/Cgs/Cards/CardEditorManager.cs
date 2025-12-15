/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.CardGameView.Viewer;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.Cards
{
    public class CardEditorManager : MonoBehaviour
    {
        public CardsExplorer cardsExplorer;
        public SearchResults searchResults;
        private Button _editButton;
        private InputAction _editAction;

        private void Start()
        {
            _editButton = gameObject.GetOrAddComponent<Button>();
            _editAction = InputSystem.actions.FindAction(Tags.CardsEdit);
        }

        private void Update()
        {
            _editButton.interactable = CardViewer.Instance != null && CardViewer.Instance.SelectedCardModel != null;
            if (_editButton.interactable && _editAction != null && _editAction.WasPressedThisFrame()
                                         && CardGameManager.Instance.ModalCanvas == null
                                         && !searchResults.inputField.isFocused)
                ShowCardEditorMenu();
        }

        [UsedImplicitly]
        public void ShowCardEditorMenu()
        {
            cardsExplorer.ShowEditCard();
        }
    }
}
