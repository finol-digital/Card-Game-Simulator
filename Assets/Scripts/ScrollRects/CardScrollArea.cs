using System;
using System.Collections;
using CardGameView;
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
        public ScrollRect scrollRect;
        public CardScrollDirection scrollDirection = CardScrollDirection.Left;
        public float scrollAmount = 0.01f;
        public float holdFrequency = 0.01f;

        private CanvasGroup _canvasGroup;

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

            _canvasGroup.blocksRaycasts = blocksRayCast;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null)
                return;

            var cardModel = eventData.pointerDrag.GetComponent<CardModel>();
            if (cardModel != null &&
                (cardModel.ParentCardStack == null || cardModel.ParentCardStack.type == CardStackType.Area))
                StartCoroutine(MoveScrollbar());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _canvasGroup.alpha = 0;
            StopAllCoroutines();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _canvasGroup.alpha = 0;
            StopAllCoroutines();
        }

        private IEnumerator MoveScrollbar()
        {
            _canvasGroup.alpha = 1;
            switch (scrollDirection)
            {
                case CardScrollDirection.Left:
                    scrollRect.horizontalNormalizedPosition =
                        Mathf.Clamp01(scrollRect.horizontalNormalizedPosition - scrollAmount);
                    if (scrollRect.horizontalNormalizedPosition <= 0)
                        yield break;
                    break;
                case CardScrollDirection.Down:
                    scrollRect.verticalNormalizedPosition =
                        Mathf.Clamp01(scrollRect.verticalNormalizedPosition - scrollAmount);
                    if (scrollRect.verticalNormalizedPosition <= 0)
                        yield break;
                    break;
                case CardScrollDirection.Right:
                    scrollRect.horizontalNormalizedPosition =
                        Mathf.Clamp01(scrollRect.horizontalNormalizedPosition + scrollAmount);
                    if (scrollRect.horizontalNormalizedPosition >= 1)
                        yield break;
                    break;
                case CardScrollDirection.Up:
                    scrollRect.verticalNormalizedPosition =
                        Mathf.Clamp01(scrollRect.verticalNormalizedPosition + scrollAmount);
                    if (scrollRect.verticalNormalizedPosition >= 1)
                        yield break;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            yield return new WaitForSeconds(holdFrequency);
            StartCoroutine(MoveScrollbar());
        }
    }
}
