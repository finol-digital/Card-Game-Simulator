/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using CardGameDef;
using Cgs.CardGameView;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.Cards
{
    public class CardDeletionManager : MonoBehaviour
    {
        public SearchResults searchResults;
        private Button _deleteButton;

        private void Start()
        {
            _deleteButton = gameObject.GetOrAddComponent<Button>();
        }

        private void Update()
        {
            _deleteButton.interactable = CardViewer.Instance != null && CardViewer.Instance.SelectedCardModel != null;
            if (Inputs.IsLoad && CardGameManager.Instance.ModalCanvas == null && !searchResults.inputField.isFocused)
                Delete();
        }

        [UsedImplicitly]
        public void Delete()
        {
            if (CardViewer.Instance == null || CardViewer.Instance.SelectedCardModel == null)
                return;

            Card toDelete = CardViewer.Instance.SelectedCardModel.Value;
            if (toDelete == null)
                return;

            CardViewer.Instance.SelectedCardModel = null;
            CardGameManager.Current.Remove(toDelete);
            searchResults.Search();
        }
    }
}
