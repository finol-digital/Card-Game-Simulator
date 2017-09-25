using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardSearcherLayout : MonoBehaviour
{
    public const float WidthCheck = 1000f;

    public Vector2 SearchNameLandscapePosition {
        get { return new Vector2(200f, 367.5f); }
    }

    public Vector2 SearchNamePortraitPosition {
        get { return new Vector2(200f, 467.5f); }
    }

    public RectTransform searchName;
    public CardSearcher cardSearcher;

    #if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
    void OnRectTransformDimensionsChange()
    {
        if (GetComponent<RectTransform>().rect.width < WidthCheck)
            searchName.anchoredPosition = SearchNamePortraitPosition;
        else
            searchName.anchoredPosition = SearchNameLandscapePosition;
        
        cardSearcher.ResultsIndex = 0;
        cardSearcher.UpdateSearchResultsPanel();
        CardInfoViewer.Instance.IsVisible = false;
    }
    #endif

}
