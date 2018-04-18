using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ZonesViewer : MonoBehaviour
{
    public const float Width = 350f;
    public const float Height = 300f;
    public const float ScrollbarWidth = 80f;

    public GameObject resultsZonePrefab;
    public GameObject discardZonePrefab;
    public GameObject extraZonePrefab;
    public GameObject deckZonePrefab;
    public GameObject handZonePrefab;
    public ScrollRect scrollView;
    public GameObject extendButton;
    public GameObject showButton;
    public GameObject hideButton;

    public ExtensibleCardZone Results { get; private set; }
    public StackedZone Discard { get; private set; }
    protected List<ExtensibleCardZone> ExtraZones { get; } = new List<ExtensibleCardZone>();
    public StackedZone CurrentDeck { get; private set; }
    public ExtensibleCardZone Hand { get; private set; }


    void Start()
    {
        if (CardGameManager.Current.GameHasDiscardZone)
            CreateDiscard();
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
        int siblingIndex = 3;
        if (Results != null) {
            height += ((RectTransform)Results.transform).rect.height;
            Results.transform.SetSiblingIndex(siblingIndex);
            siblingIndex++;
        }
        if (Discard != null) {
            height += ((RectTransform)Discard.transform).rect.height;
            Discard.transform.SetSiblingIndex(siblingIndex);
            siblingIndex++;
        }
        foreach (ExtensibleCardZone zone in ExtraZones) {
            height += ((RectTransform)zone.transform).rect.height;
            zone.transform.SetSiblingIndex(siblingIndex);
            siblingIndex++;
        }
        if (CurrentDeck != null) {
            height += ((RectTransform)CurrentDeck.transform).rect.height;
            CurrentDeck.transform.SetSiblingIndex(siblingIndex);
            siblingIndex++;
        }
        if (Hand != null) {
            height += ((RectTransform)Hand.transform).rect.height;
            Hand.transform.SetSiblingIndex(siblingIndex);
            siblingIndex++;
        }
        scrollView.content.sizeDelta = new Vector2(scrollView.content.sizeDelta.x, height);
    }

    public void CreateResults()
    {
        if (Results != null)
            return;

        Results = Instantiate(resultsZonePrefab, scrollView.content).GetComponent<ExtensibleCardZone>();
        Results.Viewer = this;
        ResizeContent();
    }

    public void CreateDiscard()
    {
        if (Discard != null)
            return;

        Discard = Instantiate(discardZonePrefab, scrollView.content).GetComponent<StackedZone>();
        Discard.Viewer = this;
        Discard.IsFaceup = true;
        ResizeContent();
    }

    public void CreateExtraZone(string name, List<Card> cards)
    {
        ExtensibleCardZone extraZone = Instantiate(extraZonePrefab, scrollView.content).GetComponent<ExtensibleCardZone>();
        extraZone.labelText.text = name;
        foreach (Card card in cards)
            extraZone.AddCard(card);
        extraZone.Viewer = this;
        ExtraZones.Add(extraZone);
        ResizeContent();
    }

    public void CreateDeck()
    {
        if (CurrentDeck != null)
            Destroy(CurrentDeck.gameObject);

        CurrentDeck = Instantiate(deckZonePrefab, scrollView.content).GetComponent<StackedZone>();
        CurrentDeck.Viewer = this;
        ResizeContent();
        IsVisible = true;
    }

    public void CreateHand()
    {
        if (Hand != null)
            return;

        Hand = Instantiate(handZonePrefab, scrollView.content).GetComponent<ExtensibleCardZone>();
        Hand.Viewer = this;
        ResizeContent();
        Vector2 size = ((RectTransform)transform).sizeDelta;
        bool isPortrait = size.y > size.x;
        if (isPortrait)
            Hand.ToggleExtension();
        IsExtended = !isPortrait;
    }

    public void ResetButtons()
    {
        extendButton.SetActive(!IsExtended);
        showButton.SetActive(IsExtended && !IsVisible);
        hideButton.SetActive(IsExtended && IsVisible);
    }

    public bool IsExtended {
        get {
            return ((RectTransform)scrollView.transform.parent).anchoredPosition.x < 1 ;
        }
        set {
            RectTransform.Edge edge = RectTransform.Edge.Right;
            float size = Width;
            float inset = value ? 0 : -(size - ScrollbarWidth);
            ((RectTransform)scrollView.transform.parent).SetInsetAndSizeFromParentEdge(edge, inset, size);
            Results?.ResizeExtension();
            Discard?.ResizeExtension();
            foreach (ExtensibleCardZone zone in ExtraZones)
                zone.ResizeExtension();
            CurrentDeck?.ResizeExtension();
            Hand?.ResizeExtension();
            ResetButtons();
        }
    }

    public bool IsVisible {
        get { return scrollView.gameObject.activeSelf; }
        set {
            scrollView.gameObject.SetActive(value);
            RectTransform.Edge edge = RectTransform.Edge.Top;
            float size = GetComponent<RectTransform>().rect.height;
            ((RectTransform)scrollView.transform.parent).SetInsetAndSizeFromParentEdge(edge, 0, scrollView.gameObject.activeSelf ? size : ScrollbarWidth);
            ResetButtons();
        }
    }
}
