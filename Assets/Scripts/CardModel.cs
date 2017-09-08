using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public delegate void OnDoubleClickDelegate(CardModel doubleClickedCard);

[RequireComponent(typeof(Image), typeof(CanvasGroup))]
public class CardModel : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public bool MakesCopyOnDrag { get; set; }

    public GameObject DraggedCopy { get; set; }

    public Vector2 DragOffset { get; set; }

    public float DownClickId { get; set; }

    public OnDoubleClickDelegate DoubleClickEvent { get; set; }

    private Card _representedCard;
    private RectTransform _placeHolder;
    private BaseEventData _recentEventData;
    private Image _image;

    public void SetAsCard(Card card, bool copyOnDrag = false, OnDoubleClickDelegate onDoubleClick = null)
    {
        RepresentedCard = card;
        MakesCopyOnDrag = copyOnDrag;
        DoubleClickEvent = onDoubleClick;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        RecentEventData = eventData;

        DownClickId = eventData.pointerId;
        // HACK: NOT SURE HOW TO MANAGE THE CARD INFO VIEWER AND CARD MODEL SELECTION/VISIBILITY
        if (EventSystem.current.currentSelectedGameObject == this.gameObject && DoubleClickEvent != null)
            DoubleClickEvent(this);
        else if (CardInfoViewer.Instance.rectTransform.anchorMax.y < (CardInfoViewer.HiddenYMax + CardInfoViewer.VisibleYMax) / 2)
            EventSystem.current.SetSelectedGameObject(this.gameObject, eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        RecentEventData = eventData;
        // HACK: NOT SURE HOW TO MANAGE THE CARD INFO VIEWER AND CARD MODEL SELECTION/VISIBILITY
        if (eventData.pointerId != DownClickId || eventData.dragging || eventData.selectedObject == CardInfoViewer.Instance.gameObject || eventData.selectedObject == this.gameObject)
            return;
        
        EventSystem.current.SetSelectedGameObject(this.gameObject, eventData);
    }

    public void OnSelect(BaseEventData eventData)
    {
        CardInfoViewer.Instance.SelectedCardModel = this;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        CardInfoViewer.Instance.IsVisible = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        RecentEventData = eventData;

        EventSystem.current.SetSelectedGameObject(null, eventData);

        DragOffset = (((Vector2)this.transform.position) - eventData.position);
        if (MakesCopyOnDrag)
            CreateDraggedCopy(eventData.position);
        else
            MoveToContainingCanvas();

        GetComponent<CanvasGroup>().blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        RecentEventData = eventData;

        Vector2 targetPos = eventData.position + DragOffset;
        if (DraggedCopy != null)
            DraggedCopy.transform.position = targetPos;
        else
            this.transform.position = targetPos;

        UpdatePlaceHolderPosition();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        RecentEventData = eventData;
        GetComponent<CanvasGroup>().blocksRaycasts = true;

        // TODO: MOVE TO PLACEHOLDER THROUGH ANIMATION, INSTEAD OF TELEPORTING

        if (_placeHolder != null) {
            Debug.Log(gameObject.name + " had a placeholder, so enabling it before we destroy what was dragged");
            CanvasGroup placeHolderCanvasGroup = _placeHolder.GetOrAddComponent<CanvasGroup>();
            placeHolderCanvasGroup.alpha = 1;
            placeHolderCanvasGroup.blocksRaycasts = true;
            _placeHolder.name = _placeHolder.name.Replace("(Placeholder)", "");
        }

        if (DraggedCopy != null) {
            Debug.Log("Destroying dragged copy of " + gameObject.name);
            Destroy(DraggedCopy);
            DraggedCopy = null;
            _placeHolder = null;
        } else {
            Debug.Log("Destroying moved card " + gameObject.name);
            Destroy(this.gameObject);
        }

        RecentEventData = eventData;
    }

    public void CreateDraggedCopy(Vector2 position)
    {
        Debug.Log("Creating dragged copy for " + gameObject.name);
        Canvas canvas = UnityExtensionMethods.FindInParents<Canvas>(gameObject);
        if (canvas == null)
            return;

        GameObject cardCopy = Instantiate(this.gameObject, canvas.transform);
        cardCopy.name = cardCopy.name.Replace("(Clone)", "(Copy)");
        cardCopy.GetComponent<CanvasGroup>().blocksRaycasts = false;

        DraggedCopy = cardCopy;
        DraggedCopy.transform.position = position + DragOffset;
    }

    public void MoveToContainingCanvas()
    {
        Debug.Log("Moving card model " + gameObject.name + " to the containing canvas");
        CreatePlaceHolderInPanel(transform.parent as RectTransform);
        Canvas canvas = UnityExtensionMethods.FindInParents<Canvas>(gameObject);
        Transform container = this.transform.parent.parent;
        if (canvas != null)
            container = canvas.transform;
        else
            Debug.LogWarning("Attempted to move a card model to it's canvas, but it was not in a canvas. Moving it to it's parent instead");
        this.transform.SetParent(container);
        this.transform.SetAsLastSibling();
    }

    public void CreatePlaceHolderInPanel(RectTransform panel)
    {
        if (panel == null) {
            Debug.LogWarning("Attempted to create a place holder in a null panel. Ignoring");
            return;
        }
        RemovePlaceHolder();

        GameObject cardCopy = Instantiate(this.gameObject, panel);
        cardCopy.name = cardCopy.name.Replace("(Clone)", "(Placeholder)");

        CardModel copyModel = cardCopy.GetComponent<CardModel>();
        copyModel.RepresentedCard = this.RepresentedCard;
        copyModel.MakesCopyOnDrag = false;
        if (!MakesCopyOnDrag)
            copyModel.DoubleClickEvent = this.DoubleClickEvent;

        CanvasGroup placeHolderCanvasGroup = cardCopy.transform.GetOrAddComponent<CanvasGroup>();
        placeHolderCanvasGroup.alpha = 0;
        placeHolderCanvasGroup.blocksRaycasts = false;

        _placeHolder = cardCopy.transform as RectTransform;
        UpdatePlaceHolderPosition();
    }

    public void UpdatePlaceHolderPosition()
    {
        if (_placeHolder == null)
            return;

        Vector2 targetPos = this.transform.position;
        if (DraggedCopy != null)
            targetPos = DraggedCopy.transform.position;

        // TODO: ALLOW HORIZONTAL VS VERTICAL STACKING OF CARDS
        int newSiblingIndex = _placeHolder.parent.childCount;
        for (int i = 0; i < _placeHolder.parent.childCount; i++) {
            if (targetPos.y > _placeHolder.parent.GetChild(i).position.y) {
                newSiblingIndex = i;
                if (_placeHolder.transform.GetSiblingIndex() < newSiblingIndex)
                    newSiblingIndex--;
                break;
            }
        }
        _placeHolder.transform.SetSiblingIndex(newSiblingIndex);
    }

    public void RemovePlaceHolder()
    {
        if (_placeHolder == null) {
            return;
        }

        Destroy(_placeHolder.gameObject);
        _placeHolder = null;
    }

    public void Highlight()
    {
        Outline outline = this.transform.GetOrAddComponent<Outline>();
        outline.effectColor = Color.green;
        outline.effectDistance = new Vector2(10, 10);
    }

    public void UnHighlight()
    {
        Outline outline = this.transform.GetOrAddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(0, 0);
    }

    public IEnumerator UpdateImage()
    {
        Sprite imageToShow = CardGameManager.Current.CardBackImage;
        yield return UnityExtensionMethods.RunOutputCoroutine<Sprite>(UnityExtensionMethods.LoadOrGetImage(RepresentedCard.ImageFilePath, RepresentedCard.ImageWebURL), (output) => imageToShow = output);
        Image.sprite = imageToShow;
    }

    public Card RepresentedCard {
        get {
            if (_representedCard == null)
                _representedCard = new Card("", "", "", new Dictionary<string, PropertySet>());
            return _representedCard;
        }
        set {
            _representedCard = value;
            if (_representedCard == null) {
                Debug.LogWarning("Attempted to set a card model as a null card! Defaulting to a blank card");
                _representedCard = new Card("", "", "", new Dictionary<string, PropertySet>());
            }
            this.gameObject.name = _representedCard.Name + " [" + _representedCard.Id + "]";
            Image.sprite = CardGameManager.Current.CardBackImage;
            StartCoroutine(UpdateImage());
        }
    }

    public RectTransform PlaceHolder {
        get {
            return _placeHolder;
        }
        set {
            _placeHolder = value;
        }
    }

    public bool HasPlaceHolder {
        get { 
            return _placeHolder != null; 
        }
    }

    public BaseEventData RecentEventData {
        get {
            if (_recentEventData == null) {
                Debug.LogWarning("Attempted to access recent event data for " + this.gameObject.name + ", but it was null! Using a default eventData");
                _recentEventData = new BaseEventData(EventSystem.current);
            }
            return _recentEventData;
        }
        set {
            _recentEventData = value;
        }
    }

    public Image Image {
        get {
            if (_image == null)
                _image = GetComponent<Image>();
            return _image;
        }
    }
}
