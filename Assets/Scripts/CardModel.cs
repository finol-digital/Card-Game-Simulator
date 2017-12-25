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
    public const float AlphaHitTestMinimumThreshold = 0.1f;
    public static readonly Vector2 OutlineHighlightDistance = new Vector2(10, 10);

    public bool IsOnline => NetworkManager.singleton != null && NetworkManager.singleton.isNetworkActive
        && transform.parent == ((LocalNetManager)NetworkManager.singleton).playController.playAreaContent;
    public bool IsProcessingSecondaryDragAction => PointerPositions.Count > 1
        || (CurrentPointerEventData != null && CurrentPointerEventData.button == PointerEventData.InputButton.Right);
    public CardStack ParentCardStack => transform.parent.GetComponent<CardStack>();

    public Dictionary<int, CardModel> DraggedClones { get; } = new Dictionary<int, CardModel>();
    public Dictionary<int, Vector2> PointerPositions { get; } = new Dictionary<int, Vector2>();
    public Dictionary<int, Vector2> PointerDragOffsets { get; } = new Dictionary<int, Vector2>();

    public bool DoesCloneOnDrag { get; set; }
    public OnDoubleClickDelegate DoubleClickAction { get; set; }
    public SecondaryDragDelegate SecondaryDragAction { get; set; }
    public CardDropZone DropTarget { get; set; }

    public bool DidSelectOnDown { get; private set; }
    public PointerEventData CurrentPointerEventData { get; private set; }
    public DragPhase CurrentDragPhase { get; private set; }

    [SyncVar]
    public Vector2 LocalPosition;

    [SyncVar]
    public Quaternion Rotation;

    [SyncVar]
    private string _id;
    public string Id => _id;

    [SyncVar]
    private bool _isFacedown;

    private RectTransform _placeHolder;
    private CardStack _placeHolderCardStack;

    void Start()
    {
        GetComponent<RectTransform>().sizeDelta = CardGameManager.Current.CardSize * CardGameManager.PixelsPerInch;
        if (!IsFacedown)
            CardGameManager.Current.PutCardImage(this);
        else
            GetComponent<Image>().sprite = CardGameManager.Current.CardBackImageSprite;
        GetComponent<Image>().alphaHitTestMinimumThreshold = AlphaHitTestMinimumThreshold;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // HACK TO SELECT ON DOWN WHEN THE CARD INFO VIEWER IS VISIBLE; CAN'T USE CARDINFOVIEWER.ISVISIBLE SINCE IT IS SET TO FALSE WHEN POINTER DOWN, BEFORE THIS METHOD IS CALLED
        DidSelectOnDown = eventData.button != PointerEventData.InputButton.Right && CardInfoViewer.Instance.SelectedCardModel != this && CardInfoViewer.Instance.infoPanel.anchorMax.y < (CardInfoViewer.HiddenYMax + CardInfoViewer.VisibleYMax) / 2.0f;
        if (DidSelectOnDown)
            EventSystem.current.SetSelectedGameObject(gameObject, eventData);

        CurrentPointerEventData = eventData;

        PointerPositions [eventData.pointerId] = eventData.position;
        PointerDragOffsets [eventData.pointerId] = ((Vector2)transform.position) - eventData.position;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (CurrentPointerEventData != null && CurrentPointerEventData.pointerId == eventData.pointerId && eventData.button != PointerEventData.InputButton.Right && !eventData.dragging && !DraggedClones.ContainsKey(eventData.pointerId)) {
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

        EventSystem.current.SetSelectedGameObject(null, eventData);

        CardModel cardModel = this;
        if (DoesCloneOnDrag) {
            PointerPositions.Remove(eventData.pointerId);
            PointerDragOffsets.Remove(eventData.pointerId);
            DraggedClones [eventData.pointerId] = Instantiate(gameObject, transform.position, transform.rotation, gameObject.FindInParents<Canvas>().transform).GetOrAddComponent<CardModel>();
            cardModel = DraggedClones [eventData.pointerId];
            cardModel.HideHighlight();
            cardModel.Value = Value;
            cardModel.PointerDragOffsets[eventData.pointerId] = ((Vector2)transform.position) - eventData.position;
            cardModel.GetComponent<CanvasGroup>().blocksRaycasts = false;
        }

        cardModel.CurrentPointerEventData = eventData;
        cardModel.CurrentDragPhase = DragPhase.Begin;
        cardModel.PointerPositions [eventData.pointerId] = eventData.position;

        cardModel.UpdatePosition();
        if (cardModel.SecondaryDragAction != null && cardModel.IsProcessingSecondaryDragAction)
            cardModel.SecondaryDragAction();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (IsOnline && !hasAuthority)
            return;

        CardModel cardModel;
        if (!DraggedClones.TryGetValue(eventData.pointerId, out cardModel))
            cardModel = this;

        cardModel.CurrentPointerEventData = eventData;
        cardModel.CurrentDragPhase = DragPhase.Drag;
        cardModel.PointerPositions [eventData.pointerId] = eventData.position;

        cardModel.UpdatePosition();
        if (cardModel.SecondaryDragAction != null && cardModel.IsProcessingSecondaryDragAction)
            cardModel.SecondaryDragAction();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (IsOnline && !hasAuthority)
            return;

        CardModel cardModel;
        if (!DraggedClones.TryGetValue(eventData.pointerId, out cardModel))
            cardModel = this;
        else
            DraggedClones.Remove(eventData.pointerId);

        cardModel.CurrentPointerEventData = eventData;
        cardModel.CurrentDragPhase = DragPhase.End;

        cardModel.UpdatePosition();
        if (cardModel.SecondaryDragAction != null && cardModel.IsProcessingSecondaryDragAction)
            cardModel.SecondaryDragAction();

        cardModel.PointerPositions.Remove(eventData.pointerId);
        cardModel.PointerDragOffsets.Remove(eventData.pointerId);

        if (cardModel.IsProcessingSecondaryDragAction)
            return;

        if (cardModel.PlaceHolder != null)
            cardModel.StartCoroutine(cardModel.MoveToPlaceHolder());
        else if (cardModel.ParentCardStack == null)
            Destroy(cardModel.gameObject);
    }

    public static CardModel GetPointerDrag(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
            return null;

        CardModel cardModel = eventData.pointerDrag.GetComponent<CardModel>();
        if (cardModel == null)
            return null;

        CardModel draggedCardModel;
        if (cardModel.DraggedClones.TryGetValue(eventData.pointerId, out draggedCardModel))
            cardModel = draggedCardModel;
        return cardModel;
    }

    public void UpdatePosition()
    {
#if (!UNITY_ANDROID && !UNITY_IOS) || UNITY_EDITOR
        if (Input.GetMouseButton(1) || Input.GetMouseButtonUp(1))
            return;
#endif
        bool isOnline = IsOnline;
        if (PointerPositions.Count < 1 || PointerDragOffsets.Count < 1 || (isOnline && !hasAuthority))
            return;

        Vector2 targetPosition = UnityExtensionMethods.CalculateMean(PointerPositions.Values.ToList()) + UnityExtensionMethods.CalculateMean(PointerDragOffsets.Values.ToList());
        if (ParentCardStack != null)
            UpdateCardStackPosition(targetPosition);
        else
            transform.position = targetPosition;

        if (PlaceHolderCardStack != null)
            PlaceHolderCardStack.UpdateLayout(PlaceHolder, targetPosition);

        LocalPosition = transform.localPosition;
        if (isOnline)
            CmdUpdateLocalPosition(LocalPosition);
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

        Vector3[] stackCorners = new Vector3[4];
        ((RectTransform) cardStack.transform).GetWorldCorners(stackCorners);
        bool isOutYBounds = targetPosition.y < stackCorners [0].y || targetPosition.y > stackCorners [1].y;
        bool isOutXBounds = targetPosition.x < stackCorners [0].x || targetPosition.y > stackCorners [2].x;
        if ((cardStack.DoesImmediatelyRelease && !IsProcessingSecondaryDragAction)
            || (cardStack.type == CardStackType.Full && CurrentDragPhase == DragPhase.Begin)
            || (cardStack.type == CardStackType.Vertical && isOutXBounds)
            || ((cardStack.type == CardStackType.Horizontal || cardStack.type == CardStackType.Area) && isOutYBounds))
            ParentToCanvas(targetPosition);
    }

    [Command]
    void CmdUpdateLocalPosition(Vector2 localPosition)
    {
        RpcUpdateLocalPosition(localPosition);
    }

    [ClientRpc]
    void RpcUpdateLocalPosition(Vector2 localPosition)
    {
        if (!hasAuthority)
            transform.localPosition = localPosition;
    }

    public void ParentToCanvas(Vector3 targetPosition)
    {
        if (IsOnline && hasAuthority)
            CmdUnspawnCard();
        CardStack prevParentStack = ParentCardStack;
        transform.SetParent(CardGameManager.Instance.TopCanvas.transform);
        transform.SetAsLastSibling();
        if (prevParentStack != null)
            prevParentStack.OnRemove(this);
        GetComponent<CanvasGroup>().blocksRaycasts = false;
        transform.position = targetPosition;
    }

    [Command]
    void CmdUnspawnCard()
    {
        RpcUnspawn();
        NetworkServer.UnSpawn(gameObject);
        ((LocalNetManager)NetworkManager.singleton).UnSpawnCard(gameObject);
    }

    [ClientRpc]
    public void RpcUnspawn()
    {
        if (!isServer && !hasAuthority)
            Destroy(gameObject);
    }

    public IEnumerator MoveToPlaceHolder()
    {
        while (PlaceHolder != null && Vector3.Distance(transform.position, PlaceHolder.position) > 1) {
            float step = MovementSpeed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, PlaceHolder.position, step);
            yield return null;
        }

        if (PlaceHolder == null) {
            Destroy(gameObject);
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

        Rotation = transform.rotation;
        if (IsOnline)
            CmdUpdateRotation(Rotation);
    }

    [Command]
    void CmdUpdateRotation(Quaternion rotation)
    {
        RpcUpdateRotation(rotation);
    }

    [ClientRpc]
    void RpcUpdateRotation(Quaternion rotation)
    {
        if (!hasAuthority)
            transform.rotation = rotation;
    }

    public static void ResetRotation(CardStack cardStack, CardModel cardModel)
    {
        if (cardModel == null || (cardModel.IsOnline && !cardModel.hasAuthority))
            return;

        cardModel.transform.rotation = Quaternion.identity;
        cardModel.Rotation = cardModel.transform.rotation;
        if (cardModel.IsOnline)
            cardModel.CmdUpdateRotation(cardModel.Rotation);
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
        GetComponent<Outline>().effectColor = Color.green;
        GetComponent<Outline>().effectDistance = OutlineHighlightDistance;
    }

    public void WarnHighlight()
    {
        GetComponent<Outline>().effectColor = Color.red;
        GetComponent<Outline>().effectDistance = OutlineHighlightDistance;
    }

    public void HideHighlight()
    {
        bool isOthers = IsOnline && !hasAuthority;
        GetComponent<Outline>().effectColor = isOthers ? Color.yellow : Color.black;
        GetComponent<Outline>().effectDistance = isOthers ? OutlineHighlightDistance : Vector2.zero;
    }

    [ClientRpc]
    public void RpcHideHighlight()
    {
        HideHighlight();
    }

    void OnDestroy()
    {
        if (CardGameManager.IsQuitting)
            return;

        CardGameManager.Current.RemoveCardImage(this);
        if (PlaceHolder != null)
            Destroy(PlaceHolder.gameObject);
    }

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

    public bool IsFacedown {
        get { return _isFacedown; }
        set {
            if (value == _isFacedown)
                return;
            _isFacedown = value;
            if (_isFacedown)
                GetComponent<Image>().sprite = CardGameManager.Current.CardBackImageSprite;
            else
                CardGameManager.Current.PutCardImage(this);
            if (IsOnline && hasAuthority)
                CmdUpdateIsFacedown(_isFacedown);
        }
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
