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

    protected List<ExtensibleCardZone> Zones { get; } = new List<ExtensibleCardZone>();

    private ScrollRect _activeScrollRect;

    void Start()
    {
        verticalScrollView.transform.parent.gameObject.SetActive(false);
        horizontalScrollView.transform.parent.gameObject.SetActive(false);
        ActiveScrollView = GetComponent<RectTransform>().rect.width > GetComponent<RectTransform>().rect.height ? verticalScrollView : horizontalScrollView;
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
            width += ((RectTransform) zone.transform).rect.width;
            height += ((RectTransform) zone.transform).rect.height;
        }
        verticalScrollView.content.sizeDelta = new Vector2(verticalScrollView.content.sizeDelta.x, height);
        horizontalScrollView.content.sizeDelta = new Vector2(width, horizontalScrollView.content.sizeDelta.y);
    }

    public ScrollRect ActiveScrollView {
        get {
            return _activeScrollRect ?? (_activeScrollRect =
                       GetComponent<RectTransform>().rect.width > GetComponent<RectTransform>().rect.height
                           ? verticalScrollView
                           : horizontalScrollView);
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

    public bool IsExtended {
        get {
            return ActiveScrollView == verticalScrollView ? ((RectTransform)verticalScrollView.transform.parent).anchoredPosition.x < 1 : ((RectTransform)horizontalScrollView.transform.parent).anchoredPosition.y > 1;
        }
        set {
            RectTransform.Edge edge = ActiveScrollView == verticalScrollView ? RectTransform.Edge.Right : RectTransform.Edge.Top;
            float size = ActiveScrollView == verticalScrollView ? VerticalWidth : HorizontalHeight;
            float inset = value ? 0 : -(size - ButtonLength);
            ((RectTransform)ActiveScrollView.transform.parent ).SetInsetAndSizeFromParentEdge(edge, inset, size);
            foreach (ExtensibleCardZone zone in Zones)
                zone.ResizeExtension();
        }
    }

    public bool IsVisible {
        get { return ActiveScrollView.gameObject.activeSelf; }
        set {
            ActiveScrollView.gameObject.SetActive(value);
            RectTransform.Edge edge = ActiveScrollView == verticalScrollView ? RectTransform.Edge.Top : RectTransform.Edge.Right;
            float size = ActiveScrollView == verticalScrollView ? GetComponent<RectTransform>().rect.height : GetComponent<RectTransform>().rect.width;
            ((RectTransform)ActiveScrollView.transform.parent).SetInsetAndSizeFromParentEdge(edge, 0, ActiveScrollView.gameObject.activeSelf ? size : ButtonLength);
        }
    }
}
