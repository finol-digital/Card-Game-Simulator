using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Networking;

public delegate void OnDoubleClickDelegate(CardModel cardModel);
public delegate void SecondaryDragDelegate();

public enum DragPhase
{
    Begin,
    Drag,
    End
}

[RequireComponent(typeof(Image), typeof(CanvasGroup), typeof(Outline))]
public class CardModel : NetworkBehaviour, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public const float MovementSpeed = 600f;
    public static readonly Color SelectedHighlightColor = new Color(0.39f, 0.29f, 0.79f);
    public static readonly Vector2 OutlineHighlightDistance = new Vector2(10, 10);

    public bool IsOnline => CGSNetManager.Instance != null && CGSNetManager.Instance.isNetworkActive
        && transform.parent == CGSNetManager.Instance.playController.playAreaContent;
    public bool IsProcessingSecondaryDragAction => PointerPositions.Count > 1 || (CurrentPointerEventData != null && 
        (CurrentPointerEventData.button == PointerEventData.InputButton.Middle || CurrentPointerEventData.button == PointerEventData.InputButton.Right));
    public CardStack ParentCardStack => transform.parent.GetComponent<CardStack>();

    public bool DoesCloneOnDrag { get; set; }
    public OnDoubleClickDelegate DoubleClickAction { get; set; }
    public SecondaryDragDelegate SecondaryDragAction { get; set; }
    public CardDropZone DropTarget { get; set; }

    public bool DidSelectOnDown { get; private set; }
    public PointerEventData CurrentPointerEventData { get; private set; }
    public DragPhase CurrentDragPhase { get; private set; }

    protected Dictionary<int, Vector2> PointerPositions { get; } = new Dictionary<int, Vector2>();
    protected Dictionary<int, Vector2> PointerDragOffsets { get; } = new Dictionary<int, Vector2>();

    [SyncVar(hook ="OnChangeLocalPosition")]
    public Vector2 localPosition;

    [SyncVar(hook ="OnChangeRotation")]
    public Quaternion rotation;

    [SyncVar]
    private string _id;
    public string Id => _id;
    public Card Value {
        get {
            Card cardValue;
            if (string.IsNullOrEmpty(_id) || !CardGameManager.Current.Cards.TryGetValue(_id, out cardValue))
                return Card.Blank;
            return cardValue;
        }
        set {
            _id = value != null ? value.Id : string.Empty;
            gameObject.name = value != null ? "[" + value.Id + "] " + value.Name : string.Empty;
            CardGameManager.Current.PutCardImage(this);
        }
    }

    [SyncVar]
    private bool _isFacedown;
    public bool IsFacedown {
        get { return _isFacedown; }
        set {
            if (value == _isFacedown)
                return;
            _isFacedown = value;
            if (_isFacedown) {
                HideNameLabel();
                image.sprite = CardGameManager.Current.CardBackImageSprite;
            } else
                CardGameManager.Current.PutCardImage(this);

            if (IsOnline && hasAuthority)
                CmdUpdateIsFacedown(_isFacedown);
        }
    }

    private Image _image;
    public Image image => _image ?? (_image = GetComponent<Image>());
    public bool HasImage => image.sprite != CardGameManager.Current.CardBackImageSprite;

    private Outline _outline;
    private Text _nameText;

    private RectTransform _placeHolder;
    private CardStack _placeHolderCardStack;

    void Start()
    {
        ((RectTransform)transform).sizeDelta = CardGameManager.PixelsPerInch * CardGameManager.Current.CardSize;

        if (IsOnline) {
            if (Vector2.zero != localPosition)
                OnChangeLocalPosition(localPosition);
            if (Quaternion.identity != rotation)
                OnChangeRotation(rotation);
        }

        if (!IsFacedown)
            CardGameManager.Current.PutCardImage(this);
        else
            image.sprite = CardGameManager.Current.CardBackImageSprite;

        _outline = GetComponent<Outline>();
        _nameText = GetComponentInChildren<Text>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        DidSelectOnDown = eventData.button != PointerEventData.InputButton.Middle && eventData.button != PointerEventData.InputButton.Right
            && CardInfoViewer.Instance.SelectedCardModel != this && CardInfoViewer.Instance.WasVisible;
        if (DidSelectOnDown)
            EventSystem.current.SetSelectedGameObject(gameObject, eventData);

        CurrentPointerEventData = eventData;

        PointerPositions[eventData.pointerId] = eventData.position;
        PointerDragOffsets[eventData.pointerId] = ((Vector2)transform.position) - eventData.position;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (CurrentPointerEventData != null && CurrentPointerEventData.pointerId == eventData.pointerId && !eventData.dragging
            && eventData.button != PointerEventData.InputButton.Middle && eventData.button != PointerEventData.InputButton.Right) {
            if (!DidSelectOnDown && EventSystem.current.currentSelectedGameObject == gameObject && DoubleClickAction != null)
                DoubleClickAction(this);
            else if (PlaceHolder == null)
                EventSystem.current.SetSelectedGameObject(gameObject, eventData);
        }

        CurrentPointerEventData = eventData;
        if (CurrentDragPhase == DragPhase.Drag)
            return;

        PointerPositions.Remove(eventData.pointerId);
        PointerDragOffsets.Remove(eventData.pointerId);
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!IsFacedown)
            CardInfoViewer.Instance.SelectedCardModel = this;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        CardInfoViewer.Instance.IsVisible = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (IsOnline && !hasAuthority)
            return;

        if (DoesCloneOnDrag) {
            GameObject newGameObject = Instantiate(gameObject, transform.position, transform.rotation, gameObject.FindInParents<Canvas>().transform);
            eventData.pointerPress = newGameObject;
            eventData.pointerDrag = newGameObject;
            newGameObject.GetComponent<CanvasGroup>().blocksRaycasts = false;
            CardModel cardModel = newGameObject.GetOrAddComponent<CardModel>();
            cardModel.HideHighlight();
            cardModel.Value = Value;
            cardModel.DoesCloneOnDrag = false;
            cardModel.PointerDragOffsets[eventData.pointerId] = (Vector2)transform.position - eventData.position;
            cardModel.OnBeginDrag(eventData);
            return;
        }

        EventSystem.current.SetSelectedGameObject(null, eventData);
        CurrentPointerEventData = eventData;
        CurrentDragPhase = DragPhase.Begin;
        PointerPositions[eventData.pointerId] = eventData.position;

        UpdatePosition();
        if (SecondaryDragAction != null && IsProcessingSecondaryDragAction)
            SecondaryDragAction();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (IsOnline && !hasAuthority)
            return;
            
        CurrentPointerEventData = eventData;
        CurrentDragPhase = DragPhase.Drag;
        PointerPositions[eventData.pointerId] = eventData.position;

        UpdatePosition();
        if (SecondaryDragAction != null && IsProcessingSecondaryDragAction)
            SecondaryDragAction();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (IsOnline && !hasAuthority)
            return;

        CurrentPointerEventData = eventData;
        CurrentDragPhase = DragPhase.End;

        UpdatePosition();
        if (SecondaryDragAction != null && IsProcessingSecondaryDragAction)
            SecondaryDragAction();

        Vector2 removedOffset = Vector2.zero;
        Vector2 pointerDragOffset;
        if (PointerDragOffsets.TryGetValue(eventData.pointerId, out pointerDragOffset))
            removedOffset = (Vector2)transform.position - eventData.position - pointerDragOffset;
        PointerPositions.Remove(eventData.pointerId);
        PointerDragOffsets.Remove(eventData.pointerId);
        Vector2 otherOffset;
        foreach (int offsetKey in PointerDragOffsets.Keys.ToList())
            if (PointerDragOffsets.TryGetValue(offsetKey, out otherOffset))
                PointerDragOffsets[offsetKey] = otherOffset - removedOffset;

        if (IsProcessingSecondaryDragAction)
            return;

        if (PlaceHolder != null)
            StartCoroutine(MoveToPlaceHolder());
        else if (ParentCardStack == null)
            Discard();
    }

    public static CardModel GetPointerDrag(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
            return null;
        return eventData.pointerDrag.GetComponent<CardModel>();
    }

    public void UpdatePosition()
    {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        if (SecondaryDragAction != Rotate && IsProcessingSecondaryDragAction)
            return;
#else
        if (Input.GetMouseButton(1) || Input.GetMouseButtonUp(1) || Input.GetMouseButton(2) || Input.GetMouseButtonUp(2))
            return;
#endif
        if (PointerPositions.Count < 1 || PointerDragOffsets.Count < 1 || (IsOnline && !hasAuthority))
            return;

        Vector2 targetPosition = UnityExtensionMethods.CalculateMean(PointerPositions.Values.ToList());
        targetPosition = targetPosition + UnityExtensionMethods.CalculateMean(PointerDragOffsets.Values.ToList());
        if (ParentCardStack != null)
            UpdateCardStackPosition(targetPosition);
        else
            transform.position = targetPosition;

        if (PlaceHolderCardStack != null)
            PlaceHolderCardStack.UpdateLayout(PlaceHolder, targetPosition);

        if (IsOnline)
            localPosition = transform.localPosition;
    }

    public void UpdateCardStackPosition(Vector2 targetPosition)
    {
        CardStack cardStack = ParentCardStack;
        if (cardStack == null || (IsOnline && !hasAuthority))
            return;

        if (!cardStack.DoesImmediatelyRelease && (cardStack.type == CardStackType.Vertical || cardStack.type == CardStackType.Horizontal))
            cardStack.UpdateScrollRect(CurrentDragPhase, CurrentPointerEventData);
        else
            cardStack.UpdateLayout(transform as RectTransform, targetPosition);

        if (cardStack.type == CardStackType.Area)
            transform.SetAsLastSibling();

        Vector3[] stackCorners = new Vector3[4];
        ((RectTransform)cardStack.transform).GetWorldCorners(stackCorners);
        bool isOutYBounds = targetPosition.y < stackCorners[0].y || targetPosition.y > stackCorners[1].y;
        bool isOutXBounds = targetPosition.x < stackCorners[0].x || targetPosition.y > stackCorners[2].x;
        if ((cardStack.DoesImmediatelyRelease && !IsProcessingSecondaryDragAction)
            || (cardStack.type == CardStackType.Full && CurrentDragPhase == DragPhase.Begin)
            || (cardStack.type == CardStackType.Vertical && isOutXBounds)
            || (cardStack.type == CardStackType.Horizontal && isOutYBounds)
            || (cardStack.type == CardStackType.Area 
                && (isOutYBounds || (PlaceHolder != null && PlaceHolder.parent != transform.parent))))
            ParentToCanvas(targetPosition);
    }

    public void OnChangeLocalPosition(Vector2 localPosition)
    {
        if (IsOnline && !hasAuthority)
            transform.localPosition = localPosition;
    }

    public void ParentToCanvas(Vector3 targetPosition)
    {
        if (IsOnline && hasAuthority)
            CmdUnspawnCard();
        CardStack prevParentStack = ParentCardStack;
        if (CurrentDragPhase == DragPhase.Drag)
            prevParentStack.UpdateScrollRect(DragPhase.End, CurrentPointerEventData);
        transform.SetParent(CardGameManager.TopCardCanvas.transform);
        transform.SetAsLastSibling();
        if (prevParentStack != null)
            prevParentStack.OnRemove(this);
        GetComponent<CanvasGroup>().blocksRaycasts = false;
        transform.position = targetPosition;
        transform.localScale = Vector3.one;
    }

    [Command]
    void CmdUnspawnCard()
    {
        RpcUnspawnCard();
    }

    [ClientRpc]
    public void RpcUnspawnCard()
    {
        if (!isServer && !hasAuthority)
            Discard();
        else if (isServer)
            NetworkServer.UnSpawn(gameObject);
    }

    public IEnumerator MoveToPlaceHolder()
    {
        while (PlaceHolder != null && Vector3.Distance(transform.position, PlaceHolder.position) > 1) {
            float step = MovementSpeed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, PlaceHolder.position, step);
            yield return null;
        }

        if (PlaceHolder == null) {
            Discard();
            yield break;
        }

        CardStack prevParentStack = ParentCardStack;
        transform.SetParent(PlaceHolder.parent);
        transform.SetSiblingIndex(PlaceHolder.GetSiblingIndex());
        if (prevParentStack != null)
            prevParentStack.OnRemove(this);
        if (ParentCardStack != null)
            ParentCardStack.OnAdd(this);
        PlaceHolder = null;
        GetComponent<CanvasGroup>().blocksRaycasts = true;
    }

    public void UpdateParentCardStackScrollRect()
    {
        CardStack cardStack = ParentCardStack;
        if (cardStack != null)
            cardStack.UpdateScrollRect(CurrentDragPhase, CurrentPointerEventData);
    }

    public void Rotate()
    {
        Vector2 referencePoint = transform.position;
        foreach (KeyValuePair<int, Vector2> pointerDragPosition in PointerPositions)
            if (pointerDragPosition.Key != CurrentPointerEventData.pointerId)
                referencePoint = pointerDragPosition.Value;
        Vector2 prevDir = (CurrentPointerEventData.position - CurrentPointerEventData.delta) - referencePoint;
        Vector2 currDir = CurrentPointerEventData.position - referencePoint;
        transform.Rotate(0, 0, Vector2.SignedAngle(prevDir, currDir));

        if (IsOnline)
            rotation = transform.rotation;
    }

    public void OnChangeRotation(Quaternion rotation)
    {
        if (IsOnline && !hasAuthority)
            transform.rotation = rotation;
    }

    public static void ResetRotation(CardStack cardStack, CardModel cardModel)
    {
        if (cardModel == null || (cardModel.IsOnline && !cardModel.hasAuthority))
            return;

        cardModel.transform.rotation = Quaternion.identity;
        if (cardModel.IsOnline)
            cardModel.rotation = cardModel.transform.rotation;
    }

    public static void ShowCard(CardStack cardStack, CardModel cardModel)
    {
        if (cardModel == null || (cardModel.IsOnline && !cardModel.hasAuthority))
            return;

        cardModel.IsFacedown = false;
    }

    public static void HideCard(CardStack cardStack, CardModel cardModel)
    {
        if (cardModel == null || (cardModel.IsOnline && !cardModel.hasAuthority))
            return;

        cardModel.IsFacedown = true;
        EventSystem.current.SetSelectedGameObject(null, cardModel.CurrentPointerEventData);
    }

    public static void ToggleFacedown(CardModel cardModel)
    {
        if (cardModel == null || (cardModel.IsOnline && !cardModel.hasAuthority))
            return;

        cardModel.IsFacedown = !cardModel.IsFacedown;
        EventSystem.current.SetSelectedGameObject(null, cardModel.CurrentPointerEventData);
    }

    [Command]
    void CmdUpdateIsFacedown(bool isFacedown)
    {
        RpcUpdateIsFacedown(isFacedown);
    }

    [ClientRpc]
    void RpcUpdateIsFacedown(bool isFacedown)
    {
        if (!hasAuthority)
            IsFacedown = isFacedown;
    }

    public void ShowHighlight()
    {
        if (_outline == null)
            _outline = GetComponent<Outline>();
        _outline.effectColor = SelectedHighlightColor;
        _outline.effectDistance = OutlineHighlightDistance;
    }

    public void WarnHighlight()
    {
        if (_outline == null)
            _outline = GetComponent<Outline>();
        _outline.effectColor = Color.red;
        _outline.effectDistance = OutlineHighlightDistance;
    }

    public void HideHighlight()
    {
        if (_outline == null)
            _outline = GetComponent<Outline>();
        bool isOthers = IsOnline && !hasAuthority;
        _outline.effectColor = isOthers ? Color.yellow : Color.black;
        _outline.effectDistance = isOthers ? OutlineHighlightDistance : Vector2.zero;
    }

    [ClientRpc]
    public void RpcHideHighlight()
    {
        HideHighlight();
    }

    public void ShowNameLabel()
    {
        transform.GetChild(0).gameObject.SetActive(true);
        if (_nameText == null)
            _nameText = GetComponentInChildren<Text>();
        _nameText.text = Value.Name;
    }

    public void HideNameLabel()
    {
        transform.GetChild(0).gameObject.SetActive(false);
    }

    public void Discard()
    {
        if (DropTarget == null && CardGameManager.Current.GameCatchesDiscard && CGSNetManager.Instance != null)
            CGSNetManager.Instance.playController.CatchDiscard(Value);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (CardGameManager.IsQuitting)
            return;

        CardGameManager.Current.RemoveCardImage(this);
        if (PlaceHolder != null)
            Destroy(PlaceHolder.gameObject);
    }

    public RectTransform PlaceHolder {
        get { return _placeHolder; }
        private set {
            if (_placeHolder != null)
                Destroy(_placeHolder.gameObject);
            _placeHolder = value;
            if (_placeHolder == null) {
                if (ParentCardStack == null && DropTarget == null)
                    WarnHighlight();
                _placeHolderCardStack = null;
            } else
                HideHighlight();
        }
    }

    public CardStack PlaceHolderCardStack {
        get { return _placeHolderCardStack; }
        set {
            _placeHolderCardStack = value;

            if (_placeHolderCardStack == null) {
                PlaceHolder = null;
                return;
            }
            DropTarget = null;

            GameObject placeholder = new GameObject(gameObject.name + "(PlaceHolder)", typeof(RectTransform));
            PlaceHolder = (RectTransform)placeholder.transform;
            PlaceHolder.SetParent(_placeHolderCardStack.transform);
            PlaceHolder.sizeDelta = ((RectTransform)transform).sizeDelta;
            PlaceHolder.anchoredPosition = Vector2.zero;
        }
    }
}
