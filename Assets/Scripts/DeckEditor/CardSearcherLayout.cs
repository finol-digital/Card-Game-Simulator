using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardSearcherLayout : MonoBehaviour
{

    public Vector2 searchNameLandscapePosition {
        get { return new Vector2(200f, 367.5f); }
    }

    public Vector2 searchNamePortraitPosition {
        get { return new Vector2(200f, 467.5f); }
    }

    public RectTransform searchName;
    public CardSearcher cardSearcher;

    #if UNITY_ANDROID && !UNITY_EDITOR
    void OnRectTransformDimensionsChange()
    {
        if (Screen.orientation == ScreenOrientation.Portrait || Screen.orientation == ScreenOrientation.PortraitUpsideDown)
            searchName.anchoredPosition = searchNamePortraitPosition;
        else
            searchName.anchoredPosition = searchNameLandscapePosition;
        
        cardSearcher.ResultsIndex = 0;
        cardSearcher.UpdateSearchResultsPanel();
        CardInfoViewer.Instance.IsVisible = false;
    }
    #endif

}
