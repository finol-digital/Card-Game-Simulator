/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.CardGameView.Viewer;
using FinolDigital.Cgs.Json;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.Cards
{
    public class CardDeletionManager : MonoBehaviour
    {
        public const string DeletePrompt = "Delete {}?";

        public SearchResults searchResults;
        private Button _deleteButton;
        private InputAction _deleteAction;

        private void Start()
        {
            _deleteButton = gameObject.GetOrAddComponent<Button>();
            _deleteAction = InputSystem.actions.FindAction(Tags.PlayerDelete);
        }

        private void Update()
        {
            _deleteButton.interactable = CardViewer.Instance != null && CardViewer.Instance.SelectedCardModel != null;
            if (_deleteButton.interactable && _deleteAction.WasPressedThisFrame()
                                           && CardGameManager.Instance.ModalCanvas == null
                                           && !searchResults.inputField.isFocused)
                PromptDelete();
        }

        [UsedImplicitly]
        public void PromptDelete()
        {
            if (CardViewer.Instance == null || CardViewer.Instance.SelectedCardModel == null)
            {
                Debug.LogWarning("CardDeletionManager::PromptDelete:No card selected to delete.");
                return;
            }

            Card toDelete = CardViewer.Instance.SelectedCardModel.Value;
            if (toDelete == null)
            {
                Debug.LogWarning("CardDeletionManager::PromptDelete:No card selected to delete.");
                return;
            }

            var cardName = string.IsNullOrEmpty(toDelete.Name) ? "this card" : $"'{toDelete.Name}'";
            var prompt = DeletePrompt.Replace("{}", cardName);
            CardGameManager.Instance.Messenger.Prompt(prompt, Delete);
        }

        private void Delete()
        {
            if (CardViewer.Instance == null || CardViewer.Instance.SelectedCardModel == null)
            {
                Debug.LogWarning("CardDeletionManager::Delete:No card selected to delete.");
                return;
            }

            Card toDelete = CardViewer.Instance.SelectedCardModel.Value;
            if (toDelete == null)
            {
                Debug.LogWarning("CardDeletionManager::Delete:No card selected to delete.");
                return;
            }

            CardViewer.Instance.SelectedCardModel = null;
            CardGameManager.Current.Remove(toDelete);
            searchResults.Search();
        }
    }
}
