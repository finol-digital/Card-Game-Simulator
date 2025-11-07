/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.CardGameView.Viewer;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.Cards
{
    public class CardEditorManager : MonoBehaviour
    {
        public CardsExplorer cardsExplorer;
        public SearchResults searchResults;
        private Button _editButton;

        private void Start()
        {
            _editButton = gameObject.GetOrAddComponent<Button>();
        }

        private void Update()
        {
            _editButton.interactable = CardViewer.Instance != null && CardViewer.Instance.SelectedCardModel != null;
            if (InputManager.IsLoad && CardGameManager.Instance.ModalCanvas == null && !searchResults.inputField.isFocused)
                ShowCardEditorMenu();
        }

        [UsedImplicitly]
        public void ShowCardEditorMenu()
        {
            cardsExplorer.ShowEditCard();
        }
    }
}
