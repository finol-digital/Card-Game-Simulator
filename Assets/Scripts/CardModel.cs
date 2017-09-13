using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public delegate void OnDoubleClickDelegate(CardModel cardModel);

[RequireComponent(typeof(Image), typeof(CanvasGroup), typeof(LayoutElement))]
public class CardModel : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public const float MovementSpeed = 600f;

    public Vector2 OutlineHighlightDistance {
        get { return new Vector2(10, 10); }
    }

    public bool ClonesOnDrag { get; set; }

    public Vector2 DragOffset { get; set; }

    public OnDoubleClickDelegate DoubleClickEvent { get; set; }

    public PointerEventData RecentPointerEventData { get; set; }

    private Card _representedCard;
    private Dictionary<int, CardModel> _draggedClones;
    private RectTransform _placeHolder;
    private Outline _outline;
    private Sprite _newSprite;
    private Image _image;
    private CanvasGroup _canvasGroup;
    private Canvas _canvas;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.dragging)
            return;

        // HACK: NOT SURE HOW TO MANAGE THE CARD INFO VIEWER AND CARD MODEL SELECTION/VISIBILITY
        if (EventSystem.current.currentSelectedGameObject == this.gameObject && DoubleClickEvent != null)
            DoubleClickEvent(this);
        else if (CardInfoViewer.Instance.rectTransform.anchorMax.y < (CardInfoViewer.HiddenYMax + CardInfoViewer.VisibleYMax) / 2.0f)
            EventSystem.current.SetSelectedGameObject(this.gameObject, eventData);
        RecentPointerEventData = eventData;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (RecentPointerEventData == null || RecentPointerEventData.pointerId != eventData.pointerId || eventData.dragging || eventData.selectedObject == CardInfoViewer.Instance.gameObject || eventData.selectedObject == this.gameObject)
            return;
        // HACK: NOT SURE HOW TO MANAGE THE CARD INFO VIEWER AND CARD MODEL SELECTION/VISIBILITY
        EventSystem.current.SetSelectedGameObject(this.gameObject, eventData);
        RecentPointerEventData = eventData;
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
        RecentPointerEventData = eventData;

        DragOffset = (((Vector2)this.transform.position) - eventData.position);
        if (ClonesOnDrag)
            DragClone();
        else
            MoveToCanvas();

        // HACK: NOT SURE HOW TO MANAGE THE CARD INFO VIEWER AND CARD MODEL SELECTION/VISIBILITY
        EventSystem.current.SetSelectedGameObject(null, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        RecentPointerEventData = eventData;

        Vector2 targetPos = eventData.position + DragOffset;
        CardModel draggedClone;
        if (DraggedClones.TryGetValue(eventData.pointerId, out draggedClone))
            draggedClone.transform.position = targetPos;
        else
            this.transform.position = targetPos;

        UpdatePlaceHolderPosition();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        RecentPointerEventData = eventData;

        CardModel cardModel;
        if (!DraggedClones.TryGetValue(eventData.pointerId, out cardModel))
            cardModel = this;
        
        if (cardModel.PlaceHolder != null)
            cardModel.StartCoroutine(cardModel.MoveToPlaceHolder());
        else
            Destroy(cardModel.gameObject);
    }

    public void DragClone()
    {
        CardModel draggedClone = Instantiate(this.gameObject, Canvas.transform).GetOrAddComponent<CardModel>();
        draggedClone.CanvasGroup.blocksRaycasts = false;
        draggedClone.RepresentedCard = this.RepresentedCard;
        draggedClone.UnHighlight();
        DraggedClones [RecentPointerEventData.pointerId] = draggedClone;
    }

    public void MoveToCanvas()
    {
        CreatePlaceHolderInPanel(transform.parent as RectTransform);
        CanvasGroup.blocksRaycasts = false;
        this.transform.SetParent(Canvas.transform);
        this.transform.SetAsLastSibling();
    }

    public void CreatePlaceHolderInPanel(RectTransform panel)
    {
        if (panel == null) {
            Debug.LogWarning("Attempted to create a place holder in a null panel. Ignoring");
            return;
        }

        GameObject placeholder = new GameObject(this.gameObject.name + "(PlaceHolder)", typeof(RectTransform));
        PlaceHolder = placeholder.transform as RectTransform;
        PlaceHolder.sizeDelta = ((RectTransform)this.transform).sizeDelta;
        PlaceHolder.gameObject.AddComponent(typeof(LayoutElement));
        PlaceHolder.SetParent(panel);

        UpdatePlaceHolderPosition();
    }

    public void UpdatePlaceHolderPosition()
    {
        if (PlaceHolder == null)
            return;
        
        Vector2 targetPos = this.transform.position;
        int newSiblingIndex = PlaceHolder.transform.parent.childCount;
        for (int i = 0; i < PlaceHolder.transform.parent.childCount; i++) {
            if (targetPos.y > PlaceHolder.transform.parent.GetChild(i).position.y) {
                newSiblingIndex = i;
                if (PlaceHolder.transform.GetSiblingIndex() < newSiblingIndex)
                    newSiblingIndex--;
                break;
            }
        }
        PlaceHolder.transform.SetSiblingIndex(newSiblingIndex);
    }

    public IEnumerator MoveToPlaceHolder()
    {
        while (PlaceHolder != null && Vector3.Distance(this.transform.position, PlaceHolder.position) > 1) {
            float step = MovementSpeed * Time.deltaTime;
            this.transform.position = Vector3.MoveTowards(this.transform.position, PlaceHolder.position, step);
            yield return null;
        }

        if (PlaceHolder == null) {
            Destroy(this.gameObject);
            yield break;
        }

        this.transform.SetParent(PlaceHolder.parent);
        this.transform.SetSiblingIndex(PlaceHolder.GetSiblingIndex());
        PlaceHolder = null;
        CanvasGroup.blocksRaycasts = true;
    }

    public void Highlight()
    {
        Outline.effectColor = Color.green;
        Outline.effectDistance = OutlineHighlightDistance;
    }

    public void UnHighlight()
    {
        Outline.effectColor = Color.black;
        Outline.effectDistance = Vector2.zero;
    }

    public IEnumerator UpdateImage()
    {
        Sprite newSprite = null;
        yield return UnityExtensionMethods.RunOutputCoroutine<Sprite>(UnityExtensionMethods.CreateAndOutputSpriteFromImageFile(RepresentedCard.ImageFilePath, RepresentedCard.ImageWebURL), (output) => newSprite = output);
        if (newSprite != null)
            NewSprite = newSprite;
    }

    void OnDestroy()
    {
        PlaceHolder = null;
        NewSprite = null;
    }

    void OnApplicationQuit()
    {
        PlaceHolder = null;
        NewSprite = null;
    }

    public Card RepresentedCard {
        get {
            if (_representedCard == null)
                _representedCard = new Card(string.Empty, string.Empty, string.Empty, new Dictionary<string, PropertySet>());
            return _representedCard;
        }
        set {
            _representedCard = value;
            if (_representedCard == null)
                _representedCard = new Card(string.Empty, string.Empty, string.Empty, new Dictionary<string, PropertySet>());
            this.gameObject.name = _representedCard.Name + " [" + _representedCard.Id + "]";
            StartCoroutine(UpdateImage());
        }
    }

    public Dictionary<int, CardModel> DraggedClones {
        get {
            if (_draggedClones == null)
                _draggedClones = new Dictionary<int, CardModel>();
            return _draggedClones;
        }
    }

    public RectTransform PlaceHolder {
        get {
            return _placeHolder;
        }
        set {
            if (_placeHolder != null)
                Destroy(_placeHolder.gameObject);
            _placeHolder = value;
        }
    }

    public Outline Outline {
        get {
            if (_outline == null)
                _outline = this.gameObject.GetOrAddComponent<Outline>();
            return _outline;
        }
    }

    public Sprite NewSprite {
        get {
            return _newSprite;
        }
        set {
            if (_newSprite != null) {
                Destroy(Image.sprite.texture);
                Destroy(Image.sprite);
            }
            _newSprite = value;
            Image.sprite = _newSprite;
        }
    }

    public Image Image {
        get {
            if (_image == null)
                _image = GetComponent<Image>();
            return _image;
        }
    }

    public CanvasGroup CanvasGroup {
        get {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
            return _canvasGroup;
        }
    }

    public Canvas Canvas {
        get {
            if (_canvas == null)
                _canvas = this.gameObject.FindInParents<Canvas>();
            return _canvas;
        }
    }
}
