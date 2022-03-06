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

namespace Cgs.CardGameView.Multiplayer
{
    public enum DragPhase
    {
        Begin,
        Drag,
        End
    }

    public class CgsNetPlayable : NetworkBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler,
        IPointerUpHandler, ISelectHandler, IDeselectHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public bool IsOnline => CgsNetManager.Instance != null && CgsNetManager.Instance.isNetworkActive
                                                               && transform.parent == CgsNetManager.Instance
                                                                   .playController.playMat.transform;

        protected bool LacksAuthority => NetworkManager.singleton.isNetworkActive && !hasAuthority;

        [SyncVar(hook = nameof(OnChangePosition))]
        public Vector2 position;

        protected PointerEventData CurrentPointerEventData;
        protected DragPhase CurrentDragPhase;

        protected Dictionary<int, Vector2> PointerPositions { get; } = new Dictionary<int, Vector2>();
        protected Dictionary<int, Vector2> PointerDragOffsets { get; } = new Dictionary<int, Vector2>();

        private void Start()
        {
            OnStartPlayable();

            if (IsOnline && Vector2.zero != position)
                ((RectTransform) transform).localPosition = position;
        }

        protected virtual void OnStartPlayable()
        {
            // Child classes may override
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // TODO: PREVIEW
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // TODO: PREVIEW
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

        protected virtual void OnPointerUpSelect(PointerEventData eventData)
        {
            if (!EventSystem.current.alreadySelecting)
                EventSystem.current.SetSelectedGameObject(gameObject, eventData);
        }

        protected virtual void OnPointerUpPlayable(PointerEventData eventData)
        {
            // Child classes may override
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            OnPointerUpSelect(eventData);

            OnPointerUpPlayable(eventData);

            CurrentPointerEventData = eventData;

            if (CurrentDragPhase == DragPhase.Drag)
                return;
            PointerPositions.Remove(eventData.pointerId);
            PointerDragOffsets.Remove(eventData.pointerId);
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

        public void OnBeginDrag(PointerEventData eventData)
        {
            CurrentPointerEventData = eventData;
            CurrentDragPhase = DragPhase.Begin;
            PointerPositions[eventData.pointerId] = eventData.position;

            OnBeginDragPlayable(eventData);
        }

        protected virtual void OnBeginDragPlayable(PointerEventData eventData)
        {
            if (NetworkManager.singleton.isNetworkActive)
                CmdTransferAuthority();
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
            if (LacksAuthority)
                CmdTransferAuthority();
            else
                UpdatePosition();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            CurrentPointerEventData = eventData;
            CurrentDragPhase = DragPhase.End;

            OnEndDragPlayable(eventData);

            if (hasAuthority)
                CmdReleaseAuthority();
            RemovePointer(eventData);
        }

        protected virtual void OnEndDragPlayable(PointerEventData eventData)
        {
            // Child classes may override
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

        protected void UpdatePosition()
        {
            var targetPosition =
                UnityExtensionMethods.UnityExtensionMethods.CalculateMean(PointerPositions.Values.ToList());
            targetPosition +=
                UnityExtensionMethods.UnityExtensionMethods.CalculateMean(PointerDragOffsets.Values.ToList());

            var rectTransform = (RectTransform) transform;
            rectTransform.position = targetPosition;
            rectTransform.SetAsLastSibling();

            if (hasAuthority)
                CmdUpdatePosition(rectTransform.localPosition);
        }

        protected void RequestTransferAuthority()
        {
            CmdTransferAuthority();
        }

        [Command(requiresAuthority = false)]
        private void CmdTransferAuthority(NetworkConnectionToClient sender = null)
        {
            if (sender != null && netIdentity.connectionToClient == null)
                netIdentity.AssignClientAuthority(sender);
        }

        [Command]
        private void CmdUpdatePosition(Vector2 newPosition)
        {
            position = newPosition;
        }

        [PublicAPI]
        public void OnChangePosition(Vector2 oldValue, Vector2 newValue)
        {
            transform.localPosition = newValue;
        }

        [Command]
        private void CmdReleaseAuthority()
        {
            netIdentity.RemoveClientAuthority();
        }

        protected void Delete()
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
