/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;

using CardGameView;

namespace CGS.Cards
{
    public class SearchResultsLayout : MonoBehaviour
    {
        public SearchResults searchResults;

        void OnRectTransformDimensionsChange()
        {
            if (!gameObject.activeInHierarchy)
                return;

            // TODO: CORRECTLY RE-MAP TO CURRENT PAGE
            searchResults.CurrentPageIndex = 0;
            searchResults.UpdateSearchResultsPanel();
            if (CardInfoViewer.Instance != null)
                CardInfoViewer.Instance.IsVisible = false;
        }

    }
}

