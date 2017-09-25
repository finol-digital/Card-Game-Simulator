using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZonesViewer : MonoBehaviour
{
    public const float HiddenWidth = 395f;
    public const float TotalWidth = 475f;

    public RectTransform zonesCondensed;
    public RectTransform zonesExtended;

    public bool IsVisible { get; set; }

    public bool WasVisible { get; private set; }

    void Start()
    {
        IsVisible = true;
        #if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        IsVisible = GetComponent<RectTransform>().rect.width > GetComponent<RectTransform>().rect.height;
        #endif
        WasVisible = !IsVisible;
    }

    void Update()
    {
        if (IsVisible == WasVisible)
            return;
        
        zonesCondensed.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, IsVisible ? 0 : -HiddenWidth, TotalWidth);

        float width = ((RectTransform)this.transform).rect.width;
        zonesExtended.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, TotalWidth - (IsVisible ? 0 : HiddenWidth), width - TotalWidth + (IsVisible ? 0 : HiddenWidth));

        WasVisible = IsVisible;
    }

    #if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
    void OnRectTransformDimensionsChange()
    {
        IsVisible = GetComponent<RectTransform>().rect.width > GetComponent<RectTransform>().rect.height;
    }
    #endif

}
