using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SearchResultsLayout : MonoBehaviour
{
    public const float WidthCheck = 1000f;

    public Vector2 SearchNameLandscapePosition {
        get { return new Vector2(150f, 367.5f); }
    }

    public Vector2 SearchNamePortraitPosition {
        get { return new Vector2(150f, 450f); }
    }

    public RectTransform searchName;
    public SearchResults searchResults;

    void OnRectTransformDimensionsChange()
    {
        if (!this.gameObject.activeInHierarchy)
            return;

        if (GetComponent<RectTransform>().rect.width < WidthCheck)
            searchName.anchoredPosition = SearchNamePortraitPosition;
        else
            searchName.anchoredPosition = SearchNameLandscapePosition;
        
        searchResults.CurrentPageIndex = 0;
        searchResults.UpdateSearchResultsPanel();
        CardInfoViewer.Instance.IsVisible = false;
    }

}
