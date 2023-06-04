/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using Cgs.CardGameView.Viewer;
using Cgs.Menu;
using Cgs.Play;
using Cgs.Play.Multiplayer;
using JetBrains.Annotations;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.CardGameView.Multiplayer
{
    public enum DragPhase
    {
        Begin,
        Drag,
        End
    }

    public enum HighlightMode
    {
        Off,
        Selected,
        Authorized,
        Unauthorized,
        Warn
    }

    public class CgsNetPlayable : NetworkBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler,
        IPointerUpHandler, ISelectHandler, IDeselectHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private static readonly Vector2 OutlineHighlightDistance = new(15, 15);
        private static readonly Color SelectedHighlightColor = new(0.02f, 0.5f, 0.4f);

        private const float DisownTime = 5.0f;

        public CardZone ParentCardZone => transform.parent != null ? transform.parent.GetComponent<CardZone>() : null;

        public bool IsOnline => PlayController.Instance != null && PlayController.Instance.playMat != null &&
                                PlayController.Instance.playMat.transform == transform.parent && IsSpawned;

        public bool LacksOwnership => NetworkManager.Singleton.IsConnectedClient && !IsOwner;

        public NetworkObject MyNetworkObject => _networkObject ??= GetComponent<NetworkObject>();

        private NetworkObject _networkObject;

        protected bool IsProcessingSecondaryDragAction => PointerDragOffsets.Count > 1 || CurrentPointerEventData is
            {button: PointerEventData.InputButton.Middle or PointerEventData.InputButton.Right};

        public Vector2 Position
        {
            get => IsOnline ? _positionNetworkVariable.Value : _position;
            set
            {
                _position = value;
                if (IsOnline)
                    _positionNetworkVariable.Value = value;
            }
        }

        private Vector2 _position;
        private readonly NetworkVariable<Vector2> _positionNetworkVariable = new();

        public Quaternion Rotation
        {
            get => IsOnline ? _rotationNetworkVariable.Value : _rotation;
            set
            {
                _rotation = value;
                if (IsOnline)
                    _rotationNetworkVariable.Value = value;
            }
        }

        private Quaternion _rotation;
        private readonly NetworkVariable<Quaternion> _rotationNetworkVariable = new();

        public PointerEventData CurrentPointerEventData { get; protected set; }
        public Dictionary<int, Vector2> PointerPositions { get; } = new();
        protected Dictionary<int, Vector2> PointerDragOffsets { get; } = new();

        protected bool DidSelectOnDown { get; set; }
        protected bool DidDrag { get; set; }
        protected DragPhase CurrentDragPhase { get; private set; }
        protected float HoldTime { get; private set; }

        private float _disownedTime;
        private Vector2 _previousPosition;

        public bool ToDiscard { get; protected set; }

        public virtual string ViewValue => "<Playable:Value>";

        public HighlightMode HighlightMode
        {
            get => _highlightMode;
            set
            {
                _highlightMode = value;
                switch (_highlightMode)
                {
                    case HighlightMode.Selected:
                        Highlight.effectColor = SelectedHighlightColor;
                        Highlight.effectDistance = OutlineHighlightDistance;
                        break;
                    case HighlightMode.Authorized:
                        Highlight.effectColor = Color.green;
                        Highlight.effectDistance = OutlineHighlightDistance;
                        break;
                    case HighlightMode.Unauthorized:
                        Highlight.effectColor = Color.yellow;
                        Highlight.effectDistance = OutlineHighlightDistance;
                        break;
                    case HighlightMode.Warn:
                        Highlight.effectColor = Color.red;
                        Highlight.effectDistance = OutlineHighlightDistance;
                        break;
                    default:
                    case HighlightMode.Off:
                        var isOthers = IsOnline && !IsOwner;
                        Highlight.effectColor = isOthers ? Color.yellow : Color.black;
                        Highlight.effectDistance = isOthers ? OutlineHighlightDistance : Vector2.zero;
                        break;
                }
            }
        }

        private HighlightMode _highlightMode = HighlightMode.Off;

        private Outline Highlight => _highlight ??= gameObject.GetOrAddComponent<Outline>();
        private Outline _highlight;

        private void Awake()
        {
            _positionNetworkVariable.OnValueChanged += OnChangePosition;
            _rotationNetworkVariable.OnValueChanged += OnChangeRotation;

            OnAwakePlayable();
        }

        protected virtual void OnAwakePlayable()
        {
            // Child classes may override
        }

        protected virtual void OnStartPlayable()
        {
            // Child classes may override
        }

        private void Start()
        {
            OnStartPlayable();

            if (!IsOnline)
                return;

            if (Vector2.zero != Position)
                ((RectTransform) transform).localPosition = Position;
            if (Quaternion.identity != Rotation)
                transform.rotation = Rotation;
        }

        private void Update()
        {
            if (PointerPositions.Count > 0 && !DidDrag)
                HoldTime += Time.deltaTime;
            else
                HoldTime = 0;

            if (IsOnline && IsServer && !IsOwner)
            {
                if (_previousPosition == Position)
                    _disownedTime += Time.deltaTime;
                else
                    _disownedTime = 0;

                _previousPosition = Position;

                if (_disownedTime > DisownTime)
                    MyNetworkObject.RemoveOwnership();
            }

            OnUpdatePlayable();
        }

        protected virtual void OnUpdatePlayable()
        {
            // Child classes may override
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            OnPointerEnterPlayable(eventData);
        }

        protected virtual void OnPointerEnterPlayable(PointerEventData eventData)
        {
            if (Settings.PreviewOnMouseOver && CardViewer.Instance != null && !CardViewer.Instance.IsVisible
                && PlayableViewer.Instance != null && !PlayableViewer.Instance.IsVisible)
                PlayableViewer.Instance.Preview(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            OnPointerExitPlayable(eventData);
        }

        protected virtual void OnPointerExitPlayable(PointerEventData eventData)
        {
            if (PlayableViewer.Instance != null)
                PlayableViewer.Instance.HidePreview();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            CurrentPointerEventData = eventData;
            PointerPositions[eventData.pointerId] = eventData.position;
            PointerDragOffsets[eventData.pointerId] = (Vector2) transform.position - eventData.position;

            OnPointerDownPlayable(eventData);
        }

        protected virtual void OnPointerDownPlayable(PointerEventData eventData)
        {
            // Child classes may override
        }

        protected virtual void OnPointerUpSelectPlayable(PointerEventData eventData)
        {
            if (CurrentDragPhase != DragPhase.Drag && !EventSystem.current.alreadySelecting)
                EventSystem.current.SetSelectedGameObject(gameObject, eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            OnPointerUpSelectPlayable(eventData);

            CurrentPointerEventData = eventData;

            if (CurrentDragPhase == DragPhase.Drag)
                return;

            PointerPositions.Remove(eventData.pointerId);
            PointerDragOffsets.Remove(eventData.pointerId);
            if (DidDrag && PointerDragOffsets.Count == 0)
                DidDrag = false;
        }

        public void OnSelect(BaseEventData eventData)
        {
            OnSelectPlayable(eventData);
        }

        protected virtual void OnSelectPlayable(BaseEventData eventData)
        {
            if (PlayableViewer.Instance != null)
                PlayableViewer.Instance.SelectedPlayable = this;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            OnDeselectPlayable(eventData);
        }

        protected virtual void OnDeselectPlayable(BaseEventData eventData)
        {
            if (PlayableViewer.Instance != null)
                PlayableViewer.Instance.IsVisible = false;
        }

        protected virtual bool PreBeginDrag(PointerEventData eventData)
        {
            return false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (PreBeginDrag(eventData))
                return;

            CurrentPointerEventData = eventData;
            CurrentDragPhase = DragPhase.Begin;
            PointerPositions[eventData.pointerId] = eventData.position;

            OnBeginDragPlayable(eventData);
        }

        protected virtual void OnBeginDragPlayable(PointerEventData eventData)
        {
            if (IsOnline)
                RequestChangeOwnership();
        }

        public void OnDrag(PointerEventData eventData)
        {
            CurrentPointerEventData = eventData;
            CurrentDragPhase = DragPhase.Drag;
            PointerPositions[eventData.pointerId] = eventData.position;

            OnDragPlayable(eventData);
        }

        protected virtual void OnDragPlayable(PointerEventData eventData)
        {
            if (LacksOwnership)
                RequestChangeOwnership();
            else
                UpdatePosition();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            CurrentPointerEventData = eventData;
            CurrentDragPhase = DragPhase.End;

            OnEndDragPlayable(eventData);

            RemovePointer(eventData);

            PostDragPlayable(eventData);

            if (IsOnline && IsOwner && !ToDiscard)
                RemoveOwnershipServerRpc();
        }

        protected virtual void OnEndDragPlayable(PointerEventData eventData)
        {
            if (!LacksOwnership)
                UpdatePosition();
        }

        protected virtual void PostDragPlayable(PointerEventData eventData)
        {
            if (ParentCardZone != null && ParentCardZone.type == CardZoneType.Area)
                SnapToGrid();
        }

        protected void RemovePointer(PointerEventData eventData)
        {
            var removedOffset = Vector2.zero;
            if (PointerDragOffsets.TryGetValue(eventData.pointerId, out var pointerDragOffset))
                removedOffset = (Vector2) transform.position - eventData.position - pointerDragOffset;
            PointerPositions.Remove(eventData.pointerId);
            PointerDragOffsets.Remove(eventData.pointerId);
            foreach (var offsetKey in PointerDragOffsets.Keys.ToList())
                if (PointerDragOffsets.TryGetValue(offsetKey, out var otherOffset))
                    PointerDragOffsets[offsetKey] = otherOffset - removedOffset;
        }

        protected virtual void UpdatePosition()
        {
            if (ParentCardZone == null)
                HighlightMode = HighlightMode.Warn;
            else if (CurrentDragPhase != DragPhase.End)
                HighlightMode = HighlightMode.Authorized;
            else
                HighlightMode = HighlightMode.Off;

            var targetPosition =
                UnityExtensionMethods.UnityExtensionMethods.CalculateMean(PointerPositions.Values.ToList());
            targetPosition +=
                UnityExtensionMethods.UnityExtensionMethods.CalculateMean(PointerDragOffsets.Values.ToList());

            var rectTransform = (RectTransform) transform;
            rectTransform.position = targetPosition;
            rectTransform.SetAsLastSibling();

            if (IsOwner)
                RequestUpdatePosition(rectTransform.localPosition);
        }

        protected void ParentToPlayMat()
        {
            if (PlayController.Instance == null || PlayController.Instance.playMat == null)
            {
                Debug.LogError($"ERROR: Attempted to parent {gameObject.name} to non-existent play mat!");
                return;
            }

            var rectTransform = (RectTransform) transform;
            rectTransform.SetParent(PlayController.Instance.playMat.transform);
            rectTransform.localScale = Vector3.one;
        }

        public virtual void SnapToGrid()
        {
            var rectTransform = (RectTransform) transform;
            var gridPosition = CalculateGridPosition();
            rectTransform.position = gridPosition;

            if (IsOwner)
                RequestUpdatePosition(rectTransform.localPosition);
        }

        protected Vector2 CalculateGridPosition()
        {
            var rectTransform = (RectTransform) transform;
            var currentPosition = rectTransform.position;
            if (CardGameManager.Current.PlayMatGridCellSize.X <= 0 ||
                CardGameManager.Current.PlayMatGridCellSize.Y <= 0)
                return currentPosition;

            var gridCellSize = new Vector2(CardGameManager.Current.PlayMatGridCellSize.X,
                CardGameManager.Current.PlayMatGridCellSize.Y) * CardGameManager.PixelsPerInch;

            var x = Mathf.Round(currentPosition.x / gridCellSize.x) * gridCellSize.x;
            var y = Mathf.Round(currentPosition.y / gridCellSize.y) * gridCellSize.y;
            return new Vector2(x, y);
        }

        public void Rotate()
        {
            Vector2 referencePoint = transform.position;
            foreach (var pointerPosition in PointerPositions.Where(pointerPosition =>
                         pointerPosition.Key != CurrentPointerEventData.pointerId))
                referencePoint = pointerPosition.Value;
            var previousDirection = (CurrentPointerEventData.position - CurrentPointerEventData.delta) - referencePoint;
            var currentDirection = CurrentPointerEventData.position - referencePoint;
            transform.Rotate(0, 0, Vector2.SignedAngle(previousDirection, currentDirection));

            if (IsOnline)
                RequestUpdateRotation(transform.rotation);
        }

        protected void RequestChangeOwnership()
        {
            ChangeOwnershipServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        // ReSharper disable once SuggestBaseTypeForParameter
        private void ChangeOwnershipServerRpc(ServerRpcParams serverRpcParams = default)
        {
            var clientId = serverRpcParams.Receive.SenderClientId;
            if (!NetworkManager.ConnectedClients.ContainsKey(clientId))
            {
                Debug.Log($"CgsNetPlayable: Ignoring request to transfer authority for {gameObject.name}");
                return;
            }

            MyNetworkObject.ChangeOwnership(clientId);
        }

        protected void RequestUpdatePosition(Vector2 position)
        {
            UpdatePositionServerRpc(position);
        }

        [ServerRpc]
        private void UpdatePositionServerRpc(Vector2 position)
        {
            Position = position;
        }

        [PublicAPI]
        public void OnChangePosition(Vector2 oldValue, Vector2 newValue)
        {
            _position = newValue;
            if (!IsOwner)
                transform.localPosition = newValue;
        }

        protected void RequestUpdateRotation(Quaternion rotation)
        {
            UpdateRotationServerRpc(rotation);
        }

        [ServerRpc(RequireOwnership = false)]
        public void UpdateRotationServerRpc(Quaternion rotation)
        {
            Rotation = rotation;
        }

        [PublicAPI]
        public void OnChangeRotation(Quaternion oldValue, Quaternion newValue)
        {
            if (!IsOwner)
                transform.rotation = newValue;
        }

        [ServerRpc]
        private void RemoveOwnershipServerRpc()
        {
            MyNetworkObject.RemoveOwnership();
        }

        [ClientRpc]
        public void HideHighlightClientRpc()
        {
            HighlightMode = HighlightMode.Off;
        }

        protected void RequestDelete()
        {
            if (CgsNetManager.Instance.IsOnline)
                DeleteServerRpc();
            else
                Destroy(gameObject);
        }

        [ServerRpc(RequireOwnership = false)]
        private void DeleteServerRpc()
        {
            if (!IsOwnedByServer)
            {
                Debug.LogWarning("Ignoring request to delete, since it is currently owned by a client!");
                return;
            }

            MyNetworkObject.Despawn();
            Destroy(gameObject);
        }
    }
}
