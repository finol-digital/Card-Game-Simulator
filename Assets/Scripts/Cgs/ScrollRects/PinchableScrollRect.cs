using System.Collections.Generic;
using System.Linq;
using Cgs.CardGameView.Multiplayer;
using UnityEngine;

namespace ScrollRects
{
    public class PinchableScrollRect : SecondaryScrollView
    {
        public const float MinZoom = 0.66f;
        public const float MaxZoom = 1.5f;
        public const float ZoomLerpSpeed = 7.5f;
        public const float MouseWheelSensitivity = 0.1f;

        public List<Vector2> Touches { get; private set; } = new List<Vector2>();

        private float _currentZoom = 1;
        private bool _isPinching;
        private float _startPinchDist;
        private float _startPinchZoom;
        private Vector2 _startPinchCenterPosition;
        private Vector2 _startPinchScreenPosition;
        private bool _blockPan;

        protected override void Awake()
        {
            Input.multiTouchEnabled = true;
        }

        protected override void SetContentAnchoredPosition(Vector2 position)
        {
            if (_isPinching || _blockPan)
                return;
            base.SetContentAnchoredPosition(position);
        }

        void Update()
        {
            // Touch input
            Touches = new List<Vector2>(Input.touches.Select(touch => touch.position));
            for (int i = Touches.Count - 1; i >= 0; i--)
            {
                if (IsTouchingCard(Touches[i]))
                    Touches.RemoveAt(i);
            }

            if (Touches.Count == 2)
            {
                if (!_isPinching)
                {
                    _isPinching = true;
                    OnStartPinch();
                }

                OnPinch();
            }
            else
            {
                _isPinching = false;
                if (Touches.Count == 0)
                    _blockPan = false;
            }

            // Mouse input
            float scrollWheelInput = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scrollWheelInput) > float.Epsilon)
            {
                RectTransform content1 = content;
                Rect rect = content1.rect;

                _currentZoom *= 1 + scrollWheelInput * MouseWheelSensitivity;
                _currentZoom = Mathf.Clamp(_currentZoom, MinZoom, MaxZoom);
                _startPinchScreenPosition = Input.mousePosition;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(content, _startPinchScreenPosition, null,
                    out _startPinchCenterPosition);

                var pivotPosition =
                    new Vector2(content1.pivot.x * content1.rect.size.x, content1.pivot.y * rect.size.y);
                Vector2 posFromBottomLeft = pivotPosition + _startPinchCenterPosition;
                SetPivot(content1,
                    new Vector2(posFromBottomLeft.x / rect.width, posFromBottomLeft.y / rect.height));
            }

            // Scale to zoom
            if (Mathf.Abs(content.localScale.x - _currentZoom) > 0.001f)
                content.localScale = Vector3.Lerp(content.localScale, Vector3.one * _currentZoom,
                    ZoomLerpSpeed * Time.deltaTime);
        }

        private bool IsTouchingCard(Vector2 position)
        {
            CardModel[] cardModels = content.GetComponentsInChildren<CardModel>();
            return cardModels.Any(cardModel => cardModel.PointerPositions.ContainsValue(position));
        }

        private void OnStartPinch()
        {
            RectTransform content1 = content;
            Rect rect = content1.rect;

            _startPinchDist = Distance(Touches[0], Touches[1]) * content1.localScale.x;
            _startPinchZoom = _currentZoom;
            _startPinchScreenPosition = (Touches[0] + Touches[1]) / 2;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(content1, _startPinchScreenPosition, null,
                out _startPinchCenterPosition);

            Vector2 pivotPosition =
                new Vector3(content.pivot.x * content1.rect.size.x, content1.pivot.y * rect.size.y);
            Vector2 posFromBottomLeft = pivotPosition + _startPinchCenterPosition;

            SetPivot(content1,
                new Vector2(posFromBottomLeft.x / rect.width, posFromBottomLeft.y / rect.height));
            _blockPan = true;
        }

        private void OnPinch()
        {
            float currentPinchDist = Distance(Touches[0], Touches[1]) * content.localScale.x;
            _currentZoom = (currentPinchDist / _startPinchDist) * _startPinchZoom;
            _currentZoom = Mathf.Clamp(_currentZoom, MinZoom, MaxZoom);
        }

        private float Distance(Vector2 pos1, Vector2 pos2)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(content, pos1, null, out pos1);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(content, pos2, null, out pos2);
            return Vector2.Distance(pos1, pos2);
        }

        private static void SetPivot(RectTransform rectTransform, Vector2 pivot)
        {
            if (rectTransform == null)
                return;

            Vector2 size = rectTransform.rect.size;
            Vector2 deltaPivot = rectTransform.pivot - pivot;
            Vector3 deltaPosition =
                new Vector3(deltaPivot.x * size.x, deltaPivot.y * size.y) * rectTransform.localScale.x;
            rectTransform.pivot = pivot;
            rectTransform.localPosition -= deltaPosition;
        }
    }
}
