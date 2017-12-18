using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExtensibleCardZone : MonoBehaviour, ICardDropHandler
{
    public ZonesViewer Viewer { get; set; }

    public GameObject cardPrefab;
    public List<CardDropZone> cardDropZones;
    public RectTransform extension;
    public RectTransform extensionContent;
    public Text labelText;
    public Text countText;

    public bool IsExtended { get; private set; }

    void Start()
    {
        foreach (CardDropZone dropZone in cardDropZones)
            dropZone.dropHandler = this;
        OnStart();
    }

    public virtual void OnStart()
    {
        extensionContent.gameObject.GetOrAddComponent<CardStack>().OnAddCardActions.Add(OnAddCardModel);
        extensionContent.gameObject.GetOrAddComponent<CardStack>().OnRemoveCardActions.Add(OnRemoveCardModel);
    }

    public virtual void OnDrop(CardModel cardModel)
    {
        AddCard(cardModel.Value);
    }

    public virtual void AddCard(Card card)
    {
        CardModel newCardModel = Instantiate(cardPrefab, extensionContent).GetOrAddComponent<CardModel>();
        newCardModel.Value = card;
        OnAddCardModel(null, newCardModel);
    }

    public virtual void OnAddCardModel(CardStack cardStack, CardModel cardModel)
    {
        if (cardModel == null)
            return;

        CardModel.ShowCard(cardStack, cardModel);
        CardModel.ResetRotation(cardStack, cardModel);
        cardModel.DoubleClickAction = CardModel.ToggleFacedown;
        cardModel.SecondaryDragAction = null;

        UpdateCountText();
    }

    public virtual void OnRemoveCardModel(CardStack cardStack, CardModel cardModel)
    {
        UpdateCountText();
    }

    public virtual void UpdateCountText()
    {
        countText.text = extensionContent.childCount.ToString();
    }

    public virtual void ToggleExtension()
    {
        IsExtended = !IsExtended;
        ResizeExtension();
        extension.gameObject.GetOrAddComponent<CanvasGroup>().alpha = IsExtended ? 1 : 0;
        extension.gameObject.GetOrAddComponent<CanvasGroup>().blocksRaycasts = IsExtended;
    }

    public void ResizeExtension()
    {
        RectTransform canvasRT = Viewer.transform as RectTransform;
        RectTransform.Edge edge = Viewer.ActiveScrollView == Viewer.verticalScrollView ? RectTransform.Edge.Right : RectTransform.Edge.Top;
        float inset = (Viewer.ActiveScrollView == Viewer.verticalScrollView ? ZonesViewer.VerticalWidth : ZonesViewer.HorizontalHeight) - ZonesViewer.ButtonLength;
        float width = canvasRT.rect.width - ZonesViewer.VerticalWidth + (Viewer.IsExtended ? 0 : inset);
        float height = canvasRT.rect.height - ZonesViewer.ButtonLength - (Viewer.IsExtended ? 0 : inset);
        extension.SetInsetAndSizeFromParentEdge(edge, inset, Viewer.ActiveScrollView == Viewer.verticalScrollView ? width : height);

        if (Viewer.ActiveScrollView == Viewer.verticalScrollView) {
            extension.anchorMin = new Vector2(extension.anchorMin.x, 0);
            extension.anchorMax = new Vector2(extension.anchorMin.x, 1);
            extension.offsetMin = new Vector2(extension.offsetMin.x, 0);
            extension.offsetMax = new Vector2(extension.offsetMax.x, 0);
        } else {
            extension.anchorMin = new Vector2(0, extension.anchorMin.y);
            extension.anchorMax = new Vector2(1, extension.anchorMin.y);
            extension.offsetMin = new Vector2(0, extension.offsetMin.y);
            extension.offsetMax = new Vector2(0, extension.offsetMax.y);
		}

		ReorientExtension ();
    }

	public void ReorientExtension()
	{
		CardStack cardStack = extensionContent.gameObject.GetOrAddComponent<CardStack> ();
		if (Viewer.ActiveScrollView == Viewer.verticalScrollView) {
			if (cardStack.scrollRectContainer != null) {
				cardStack.scrollRectContainer.vertical = false;
				cardStack.scrollRectContainer.horizontal = true;
			}
			cardStack.type = CardStackType.Horizontal;
			VerticalLayoutGroup oldLayout = extensionContent.GetComponent<VerticalLayoutGroup> ();
			if (oldLayout != null)
				GameObject.DestroyImmediate (oldLayout);
			HorizontalLayoutGroup newLayout = extensionContent.GetComponent<HorizontalLayoutGroup> ();
			if (newLayout == null)
				newLayout = extensionContent.gameObject.AddComponent<HorizontalLayoutGroup> ();
		} else {
			if (cardStack.scrollRectContainer != null) {
				cardStack.scrollRectContainer.vertical = true;
				cardStack.scrollRectContainer.horizontal = false;
			}
			cardStack.type = CardStackType.Vertical;
			HorizontalLayoutGroup oldLayout = extensionContent.GetComponent<HorizontalLayoutGroup> ();
			if (oldLayout != null)
				GameObject.DestroyImmediate (oldLayout);
			VerticalLayoutGroup newLayout = extensionContent.GetComponent<VerticalLayoutGroup> ();
			if (newLayout == null)
				newLayout = extensionContent.gameObject.AddComponent<VerticalLayoutGroup> ();
		}
		
	}
}
