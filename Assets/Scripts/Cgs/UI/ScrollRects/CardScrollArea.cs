/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.CardGameView;
using Cgs.CardGameView.Multiplayer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cgs.UI.ScrollRects
{
    public enum CardScrollDirection
    {
        Left,
        Down,
        Right,
        Up
    }

    [RequireComponent(typeof(CanvasGroup))]
    public class CardScrollArea : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler
    {
        private const float ScrollSpeed = 1500;

        public ScrollRect scrollRect;
        public CardScrollDirection scrollDirection = CardScrollDirection.Left;

        private CanvasGroup _canvasGroup;
        private bool _isScrolling;

        private void Start()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Update()
        {
            bool blocksRayCast;
            switch (scrollDirection)
            {
                case CardScrollDirection.Left when scrollRect.horizontalNormalizedPosition <= 0:
                case CardScrollDirection.Down when scrollRect.verticalNormalizedPosition <= 0:
                case CardScrollDirection.Right when scrollRect.horizontalNormalizedPosition >= 1:
                case CardScrollDirection.Up when scrollRect.verticalNormalizedPosition >= 1:
                    blocksRayCast = false;
                    _canvasGroup.alpha = 0;
                    break;
                default:
                    blocksRayCast = true;
                    break;
            }

            _canvasGroup.blocksRaycasts = blocksRayCast && EventSystem.current.currentSelectedGameObject == null;

            if (!_isScrolling)
                return;

            _canvasGroup.alpha = 1;
            var rect = scrollRect.content.rect;
            var deltaWidth = ScrollSpeed / rect.width * Time.deltaTime;
            var deltaHeight = ScrollSpeed / rect.height * Time.deltaTime;
            switch (scrollDirection)
            {
                case CardScrollDirection.Left:
                    scrollRect.horizontalNormalizedPosition =
                        Mathf.Clamp01(scrollRect.horizontalNormalizedPosition - deltaWidth);
                    if (scrollRect.horizontalNormalizedPosition <= 0)
                        _isScrolling = false;
                    break;
                case CardScrollDirection.Down:
                    scrollRect.verticalNormalizedPosition =
                        Mathf.Clamp01(scrollRect.verticalNormalizedPosition - deltaHeight);
                    if (scrollRect.verticalNormalizedPosition <= 0)
                        _isScrolling = false;
                    break;
                case CardScrollDirection.Right:
                    scrollRect.horizontalNormalizedPosition =
                        Mathf.Clamp01(scrollRect.horizontalNormalizedPosition + deltaWidth);
                    if (scrollRect.horizontalNormalizedPosition >= 1)
                        _isScrolling = false;
                    break;
                case CardScrollDirection.Up:
                    scrollRect.verticalNormalizedPosition =
                        Mathf.Clamp01(scrollRect.verticalNormalizedPosition + deltaHeight);
                    if (scrollRect.verticalNormalizedPosition >= 1)
                        _isScrolling = false;
                    break;
                default:
                    Debug.LogError("Card Scroll Area needs direction!");
                    break;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _canvasGroup.alpha = 1;
            if (eventData.pointerDrag == null)
                return;

            var playable = eventData.pointerDrag.GetComponent<CgsNetPlayable>();
            if (playable != null &&
                (playable.ParentCardZone == null || playable.ParentCardZone.type == CardZoneType.Area))
                _isScrolling = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Hide();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Hide();
        }

        private void Hide()
        {
            _canvasGroup.alpha = 0;
            _isScrolling = false;
        }
    }
}
