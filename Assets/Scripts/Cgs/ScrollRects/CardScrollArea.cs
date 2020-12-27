using System;
using Cgs.CardGameView;
using Cgs.CardGameView.Multiplayer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ScrollRects
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
            switch (scrollDirection)
            {
                case CardScrollDirection.Left:
                    float delta = ScrollSpeed / scrollRect.content.rect.width * Time.deltaTime;
                    scrollRect.horizontalNormalizedPosition =
                        Mathf.Clamp01(scrollRect.horizontalNormalizedPosition - delta);
                    if (scrollRect.horizontalNormalizedPosition <= 0)
                        _isScrolling = false;
                    break;
                case CardScrollDirection.Down:
                    float delta2 = ScrollSpeed / scrollRect.content.rect.height * Time.deltaTime;
                    scrollRect.verticalNormalizedPosition =
                        Mathf.Clamp01(scrollRect.verticalNormalizedPosition - delta2);
                    if (scrollRect.verticalNormalizedPosition <= 0)
                        _isScrolling = false;
                    break;
                case CardScrollDirection.Right:
                    float delta3 = ScrollSpeed / scrollRect.content.rect.width * Time.deltaTime;
                    scrollRect.horizontalNormalizedPosition =
                        Mathf.Clamp01(scrollRect.horizontalNormalizedPosition + delta3);
                    if (scrollRect.horizontalNormalizedPosition >= 1)
                        _isScrolling = false;
                    break;
                case CardScrollDirection.Up:
                    float delta4 = ScrollSpeed / scrollRect.content.rect.height * Time.deltaTime;
                    scrollRect.verticalNormalizedPosition =
                        Mathf.Clamp01(scrollRect.verticalNormalizedPosition + delta4);
                    if (scrollRect.verticalNormalizedPosition >= 1)
                        _isScrolling = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null)
                return;

            var cardModel = eventData.pointerDrag.GetComponent<CardModel>();
            if (cardModel != null &&
                (cardModel.ParentCardZone == null || cardModel.ParentCardZone.type == CardZoneType.Area))
                _isScrolling = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _canvasGroup.alpha = 0;
            _isScrolling = false;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _canvasGroup.alpha = 0;
            _isScrolling = false;
        }
    }
}
