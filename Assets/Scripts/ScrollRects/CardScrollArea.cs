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

        void Update()
        {
            var blocksRayCast = true;
            if (scrollDirection == CardScrollDirection.Left && scrollRect.horizontalNormalizedPosition <= 0)
                blocksRayCast = false;
            else if (scrollDirection == CardScrollDirection.Down && scrollRect.verticalNormalizedPosition <= 0)
                blocksRayCast = false;
            else if (scrollDirection == CardScrollDirection.Right && scrollRect.horizontalNormalizedPosition >= 1)
                blocksRayCast = false;
            else if (scrollDirection == CardScrollDirection.Up && scrollRect.verticalNormalizedPosition >= 1)
                blocksRayCast = false;

            GetComponent<CanvasGroup>().blocksRaycasts = blocksRayCast;
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
            StopAllCoroutines();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            StopAllCoroutines();
        }

        private IEnumerator MoveScrollbar()
        {
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
