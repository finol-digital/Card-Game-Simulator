using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Image))]
public class CardModel : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Card representedCard = null;
    private bool makesCopyOnDrag = false;
    private GameObject draggedCopy = null;
    private Transform placeHolder = null;
    private Vector2 dragOffset = Vector2.zero;
    private float downClickId;

    public void SetAsCard(Card card)
    {
        this.gameObject.name = card.Name + " [" + card.Id + "]";
        representedCard = card;
        GetComponent<Image>().sprite = CardImageRepository.DefaultImage;
        StartCoroutine(UpdateImage());
    }

    public IEnumerator UpdateImage()
    {
        Sprite imageToShow;
        if (!CardImageRepository.TryGetCachedCardImage(representedCard, out imageToShow)) {
            yield return CardImageRepository.GetAndCacheCardImage(representedCard);
            CardImageRepository.TryGetCachedCardImage(representedCard, out imageToShow);
        }
        GetComponent<Image>().sprite = imageToShow;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("Clicked on " + gameObject.name);
        downClickId = eventData.pointerId;
        if (CardInfoViewer.Instance.IsVisible) {
            Debug.Log(" Selecting " + gameObject.name + " on pointer down, since the card info viewer is visible");
            EventSystem.current.SetSelectedGameObject(gameObject, eventData);
        } else
            Debug.Log(" Card info view is not visible, so not selecting " + gameObject.name + " on pointer down");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != downClickId || eventData.dragging) {
            Debug.Log("Let go on " + gameObject.name + ", but did not start the press there or it was dragged, so ignoring the action");
            return;
        }

        Debug.Log("Let go on and therefore selecting " + gameObject.name);
        EventSystem.current.SetSelectedGameObject(gameObject, eventData);
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (representedCard == null) {
            Debug.Log("Selected a card that does not represent anything!");
            return;
        }

        Debug.Log("Selected " + gameObject.name);
        CardInfoViewer.Instance.SelectCard(this);
    }

    public void Highlight()
    {
        Debug.Log("Adding highlight to " + gameObject.name);
        Outline outline = this.transform.GetOrAddComponent<Outline>();
        outline.effectColor = Color.green;
        outline.effectDistance = new Vector2(10, 10);
    }

    public void UnHighlight()
    {
        Debug.Log("Removing highlight from " + gameObject.name);
        Outline outline = this.transform.GetOrAddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(0, 0);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        Debug.Log("Deselected " + gameObject.name);
        CardInfoViewer.Instance.DeselectCard();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("Started dragging " + gameObject.name);
        dragOffset = (((Vector2)this.transform.position) - eventData.position);
        EventSystem.current.SetSelectedGameObject(null, eventData);

        if (makesCopyOnDrag)
            CreateDraggedCopy(eventData.position);
        else
            MoveToContainingCanvas();

        GetComponent<CanvasGroup>().blocksRaycasts = false;
    }

    public void CreateDraggedCopy(Vector2 position)
    {
        Debug.Log("Creating dragged copy for " + gameObject.name);
        Canvas canvas = UnityExtensionMethods.FindInParents<Canvas>(gameObject);
        if (canvas == null)
            return;

        GameObject cardCopy = Instantiate(this.gameObject, canvas.transform);
        cardCopy.name = cardCopy.name.Replace("(Clone)", "(Copy)");

        CanvasGroup cardCopyCanvasGroup = cardCopy.GetComponent<CanvasGroup>();
        if (cardCopyCanvasGroup != null)
            cardCopyCanvasGroup.blocksRaycasts = false;

        draggedCopy = cardCopy;
        draggedCopy.transform.position = position + dragOffset;

    }

    public void MoveToContainingCanvas()
    {
        Debug.Log("Moving card model " + gameObject.name + " to the containing canvas");
        CreatePlaceHolderInPanel(transform.parent);
        Canvas canvas = UnityExtensionMethods.FindInParents<Canvas>(gameObject);
        Transform container = this.transform.parent.parent;
        if (canvas != null)
            container = canvas.transform;
        else
            Debug.LogWarning("Attempted to move a card model to it's canvas, but it was not in a canvas. Moving it to it's parent instead");
        this.transform.SetParent(container);
    }

    public void CreatePlaceHolderInPanel(Transform panel)
    {
        RemovePlaceHolder();

        GameObject cardCopy = Instantiate(this.gameObject, panel);
        cardCopy.name = cardCopy.name.Replace("(Clone)", "(Placeholder)");

        CardModel copyModel = cardCopy.GetComponent<CardModel>();
        copyModel.makesCopyOnDrag = false;
        copyModel.representedCard = this.representedCard;

        CanvasGroup placeHolderCanvasGroup = cardCopy.GetComponent<CanvasGroup>();
        if (placeHolderCanvasGroup != null) {
            placeHolderCanvasGroup.alpha = 0;
            placeHolderCanvasGroup.blocksRaycasts = false;
        }

        placeHolder = cardCopy.transform;
        UpdatePlaceHolderPosition();
        
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 targetPos = eventData.position + dragOffset;
        if (draggedCopy != null)
            draggedCopy.transform.position = targetPos;
        else
            this.transform.position = targetPos;
        
        UpdatePlaceHolderPosition();

    }

    public void UpdatePlaceHolderPosition()
    {
        if (placeHolder == null)
            return;

        Vector2 targetPos = this.transform.position;
        if (draggedCopy != null)
            targetPos = draggedCopy.transform.position;
        
        int newSiblingIndex = placeHolder.parent.childCount;
        for (int i = 0; i < placeHolder.parent.childCount; i++) {
            if (targetPos.x < placeHolder.parent.GetChild(i).position.x) {
                newSiblingIndex = i;
                if (placeHolder.transform.GetSiblingIndex() < newSiblingIndex)
                    newSiblingIndex--;
                break;
            }
        }
        placeHolder.transform.SetSiblingIndex(newSiblingIndex);

    }

    public void RemovePlaceHolder()
    {
        if (placeHolder == null) {
            return;
        }

        Destroy(placeHolder.gameObject);
        placeHolder = null;

    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log("Stopped dragging " + gameObject.name);
        GetComponent<CanvasGroup>().blocksRaycasts = true;
        
        if (placeHolder != null) {
            Debug.Log(" " + gameObject.name + " had a placeholder, so enabling it");
            CanvasGroup placeHolderCanvasGroup = placeHolder.GetComponent<CanvasGroup>();
            if (placeHolderCanvasGroup != null) {
                placeHolderCanvasGroup.alpha = 1;
                placeHolderCanvasGroup.blocksRaycasts = true;
            }
            placeHolder.name = placeHolder.name.Replace("(Placeholder)", "");
        }

        if (draggedCopy != null) {
            Debug.Log(" Destroying dragged copy");
            Destroy(draggedCopy);
            draggedCopy = null;
            placeHolder = null;
        } else {
            Debug.Log(" Destroying moved card");
            Destroy(this.gameObject);
        }

    }

    // TODO: IN UPDATE, CHECK IF WE HAVE A PLACEHOLDER WHILE ALSO NOT BEING DRAGGED; IF WE DO, REMOVE THE PLACEHOLDER

    public Card RepresentedCard {
        get {
            return representedCard;
        }
    }

    public bool MakesCopyOnDrag {
        get {
            return makesCopyOnDrag;
        }
        set {
            makesCopyOnDrag = value;
        }
    }

    public bool HasPlaceHolder {
        get { 
            return placeHolder != null; 
        }
    }
}
