using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExtensibleCardZone : MonoBehaviour, ICardDropHandler
{
    public GameObject cardModelPrefab;
    public List<CardDropZone> cardDropZones;
    public RectTransform extension;
    public RectTransform extensionContent;
    public Text labelText;
    public Text countText;

    public ZonesViewer Viewer { get; set; }
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
        CardModel newCardModel = Instantiate(cardModelPrefab, extensionContent).GetOrAddComponent<CardModel>();
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
        RectTransform.Edge edge = RectTransform.Edge.Right;
        float inset = ZonesViewer.Width - ZonesViewer.ScrollbarWidth;
        float width = ((RectTransform)Viewer.transform).rect.width - ZonesViewer.Width + (Viewer.IsExtended ? 0 : inset);
        extension.SetInsetAndSizeFromParentEdge(edge, inset, width);

        extension.anchorMin = new Vector2(extension.anchorMin.x, 0);
        extension.anchorMax = new Vector2(extension.anchorMin.x, 1);
        extension.offsetMin = new Vector2(extension.offsetMin.x, 0);
        extension.offsetMax = new Vector2(extension.offsetMax.x, 0);
    }
}
