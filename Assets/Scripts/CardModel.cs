using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public delegate void OnDoubleClickDelegate(CardModel cardModel);
public delegate void SecondaryDragDelegate(Vector2 primaryPointerPosition,Vector2 secondaryPointerPosition);

public enum DragPhase
{
    Begin,
    Drag,
    End
}

[RequireComponent(typeof(Image), typeof(CanvasGroup), typeof(LayoutElement))]
public class CardModel : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public const float MovementSpeed = 600f;

    public Vector2 OutlineHighlightDistance {
        get { return new Vector2(10, 10); }
    }

    public bool ClonesOnDrag { get; set; }

    public int PrimaryDragId { get; private set; }

    public Vector2 PrimaryDragOffset { get; private set; }

    public SecondaryDragDelegate SecondaryDragAction { get; set; }

    public Vector2 SecondaryDragPosition { get; private set; }

    public bool SelectedOnDown { get; private set; }

    public OnDoubleClickDelegate DoubleClickEvent { get; set; }

    public PointerEventData RecentPointerEventData { get; set; }

    private Card _representedCard;
    private Dictionary<int, CardModel> _draggedClones;
    private CardStack _placeHolderStack;
    private RectTransform _placeHolder;
    private bool _facedown;
    private Outline _outline;
    private Sprite _newSprite;
    private Image _image;
    private CanvasGroup _canvasGroup;
    private Canvas _canvas;

    public CardModel Clone(Transform parent)
    {
        CardModel clone = Instantiate(this.gameObject, this.transform.position, this.transform.rotation, parent).GetOrAddComponent<CardModel>();
        clone.RepresentedCard = this.RepresentedCard;
        clone.UnHighlight();
        return clone;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // HACK TO SELECT ON DOWN WHEN THE CARD INFO VIEWER IS VISIBLE; CAN'T USE CARDINFOVIEWER.ISVISIBLE SINCE IT IS SET TO FALSE WHEN POINTER DOWN, BEFORE THIS METHOD IS CALLED
        SelectedOnDown = eventData.button != PointerEventData.InputButton.Right && CardInfoViewer.Instance.SelectedCardModel != this && CardInfoViewer.Instance.rectTransform.anchorMax.y < (CardInfoViewer.HiddenYMax + CardInfoViewer.VisibleYMax) / 2.0f;
        if (SelectedOnDown)
            EventSystem.current.SetSelectedGameObject(this.gameObject, eventData);
        
        RecentPointerEventData = eventData;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (RecentPointerEventData == null || RecentPointerEventData.pointerId != eventData.pointerId || eventData.button == PointerEventData.InputButton.Right || DraggedClones.ContainsKey(eventData.pointerId))
            return;
        
        if (!SelectedOnDown && EventSystem.current.currentSelectedGameObject == this.gameObject && DoubleClickEvent != null)
            DoubleClickEvent(this);
        else if (PlaceHolder == null)
            EventSystem.current.SetSelectedGameObject(this.gameObject, eventData);
        
        RecentPointerEventData = eventData;
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!Facedown)
            CardInfoViewer.Instance.SelectedCardModel = this;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        CardInfoViewer.Instance.IsVisible = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        RecentPointerEventData = eventData;

        CardModel cardModel = this;
        if (ClonesOnDrag) {
            DraggedClones [eventData.pointerId] = Clone(Canvas.transform);
            cardModel = DraggedClones [eventData.pointerId];
            cardModel.CanvasGroup.blocksRaycasts = false;
        }
        
        if (cardModel.PrimaryDragId == 0 && eventData.button != PointerEventData.InputButton.Right) {
            cardModel.PrimaryDragId = eventData.pointerId;
            cardModel.PrimaryDragOffset = (((Vector2)cardModel.transform.position) - eventData.position);
            cardModel.UpdatePosition(eventData.position, DragPhase.Begin);
        } else
            SecondaryDragPosition = eventData.position;
        
        EventSystem.current.SetSelectedGameObject(null, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        RecentPointerEventData = eventData;

        CardModel cardModel;
        if (!DraggedClones.TryGetValue(eventData.pointerId, out cardModel))
            cardModel = this;

        if (eventData.pointerId == cardModel.PrimaryDragId)
            cardModel.UpdatePosition(eventData.position, DragPhase.Drag);
        else if (SecondaryDragAction != null)
            SecondaryDragAction(((Vector2)this.transform.position) - cardModel.PrimaryDragOffset, eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        RecentPointerEventData = eventData;

        CardModel cardModel;
        if (!DraggedClones.TryGetValue(eventData.pointerId, out cardModel))
            cardModel = this;
        else
            DraggedClones.Remove(eventData.pointerId);

        if (cardModel.PrimaryDragId != eventData.pointerId)
            return;
        
        cardModel.PrimaryDragId = 0;
        cardModel.PrimaryDragOffset = Vector2.zero;
        cardModel.UpdatePosition(eventData.position, DragPhase.End);
        
        if (cardModel.PlaceHolder != null)
            cardModel.StartCoroutine(cardModel.MoveToPlaceHolder());
        else if (!cardModel.IsInStack)
            Destroy(cardModel.gameObject);
    }

    public void UpdatePosition(Vector2 pointerPosition, DragPhase dragPhase)
    {
        Vector2 targetPosition = pointerPosition + PrimaryDragOffset;
        CardStack cardStack = this.transform.parent.GetComponent<CardStack>();
        if (cardStack != null)
            UpdatePositionInCardStack(targetPosition, cardStack, dragPhase);
        else
            this.transform.position = targetPosition;

        if (PlaceHolderStack != null)
            PlaceHolderStack.UpdateLayout(PlaceHolder, targetPosition);
    }

    public void UpdatePositionInCardStack(Vector2 targetPosition, CardStack cardStack, DragPhase dragPhase)
    {
        this.gameObject.GetOrAddComponent<LayoutElement>().ignoreLayout = false;
        cardStack.UpdateLayout(this.transform as RectTransform, targetPosition);

        if (!cardStack.free && cardStack.container != null) {
            switch (dragPhase) {
                case DragPhase.Begin:
                    cardStack.container.OnBeginDrag(RecentPointerEventData);
                    break;
                case DragPhase.Drag:
                    cardStack.container.OnDrag(RecentPointerEventData);
                    break;
                case DragPhase.End:
                default:
                    cardStack.container.OnEndDrag(RecentPointerEventData);
                    break;
            }
        }

        if (cardStack.type == CardStackType.Full) {
            PlaceHolderStack = cardStack;
            ParentToCanvas();
            this.transform.position = targetPosition;
            // TODO: AREA

        } else if (cardStack.type == CardStackType.Vertical || cardStack.type == CardStackType.Horizontal) {
            RectTransform stackRT = cardStack.transform as RectTransform;
            if (cardStack.free ||
                (cardStack.type == CardStackType.Vertical && (targetPosition.y < (stackRT.position.y + stackRT.offsetMin.y * Canvas.scaleFactor) || targetPosition.y > (stackRT.position.y + stackRT.offsetMax.y * Canvas.scaleFactor))) ||
                (cardStack.type == CardStackType.Horizontal && (targetPosition.x < stackRT.rect.xMin || targetPosition.x > stackRT.rect.xMax))) {
                if (cardStack.container != null)
                    cardStack.container.OnEndDrag(RecentPointerEventData);
                this.gameObject.GetOrAddComponent<LayoutElement>().ignoreLayout = false;
                PlaceHolderStack = null;
                ParentToCanvas();
                this.transform.position = targetPosition;
            } else {
                bool isMovingTop = stackRT.childCount <= 2 || (cardStack.type == CardStackType.Horizontal ? 
                    targetPosition.x > cardStack.transform.GetChild(cardStack.transform.childCount - 1).position.x : targetPosition.y < cardStack.transform.GetChild(cardStack.transform.childCount - 1).position.y);
                if (isMovingTop) {
                    PlaceHolderStack = cardStack;
                    this.gameObject.GetOrAddComponent<LayoutElement>().ignoreLayout = true;
                    this.transform.position = cardStack.type == CardStackType.Horizontal ? new Vector2(targetPosition.x, this.transform.position.y) : new Vector2(this.transform.position.x, targetPosition.y);
                } else {
                    this.gameObject.GetOrAddComponent<LayoutElement>().ignoreLayout = false;
                    PlaceHolderStack = null;
                }
            }
        }
    }

    public void ParentToCanvas()
    {
        this.transform.SetParent(Canvas.transform);
        this.transform.SetAsLastSibling();
        CanvasGroup.blocksRaycasts = false;
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

        this.gameObject.GetOrAddComponent<LayoutElement>().ignoreLayout = false;
        this.transform.SetParent(PlaceHolder.parent);
        this.transform.SetSiblingIndex(PlaceHolder.GetSiblingIndex());
        PlaceHolder = null;
        CanvasGroup.blocksRaycasts = true;
    }

    public void Rotate(Vector2 primaryPosition, Vector2 secondaryPosition)
    {
        Vector2 prevDir = SecondaryDragPosition - primaryPosition;      
        Vector2 currDir = secondaryPosition - primaryPosition;
        float signedAngle = Vector2.SignedAngle(prevDir, currDir);
        this.transform.Rotate(0, 0, signedAngle);
        SecondaryDragPosition = secondaryPosition;
    }

    public static void ResetRotation(CardStack cardStack, CardModel cardModel)
    {
        cardModel.transform.rotation = Quaternion.identity;
    }

    public static void ShowCard(CardStack cardStack, CardModel cardModel)
    {
        cardModel.Facedown = false;
    }

    public static void HideCard(CardStack cardStack, CardModel cardModel)
    {
        cardModel.Facedown = true;
        EventSystem.current.SetSelectedGameObject(null, cardModel.RecentPointerEventData);
    }

    public static void ToggleFacedown(CardModel cardModel)
    {
        cardModel.Facedown = !cardModel.Facedown;
        EventSystem.current.SetSelectedGameObject(null, cardModel.RecentPointerEventData);
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
        else
            Image.sprite = CardGameManager.Current.CardBackImageSprite;
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
                _representedCard = Card.Blank;
            return _representedCard;
        }
        set {
            _representedCard = value;
            if (_representedCard == null)
                _representedCard = Card.Blank;
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

    public CardStack PlaceHolderStack {
        get {
            return _placeHolderStack;
        }
        set {
            _placeHolderStack = value;

            if (_placeHolderStack == null) {
                PlaceHolder = null;
                return;
            }

            GameObject placeholder = new GameObject(this.gameObject.name + "(PlaceHolder)", typeof(RectTransform));
            PlaceHolder = placeholder.transform as RectTransform;
            PlaceHolder.SetParent(_placeHolderStack.transform);
            PlaceHolder.sizeDelta = ((RectTransform)this.transform).sizeDelta;
            PlaceHolder.anchoredPosition = Vector2.zero;
        }
    }

    public RectTransform PlaceHolder {
        get {
            return _placeHolder;
        }
        private set {
            if (_placeHolder != null)
                Destroy(_placeHolder.gameObject);
            _placeHolder = value;
            if (_placeHolder == null)
                _placeHolderStack = null;
        }
    }

    public bool IsInStack {
        get {
            return transform.parent.GetComponent<CardStack>() != null;
        }
    }

    public bool Facedown {
        get {
            return _facedown;
        }
        set {
            _facedown = value;
            if (_facedown)
                Image.sprite = CardGameManager.Current.CardBackImageSprite;
            else if (NewSprite != null)
                Image.sprite = NewSprite;
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
                Destroy(_newSprite.texture);
                Destroy(_newSprite);
            }
            _newSprite = value;
            if (!Facedown)
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
