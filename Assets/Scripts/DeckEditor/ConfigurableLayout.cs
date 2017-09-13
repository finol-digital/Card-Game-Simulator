using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConfigurableLayout : MonoBehaviour
{
    public Vector2 deckButtonsLandscapePosition {
        get { return new Vector2(-377.5f, 0f); }
    }

    public Vector2 deckButtonPortraitPosition {
        get { return new Vector2(0f, -(deckEditorContent.GetComponent<RectTransform>().rect.height + 125f)); }
    }

    public Vector2 searchNameLandscapePosition {
        get { return new Vector2(200f, -7.5f); }
    }

    public Vector2 searchNamePortraitPosition {
        get { return new Vector2(200f, 92.5f); }
    }

    public ScreenOrientation PreviousOrientation { get; set; }

    public GameObject deckEditorContent;
    public RectTransform deckButtons;
    public RectTransform searchName;

    #if !UNITY_EDITOR
    
    void Start()
    {
        if (Screen.orientation == ScreenOrientation.Portrait || Screen.orientation == ScreenOrientation.PortraitUpsideDown) {
            deckButtons.anchoredPosition = deckButtonPortraitPosition;
            searchName.anchoredPosition = searchNamePortraitPosition;
        }

        PreviousOrientation = Screen.orientation;
    }

    void Update()
    {
        if (PreviousOrientation == Screen.orientation)
            return;

        if (Screen.orientation == ScreenOrientation.Portrait || Screen.orientation == ScreenOrientation.PortraitUpsideDown) {
            deckButtons.anchoredPosition = deckButtonPortraitPosition;
            searchName.anchoredPosition = searchNamePortraitPosition;
        } else {
            deckButtons.anchoredPosition = deckButtonsLandscapePosition;
            searchName.anchoredPosition = searchNameLandscapePosition;
        }

        PreviousOrientation = Screen.orientation;
    }

    #endif

}
