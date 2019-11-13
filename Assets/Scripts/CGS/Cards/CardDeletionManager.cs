/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;
using UnityEngine.UI;

using CardGameDef;
using CardGameView;

namespace CGS.Cards
{
    public class CardDeletionManager : MonoBehaviour
    {
        public SearchResults searchResults;
        private Button _deleteButton;
        void Start()
        {
            _deleteButton = gameObject.GetOrAddComponent<Button>();
        }
        void Update()
        {
            _deleteButton.interactable = CardViewer.Instance?.SelectedCardModel != null;
            if (Input.GetButtonDown(Inputs.Load) && CardGameManager.Instance.ModalCanvas == null && !searchResults.inputField.isFocused)
                Delete();
        }

        public void Delete()
        {
            Card toDelete = CardViewer.Instance?.SelectedCardModel?.Value;
            if (toDelete == null)
                return;

            CardViewer.Instance.SelectedCardModel = null;
            CardGameManager.Current.Remove(toDelete);
            searchResults.Search();
        }
    }
}
