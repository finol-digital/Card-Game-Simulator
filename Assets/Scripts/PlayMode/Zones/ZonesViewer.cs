using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ZonesViewer : MonoBehaviour
{
    public const float Width = 350f;
    public const float Height = 300f;
    public const float ScrollbarWidth = 80f;

    public GameObject extraZonePrefab;
    public GameObject discardZonePrefab;
    public GameObject deckZonePrefab;
    public GameObject handZonePrefab;
    public ScrollRect scrollView;
    public GameObject extendButton;
    public GameObject condenseButton;
    public GameObject showButton;
    public GameObject hideButton;

    public StackedZone DiscardZone { get; private set; }
    public StackedZone CurrentDeckZone { get; private set; }
    public ExtensibleCardZone HandZone { get; private set; }

    protected List<ExtensibleCardZone> AllZones { get; } = new List<ExtensibleCardZone>();

    void Start()
    {
        if (CardGameManager.Current.GameHasDiscardZone) {
            DiscardZone = Instantiate(discardZonePrefab, scrollView.content).GetComponent<StackedZone>();
            DiscardZone.Viewer = this;
            AllZones.Add(DiscardZone);
            ResizeContent();
        }
    }

    void OnRectTransformDimensionsChange()
    {
        if (!gameObject.activeInHierarchy)
            return;
        IsExtended = IsExtended;
        IsVisible = IsVisible;
        ResizeContent();
    }

    public void ResizeContent()
    {
        float height = Height;
        foreach (ExtensibleCardZone zone in AllZones)
            height += ((RectTransform)zone.transform).rect.height;
        scrollView.content.sizeDelta = new Vector2(scrollView.content.sizeDelta.x, height);
    }

    public void CreateExtraZone(string name, List<Card> cards)
    {
        ExtensibleCardZone extraZone = Instantiate(extraZonePrefab, scrollView.content).GetComponent<ExtensibleCardZone>();
        extraZone.labelText.text = name;
        foreach (Card card in cards)
            extraZone.AddCard(card);
        extraZone.Viewer = this;
        AllZones.Add(extraZone);
        ResizeContent();
    }

    public void CreateDeck()
    {
        CurrentDeckZone = Instantiate(deckZonePrefab, scrollView.content).GetComponent<StackedZone>();
        CurrentDeckZone.Viewer = this;
        AllZones.Add(CurrentDeckZone);
        ResizeContent();
    }

    public void CreateHand()
    {
        if (HandZone != null)
            return;

        HandZone = Instantiate(handZonePrefab, scrollView.content).GetComponent<ExtensibleCardZone>();
        HandZone.Viewer = this;
        AllZones.Add(HandZone);
        ResizeContent();
        IsExtended = true;
        IsVisible = true;
    }

    public bool IsExtended {
        get {
            return ((RectTransform)scrollView.transform.parent).anchoredPosition.x < 1 ;
        }
        set {
            RectTransform.Edge edge = RectTransform.Edge.Right;
            float size = Width;
            float inset = value ? 0 : -(size - ScrollbarWidth);
            ((RectTransform)scrollView.transform.parent ).SetInsetAndSizeFromParentEdge(edge, inset, size);
            foreach (ExtensibleCardZone zone in AllZones)
                zone.ResizeExtension();
            extendButton.SetActive(!value);
            condenseButton.SetActive(value);
        }
    }

    public bool IsVisible {
        get { return scrollView.gameObject.activeSelf; }
        set {
            scrollView.gameObject.SetActive(value);
            RectTransform.Edge edge = RectTransform.Edge.Top;
            float size = GetComponent<RectTransform>().rect.height;
            ((RectTransform)scrollView.transform.parent).SetInsetAndSizeFromParentEdge(edge, 0, scrollView.gameObject.activeSelf ? size : ScrollbarWidth);
            showButton.SetActive(!value);
            hideButton.SetActive(value);
        }
    }
}
