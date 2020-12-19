/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.CardGameView;
using UnityEngine;

namespace Cgs.Cards
{
    public class SearchResultsLayout : MonoBehaviour
    {
        public SearchResults searchResults;

        private void OnRectTransformDimensionsChange()
        {
            if (!gameObject.activeInHierarchy)
                return;

            searchResults.CurrentPageIndex = 0;
            searchResults.UpdateSearchResultsPanel();
            if (CardViewer.Instance != null)
                CardViewer.Instance.IsVisible = false;
        }
    }
}
