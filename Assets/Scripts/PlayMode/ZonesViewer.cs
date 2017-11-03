using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZonesViewer : MonoBehaviour
{
    public const float HiddenWidth = 270f;
    public const float TotalWidth = 350f;
    public const float HeightCheck = 1300f;
    public const float InvisibleHeight = 100f;

    public RectTransform zonesPanel;
    public RectTransform zonesScrollView;
    public List<ExtensibleCardZone> zones;

    private bool _isExtended;
    private bool _isVisible;

    void Start()
    {
        IsExtended = zonesPanel.rect.height < HeightCheck;
        IsVisible = true;
    }

    void OnRectTransformDimensionsChange()
    {
        if (!this.gameObject.activeInHierarchy)
            return;
        
        IsExtended = zonesPanel.rect.height < HeightCheck;
    }

    public bool IsExtended {
        get {
            return _isExtended;
        }
        set {
            _isExtended = value;
            zonesPanel.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, _isExtended ? 0 : -HiddenWidth, TotalWidth);
            foreach (ExtensibleCardZone zone in zones)
                zone.ResetExtensionWidth();
        }
    }

    public bool IsVisible {
        get {
            return _isVisible;
        }
        set {
            _isVisible = value;
            zonesPanel.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, _isVisible ? GetComponent<RectTransform>().rect.height : InvisibleHeight);
            zonesScrollView.gameObject.SetActive(_isVisible);
        }
    }

}
