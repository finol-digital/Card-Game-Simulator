using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZonesViewer : MonoBehaviour
{
    public const float HiddenWidth = 270f;
    public const float TotalWidth = 350f;
    public const float HeightCheck = 1300f;

    public List<ExtensibleCardZone> zones;

    private bool _isVisible;

    void Start()
    {
        IsVisible = GetComponent<RectTransform>().rect.height < HeightCheck;
    }

    void OnRectTransformDimensionsChange()
    {
        if (!this.gameObject.activeInHierarchy)
            return;
        
        IsVisible = GetComponent<RectTransform>().rect.height < HeightCheck;
    }

    public bool IsVisible { 
        get {
            return _isVisible;
        }
        set {
            _isVisible = value;
            ((RectTransform)this.transform).SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, _isVisible ? 0 : -HiddenWidth, TotalWidth);
            foreach (ExtensibleCardZone zone in zones)
                zone.ResetExtensionWidth();
        }
    }

}
