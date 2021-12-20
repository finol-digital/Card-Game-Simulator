/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using Cgs.CardGameView.Multiplayer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cgs.ScrollRects
{
    public class RotateZoomableScrollRect : ScrollRect, IPointerEnterHandler, IPointerExitHandler
    {
        private const float MinRotation = -180; // Also in PlayMatRotation slider
        private const float MaxRotation = 180; // Also in PlayMatRotation slider
        private const float MinZoom = 0.66f; // Also in PlayMatZoom slider
        private const float MaxZoom = 1.33f; // Also in PlayMatZoom slider
        private const float MouseRotationSensitivity = 360;
        private const float ZoomLerpSpeed = 7.5f;
        private const float ZoomWheelSensitivity = 0.2f;
        private const float ScrollWheelSensitivity = 20; // Can be overridden by scrollSensitivity

        public float CurrentRotation
        {
            get => _currentEulerAngles.z;
            set
            {
                _currentEulerAngles = Vector3.forward * Mathf.Clamp(value, MinRotation, MaxRotation);
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
                if (ZoomEnabled)
                    _currentZoom = Mathf.Clamp(value, MinZoom, MaxZoom);
            }
        }

        private float _currentZoom = 1;

        public bool ZoomEnabled
        {
            get => scrollSensitivity == 0;
            set => scrollSensitivity = value ? 0 : _scrollSensitivity;
        }

        private float _scrollSensitivity;

        private List<Vector2> _touches = new List<Vector2>();
        private bool _isPinching;
        private float _startPinchDist;
        private float _startPinchZoom;
        private bool _blockPan;
        private bool _isOver;

        protected override void Awake()
        {
            Input.multiTouchEnabled = true;
            _scrollSensitivity = scrollSensitivity > 0 ? scrollSensitivity : ScrollWheelSensitivity;
            ZoomEnabled = true;
        }

        protected override void SetContentAnchoredPosition(Vector2 position)
        {
            if (_isPinching || _blockPan)
                return;
            base.SetContentAnchoredPosition(position);
        }

        public override void OnDrag(PointerEventData eventData)
        {
            switch (eventData.button)
            {
                case PointerEventData.InputButton.Right:
                    CurrentRotation += eventData.delta.x / Screen.width * MouseRotationSensitivity;
                    break;
                case PointerEventData.InputButton.Middle:
                    content.localPosition += new Vector3(eventData.delta.x * 2, eventData.delta.y);
                    normalizedPosition = new Vector2(Mathf.Clamp(normalizedPosition.x, 0.0f, 1.0f),
                        Mathf.Clamp(normalizedPosition.y, 0.0f, 1.0f));
                    break;
                case PointerEventData.InputButton.Left:
                default:
                    base.OnDrag(eventData);
                    break;
            }
        }

        private void Update()
        {
            // Touch input
            _touches = new List<Vector2>(Input.touches.Select(touch => touch.position));
            for (var i = _touches.Count - 1; i >= 0; i--)
                if (IsTouchingCard(_touches[i]))
                    _touches.RemoveAt(i);
            if (_touches.Count == 2)
            {
                if (!_isPinching)
                {
                    _isPinching = true;
                    _blockPan = true;
                    var contentRectTransform = content;
                    _startPinchDist = Distance(_touches[0], _touches[1]) * contentRectTransform.localScale.x;
                    _startPinchZoom = CurrentZoom;
                }

/*
                Vector2 referencePoint = transform.position;
                foreach (KeyValuePair<int, Vector2> pointerDragPosition in PointerPositions)
                    if (pointerDragPosition.Key != CurrentPointerEventData.pointerId)
                        referencePoint = pointerDragPosition.Value;
                Vector2 prevDir = (CurrentPointerEventData.position - CurrentPointerEventData.delta) - referencePoint;
                Vector2 currDir = CurrentPointerEventData.position - referencePoint;
                transform.Rotate(0, 0, Vector2.SignedAngle(prevDir, currDir));*/
                var currentPinchDist = Distance(_touches[0], _touches[1]) * content.localScale.x;
                CurrentZoom = (currentPinchDist / _startPinchDist) * _startPinchZoom;
            }
            else
            {
                _isPinching = false;
                if (_touches.Count == 0)
                    _blockPan = false;
            }

            // Mouse input
            var scrollWheelInput = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scrollWheelInput) > float.Epsilon && EventSystem.current.IsPointerOverGameObject() && _isOver)
                CurrentZoom *= 1 + scrollWheelInput * ZoomWheelSensitivity;

            // Scale to zoom
            if (Mathf.Abs(content.localScale.x - CurrentZoom) > 0.001f)
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
