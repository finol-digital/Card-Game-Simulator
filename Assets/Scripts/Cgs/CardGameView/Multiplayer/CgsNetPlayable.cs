/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using Cgs.Play.Multiplayer;
using JetBrains.Annotations;
using Mirror;
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

        public CardZone ParentCardZone => transform.parent != null ? transform.parent.GetComponent<CardZone>() : null;

        public bool IsOnline => CgsNetManager.Instance != null && CgsNetManager.Instance.isNetworkActive
                                                               && transform.parent == CgsNetManager.Instance
                                                                   .playController.playMat.transform;

        protected bool LacksAuthority => NetworkManager.singleton.isNetworkActive && !hasAuthority;

        protected bool IsProcessingSecondaryDragAction =>
            PointerDragOffsets.Count > 1
            || CurrentPointerEventData != null &&
            (CurrentPointerEventData.button == PointerEventData.InputButton.Middle ||
             CurrentPointerEventData.button == PointerEventData.InputButton.Right);

        [SyncVar(hook = nameof(OnChangePosition))]
        public Vector2 position;

        [SyncVar(hook = nameof(OnChangeRotation))]
        public Quaternion rotation;

        [SyncVar] public bool isClientAuthorized;

        public PointerEventData CurrentPointerEventData { get; protected set; }
        public Dictionary<int, Vector2> PointerPositions { get; } = new();
        public Dictionary<int, Vector2> PointerDragOffsets { get; } = new();

        protected bool DidSelectOnDown { get; set; }
        protected bool DidDrag { get; set; }
        protected DragPhase CurrentDragPhase { get; private set; }
        protected float HoldTime { get; private set; }

        protected bool ToDiscard { get; set; }

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
                        var isOthers = IsOnline && isClientAuthorized && !hasAuthority;
                        Highlight.effectColor = isOthers ? Color.yellow : Color.black;
                        Highlight.effectDistance = isOthers ? OutlineHighlightDistance : Vector2.zero;
                        break;
                }
            }
        }

        private HighlightMode _highlightMode = HighlightMode.Off;

        private Outline Highlight => _highlight ??= gameObject.GetOrAddComponent<Outline>();
        private Outline _highlight;

        protected virtual void OnStartPlayable()
        {
            // Child classes may override
        }

        private void Start()
        {
            OnStartPlayable();

            if (!IsOnline)
                return;

            if (Vector2.zero != position)
                ((RectTransform) transform).localPosition = position;
            if (Quaternion.identity != rotation)
                transform.rotation = rotation;
        }

        private void Update()
        {
            if (PointerPositions.Count > 0 && !DidDrag)
                HoldTime += Time.deltaTime;
            else
                HoldTime = 0;

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
            // Child classes may override
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            OnPointerExitPlayable(eventData);
        }

        protected virtual void OnPointerExitPlayable(PointerEventData eventData)
        {
            // Child classes may override
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
            // Child classes may override
        }

        public void OnDeselect(BaseEventData eventData)
        {
            OnDeselectPlayable(eventData);
        }

        protected virtual void OnDeselectPlayable(BaseEventData eventData)
        {
            // Child classes may override
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
            // Child classes may override
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
            // Child classes may override
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            CurrentPointerEventData = eventData;
            CurrentDragPhase = DragPhase.End;

            OnEndDragPlayable(eventData);

            RemovePointer(eventData);

            PostDragPlayable(eventData);

            if (IsOnline && hasAuthority && !ToDiscard)
                CmdReleaseAuthority();
        }

        protected virtual void OnEndDragPlayable(PointerEventData eventData)
        {
            // Child classes may override
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

            if (hasAuthority)
                RequestUpdatePosition(rectTransform.localPosition);
        }

        public virtual void SnapToGrid()
        {
            var rectTransform = (RectTransform) transform;
            var gridPosition = CalculateGridPosition();
            rectTransform.position = gridPosition;

            if (hasAuthority)
                RequestUpdatePosition(rectTransform.localPosition);
        }

        protected Vector2 CalculateGridPosition()
        {
            var rectTransform = (RectTransform) transform;
            var currentPosition = rectTransform.position;
            if (CardGameManager.Current.PlayMatGridCellSize == null ||
                CardGameManager.Current.PlayMatGridCellSize.X <= 0 ||
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

        protected void RequestTransferAuthority()
        {
            CmdTransferAuthority();
        }

        [Command(requiresAuthority = false)]
        // ReSharper disable once SuggestBaseTypeForParameter
        private void CmdTransferAuthority(NetworkConnectionToClient sender = null)
        {
            if (sender == null || netIdentity.connectionToClient != null)
            {
                Debug.Log("CgsNetPlayable: Ignoring request to transfer authority, as it is already transferred");
                return;
            }

            isClientAuthorized = true;
            netIdentity.AssignClientAuthority(sender);
        }

        protected void RequestUpdatePosition(Vector2 newPosition)
        {
            CmdUpdatePosition(newPosition);
        }

        [Command]
        private void CmdUpdatePosition(Vector2 newPosition)
        {
            position = newPosition;
        }

        [PublicAPI]
        public void OnChangePosition(Vector2 oldValue, Vector2 newValue)
        {
            if (!hasAuthority)
                transform.localPosition = newValue;
        }

        protected void RequestUpdateRotation(Quaternion newRotation)
        {
            CmdUpdateRotation(newRotation);
        }

        [Command(requiresAuthority = false)]
        public void CmdUpdateRotation(Quaternion newRotation)
        {
            rotation = newRotation;
        }

        [PublicAPI]
        public void OnChangeRotation(Quaternion oldRotation, Quaternion newRotation)
        {
            if (!hasAuthority)
                transform.rotation = newRotation;
        }

        [Command]
        private void CmdReleaseAuthority()
        {
            netIdentity.RemoveClientAuthority();
            isClientAuthorized = false;
        }

        [ClientRpc]
        public void RpcHideHighlight()
        {
            HighlightMode = HighlightMode.Off;
        }

        protected void RequestDelete()
        {
            if (NetworkManager.singleton.isNetworkActive)
                CmdDelete();
            else
                Destroy(gameObject);
        }

        [Command(requiresAuthority = false)]
        private void CmdDelete()
        {
            if (netIdentity.connectionToClient != null)
            {
                Debug.LogWarning("Ignoring request to delete, since it is currently owned by a client!");
                return;
            }

            NetworkServer.UnSpawn(gameObject);
            Destroy(gameObject);
        }
    }
}
