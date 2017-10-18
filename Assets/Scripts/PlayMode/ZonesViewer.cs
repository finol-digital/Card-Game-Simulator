using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZonesViewer : MonoBehaviour
{
    public const float HiddenWidth = 270f;
    public const float TotalWidth = 350f;

    public RectTransform zonesArea;
    public RectTransform zoneExtension;

    public bool IsVisible { get; set; }

    public bool WasVisible { get; private set; }

    void Start()
    {
        IsVisible = GetComponent<RectTransform>().rect.width > GetComponent<RectTransform>().rect.height;
        WasVisible = !IsVisible;
    }

    void Update()
    {
        if (IsVisible == WasVisible)
            return;
        
        zonesArea.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, IsVisible ? 0 : -HiddenWidth, TotalWidth);

        float width = ((RectTransform)this.transform).rect.width;
        zoneExtension.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, TotalWidth - (IsVisible ? 0 : HiddenWidth), width - TotalWidth + (IsVisible ? 0 : HiddenWidth));

        WasVisible = IsVisible;
    }

    void OnRectTransformDimensionsChange()
    {
        if (!this.gameObject.activeInHierarchy)
            return;
        
        IsVisible = GetComponent<RectTransform>().rect.width > GetComponent<RectTransform>().rect.height;
    }

}
