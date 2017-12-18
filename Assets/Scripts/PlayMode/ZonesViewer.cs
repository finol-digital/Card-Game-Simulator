using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ZonesViewer : MonoBehaviour
{
    public const float VerticalWidth = 350f;
    public const float HorizontalHeight = 450f;
    public const float ButtonLength = 80f;

    public ScrollRect verticalScrollView;
    public ScrollRect horizontalScrollView;

    private ScrollRect _activeScrollRect;
    private List<ExtensibleCardZone> _zones;

    void Start()
    {
        verticalScrollView.transform.parent.gameObject.SetActive(false);
        horizontalScrollView.transform.parent.gameObject.SetActive(false);
        ActiveScrollView = GetComponent<RectTransform>().rect.width > GetComponent<RectTransform>().rect.height ? verticalScrollView : horizontalScrollView;
        IsExtended = true;
        IsVisible = true;
    }

    public void AddZone(ExtensibleCardZone newZone)
    {
        Zones.Add(newZone);
        newZone.Viewer = this;
        ResizeContent();
    }

    public void ResizeContent()
    {
        float width = 5 * ButtonLength;
        float height = 2 * ButtonLength;
        foreach (ExtensibleCardZone zone in Zones) {
            width += (zone.transform as RectTransform).rect.width;
            height += (zone.transform as RectTransform).rect.height;
        }
        verticalScrollView.content.sizeDelta = new Vector2(verticalScrollView.content.sizeDelta.x, height);
        horizontalScrollView.content.sizeDelta = new Vector2(width, horizontalScrollView.content.sizeDelta.y);
    }

    public ScrollRect ActiveScrollView { 
        get {
            if (_activeScrollRect == null)
                _activeScrollRect = GetComponent<RectTransform>().rect.width > GetComponent<RectTransform>().rect.height ? verticalScrollView : horizontalScrollView;
            return _activeScrollRect;
        }
        set {
            if (_activeScrollRect != null)
                _activeScrollRect.transform.parent.gameObject.SetActive(false);
            _activeScrollRect = value;
            foreach (ExtensibleCardZone zone in Zones) {
                zone.transform.SetParent(_activeScrollRect.content);
                zone.ResizeExtension();
            }
            _activeScrollRect.transform.parent.gameObject.SetActive(true);
        }
    }

    protected List<ExtensibleCardZone> Zones {
        get {
            if (_zones == null)
                _zones = new List<ExtensibleCardZone>();
            return _zones;
        }
    }

    public bool IsExtended {
        get {
            return ActiveScrollView == verticalScrollView ? (verticalScrollView.transform.parent as RectTransform).anchoredPosition.x < 1 : (horizontalScrollView.transform.parent as RectTransform).anchoredPosition.y > 1;
        }
        set {
            RectTransform.Edge edge = ActiveScrollView == verticalScrollView ? RectTransform.Edge.Right : RectTransform.Edge.Top;
            float size = ActiveScrollView == verticalScrollView ? VerticalWidth : HorizontalHeight;
            float inset = value ? 0 : -(size - ButtonLength);
            (ActiveScrollView.transform.parent as RectTransform).SetInsetAndSizeFromParentEdge(edge, inset, size);
            foreach (ExtensibleCardZone zone in Zones)
                zone.ResizeExtension();
        }
    }

    public bool IsVisible {
        get {
            return ActiveScrollView.gameObject.activeSelf;
        }
        set {
            ActiveScrollView.gameObject.SetActive(value);
            RectTransform.Edge edge = ActiveScrollView == verticalScrollView ? RectTransform.Edge.Top : RectTransform.Edge.Right;
            float size = ActiveScrollView == verticalScrollView ? GetComponent<RectTransform>().rect.height : GetComponent<RectTransform>().rect.width;
            (ActiveScrollView.transform.parent as RectTransform).SetInsetAndSizeFromParentEdge(edge, 0, ActiveScrollView.gameObject.activeSelf ? size : ButtonLength);
        }
    }
}
