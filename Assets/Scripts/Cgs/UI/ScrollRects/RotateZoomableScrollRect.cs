/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using Cgs.CardGameView.Multiplayer;
using Cgs.Play;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Cgs.UI.ScrollRects
{
    public class RotateZoomableScrollRect : ScrollRect, IPointerEnterHandler, IPointerExitHandler
    {
        private const float MinRotation = -180; // Also in PlayMatRotation slider
        private const float MaxRotation = 180; // Also in PlayMatRotation slider
        private const float MinZoom = 0.25f; // Also in PlayMatZoom slider
        public const float DefaultZoom = 0.5f;
        private const float MaxZoom = 1.5f; // Also in PlayMatZoom slider
        private const float MouseRotationSensitivity = 360;
        private const float ZoomWheelSensitivity = 0.5f;
        private const float ZoomLerpSpeed = 7.5f;
        private const float ZoomThreshold = 0.001f;
        private const float ScrollWheelSensitivity = 20; // Can be overridden by scrollSensitivity

        public float CurrentRotation
        {
            get => _currentEulerAngles.z;
            set
            {
                var desiredRotation = value;
                if (desiredRotation < MinRotation)
                    desiredRotation += (MaxRotation - MinRotation);
                else if (desiredRotation > MaxRotation)
                    desiredRotation -= (MaxRotation - MinRotation);
                _currentEulerAngles = Vector3.forward * Mathf.Clamp(desiredRotation, MinRotation, MaxRotation);
                _currentRotation.eulerAngles = _currentEulerAngles;
                content.rotation = _currentRotation;
            }
        }

        private Vector3 _currentEulerAngles = Vector3.zero;
        private Quaternion _currentRotation = Quaternion.identity;

        public float CurrentZoom
        {
            get => _currentZoom;
            set
            {
                if (!ZoomEnabled)
                    return;
                _currentZoom = Mathf.Clamp(value, MinZoom, MaxZoom);
                PlaySettings.DefaultZoom = _currentZoom;
            }
        }

        private float _currentZoom = DefaultZoom;

        public bool ZoomEnabled
        {
            get => Touch.activeTouches.Count > 1 || InputManager.IsCtrl || scrollSensitivity == 0;
            set => scrollSensitivity = value ? 0 : _scrollSensitivity;
        }

        private float _scrollSensitivity;

        private Dictionary<int, Vector2> PointerPositions { get; } = new();
        private bool _isPinching;
        private bool _blockPan;
        private float _startPinchDist;
        private float _startPinchZoom;
        private bool _isOver;

        protected override void Awake()
        {
            scrollSensitivity = ScrollWheelSensitivity;
            _scrollSensitivity = scrollSensitivity > 0 ? scrollSensitivity : ScrollWheelSensitivity;
            _currentZoom = PlaySettings.DefaultZoom;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EnhancedTouchSupport.Enable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EnhancedTouchSupport.Disable();
        }

        protected override void SetContentAnchoredPosition(Vector2 position)
        {
            if (_isPinching || _blockPan)
                return;
            base.SetContentAnchoredPosition(position);
        }

        public override void OnDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && !InputManager.IsShift &&
                Touch.activeTouches.Count <= 1)
                return;

            PointerPositions[eventData.pointerId] = eventData.position;
            switch (eventData.button)
            {
                case PointerEventData.InputButton.Right:
                    if (ZoomEnabled)
                        CurrentRotation += eventData.delta.x / Screen.width * MouseRotationSensitivity;
                    else
                        OnDragPan(eventData);
                    break;

                case PointerEventData.InputButton.Middle:
                    OnDragPan(eventData);
                    break;

                case PointerEventData.InputButton.Left:
                default:
                    if (PointerPositions.Count >= 2)
                        OnDragTouch(eventData);
                    else
                        base.OnDrag(eventData);
                    break;
            }
        }

        private void OnDragPan(PointerEventData mouseEventData)
        {
            content.localPosition += new Vector3(mouseEventData.delta.x, mouseEventData.delta.y);
            normalizedPosition = new Vector2(Mathf.Clamp(normalizedPosition.x, 0.0f, 1.0f),
                Mathf.Clamp(normalizedPosition.y, 0.0f, 1.0f));
        }

        private void OnDragTouch(PointerEventData touchEventData)
        {
            if (PointerPositions.Count < 2)
            {
                base.OnDrag(touchEventData);
                return;
            }

            OnDragPan(touchEventData);

            Vector2 referencePoint = content.position;
            foreach (var position in
                     PointerPositions.Where(position => position.Key != touchEventData.pointerId))
                referencePoint = position.Value;
            var previousDirection = (touchEventData.position - touchEventData.delta) - referencePoint;
            var currentDirection = touchEventData.position - referencePoint;
            CurrentRotation += Vector2.SignedAngle(previousDirection, currentDirection);
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            base.OnEndDrag(eventData);
            PointerPositions.Remove(eventData.pointerId);
        }

        private void Update()
        {
            // Touch zoom
            var touches = new List<Vector2>(Touch.activeTouches.Select(touch => touch.screenPosition));
            for (var i = touches.Count - 1; i >= 0; i--)
                if (IsTouchingCard(touches[i]))
                    touches.RemoveAt(i);
            if (touches.Count == 2)
            {
                if (!_isPinching)
                {
                    _isPinching = true;
                    _blockPan = true;
                    _startPinchDist = Distance(touches[0], touches[1]) * content.localScale.x;
                    _startPinchZoom = CurrentZoom;
                }

                var currentPinchDist = Distance(touches[0], touches[1]) * content.localScale.x;
                CurrentZoom = (currentPinchDist / _startPinchDist) * _startPinchZoom;
            }
            else
            {
                _isPinching = false;
                if (touches.Count == 0)
                {
                    _blockPan = false;
                    // Mouse ScrollWheel zoom
                    if (Mouse.current != null)
                    {
                        var scrollDelta = Mouse.current.scroll.ReadValue().y;
                        _blockPan = Mathf.Abs(scrollDelta) > 0 &&
                                    (EventSystem.current?.IsPointerOverGameObject() ?? false) && _isOver;
                        CurrentZoom *= 1 + (scrollDelta / 120f) * ZoomWheelSensitivity;
                    }
                }
            }

            // Scale to zoom
            if (Mathf.Abs(content.localScale.x - CurrentZoom) > ZoomThreshold)
                content.localScale = Vector3.Lerp(content.localScale, Vector3.one * CurrentZoom,
                    ZoomLerpSpeed * Time.deltaTime);
        }

        private bool IsTouchingCard(Vector2 position)
        {
            var cardModels = content.GetComponentsInChildren<CardModel>();
            return cardModels.Any(cardModel => cardModel.PointerPositions.ContainsValue(position));
        }

        private float Distance(Vector2 pos1, Vector2 pos2)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(content, pos1, null, out pos1);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(content, pos2, null, out pos2);
            return Vector2.Distance(pos1, pos2);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isOver = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isOver = false;
        }
    }
}
