/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.CardGameView.Multiplayer;
using Cgs.CardGameView.Viewer;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Cgs.Cards
{
    public class CardSelector : MonoBehaviour, IDragHandler, IEndDragHandler
    {
        public SearchResults results;
        public ScrollRect scrollRect;

        private InputAction _moveAction;
        private InputAction _pageAction;

        private void Start()
        {
            CardViewer.Instance.buttonsPanel.gameObject.SetActive(true);
            CardViewer.Instance.previousButton.onClick.AddListener(SelectLeft);
            CardViewer.Instance.nextButton.onClick.AddListener(SelectRight);

            _moveAction = InputSystem.actions.FindAction(Tags.PlayerMove);
            _pageAction = InputSystem.actions.FindAction(Tags.PlayerPage);
        }

        // Poll for Vector2 inputs
        private void Update()
        {
            if (CardGameManager.Instance.ModalCanvas != null || results.inputField.isFocused)
                return;

            var pageVector2 = _pageAction?.ReadValue<Vector2>() ?? Vector2.zero;
            if ((_pageAction?.WasPressedThisFrame() ?? false) && Mathf.Abs(pageVector2.x) > 0.5f)
            {
                switch (pageVector2.x)
                {
                    case < 0:
                        PageLeft();
                        break;
                    case > 0:
                        PageRight();
                        break;
                }
            }
            else
            {
                if (CardViewer.Instance.IsVisible && CardViewer.Instance.Mode == CardViewerMode.Maximal)
                    return;
                if (pageVector2.y < 0)
                    scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition - 0.1f);
                else if (pageVector2.y > 0)
                    scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition + 0.1f);
            }

            if (!(_moveAction?.WasPressedThisFrame() ?? false))
                return;
            var moveVector2 = _moveAction.ReadValue<Vector2>();
            switch (moveVector2.y)
            {
                case < 0:
                    SelectDown();
                    break;
                case > 0:
                    SelectUp();
                    break;
                default:
                {
                    switch (moveVector2.x)
                    {
                        case < 0:
                            SelectLeft();
                            break;
                        case > 0:
                            SelectRight();
                            break;
                    }

                    break;
                }
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Just required by the interface to get OnEndDrag called
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (CardGameManager.Instance.ModalCanvas != null || results.inputField.isFocused)
                return;

            var dragDelta = eventData.position - eventData.pressPosition;
            var swipeDirection = UnityExtensionMethods.UnityExtensionMethods.GetSwipeDirection(dragDelta);

            if ((CardViewer.Instance.IsVisible && CardViewer.Instance.Mode == CardViewerMode.Maximal)
                || CardViewer.Instance.Zoom && CardViewer.Instance.ZoomTime > 0.5f)
            {
                if (swipeDirection == UnityExtensionMethods.UnityExtensionMethods.SwipeDirection.Right)
                    SelectLeft();
                else if (swipeDirection == UnityExtensionMethods.UnityExtensionMethods.SwipeDirection.Left)
                    SelectRight();
            }
            else if (!CardViewer.Instance.Zoom)
            {
                if (swipeDirection == UnityExtensionMethods.UnityExtensionMethods.SwipeDirection.Right)
                    PageLeft();
                else if (swipeDirection == UnityExtensionMethods.UnityExtensionMethods.SwipeDirection.Left)
                    PageRight();
            }
        }

        [UsedImplicitly]
        public void SelectDown()
        {
            if (EventSystem.current.alreadySelecting)
                return;

            if (results.layoutArea.childCount < 1)
            {
                EventSystem.current.SetSelectedGameObject(null);
                return;
            }

            for (var i = 0; i < results.layoutArea.childCount; i++)
            {
                if (results.layoutArea.GetChild(i).GetComponent<CardModel>() != CardViewer.Instance.SelectedCardModel)
                    continue;
                i += results.CardsPerRow;
                if (i >= results.layoutArea.childCount)
                {
                    results.IncrementPage();
                    i = 0;
                }

                EventSystem.current.SetSelectedGameObject(results.layoutArea.GetChild(i).gameObject);
                scrollRect.verticalNormalizedPosition = 1.0f - ((float)i / results.layoutArea.childCount);
                return;
            }

            EventSystem.current.SetSelectedGameObject(results.layoutArea.GetChild(0).gameObject);
            scrollRect.verticalNormalizedPosition = 0;
            if (CardViewer.Instance != null && CardViewer.Instance.SelectedCardModel != null)
                CardViewer.Instance.IsVisible = true;
        }

        [UsedImplicitly]
        public void SelectUp()
        {
            if (EventSystem.current.alreadySelecting)
                return;

            if (results.layoutArea.childCount < 1)
            {
                EventSystem.current.SetSelectedGameObject(null);
                return;
            }

            for (var i = results.layoutArea.childCount - 1; i >= 0; i--)
            {
                if (results.layoutArea.GetChild(i).GetComponent<CardModel>() != CardViewer.Instance.SelectedCardModel)
                    continue;
                i -= results.CardsPerRow;
                if (i < 0)
                {
                    results.DecrementPage();
                    i = results.layoutArea.childCount - 1;
                }

                EventSystem.current.SetSelectedGameObject(results.layoutArea.GetChild(i).gameObject);
                scrollRect.verticalNormalizedPosition = 1.0f - ((float)i / results.layoutArea.childCount);
                return;
            }

            EventSystem.current.SetSelectedGameObject(results.layoutArea.GetChild(0).gameObject);
            scrollRect.verticalNormalizedPosition = 0;
            if (CardViewer.Instance != null && CardViewer.Instance.SelectedCardModel != null)
                CardViewer.Instance.IsVisible = true;
        }

        [UsedImplicitly]
        public void SelectLeft()
        {
            if (EventSystem.current.alreadySelecting)
                return;

            if (results.layoutArea.childCount < 1)
            {
                EventSystem.current.SetSelectedGameObject(null);
                return;
            }

            for (var i = results.layoutArea.childCount - 1; i >= 0; i--)
            {
                if (results.layoutArea.GetChild(i).GetComponent<CardModel>() != CardViewer.Instance.SelectedCardModel)
                    continue;
                i--;
                if (i < 0)
                {
                    results.DecrementPage();
                    i = results.layoutArea.childCount - 1;
                }

                EventSystem.current.SetSelectedGameObject(results.layoutArea.GetChild(i).gameObject);
                scrollRect.verticalNormalizedPosition = 1.0f - ((float)i / results.layoutArea.childCount);
                return;
            }

            EventSystem.current.SetSelectedGameObject(results.layoutArea.GetChild(0).gameObject);
            scrollRect.verticalNormalizedPosition = 0;
            if (CardViewer.Instance != null && CardViewer.Instance.SelectedCardModel != null)
                CardViewer.Instance.IsVisible = true;
        }

        [UsedImplicitly]
        public void SelectRight()
        {
            if (EventSystem.current.alreadySelecting)
                return;

            if (results.layoutArea.childCount < 1)
            {
                EventSystem.current.SetSelectedGameObject(null);
                return;
            }

            for (var i = 0; i < results.layoutArea.childCount; i++)
            {
                if (results.layoutArea.GetChild(i).GetComponent<CardModel>() != CardViewer.Instance.SelectedCardModel)
                    continue;
                i++;
                if (i == results.layoutArea.childCount)
                {
                    results.IncrementPage();
                    i = 0;
                }

                EventSystem.current.SetSelectedGameObject(results.layoutArea.GetChild(i).gameObject);
                scrollRect.verticalNormalizedPosition = 1.0f - ((float)i / results.layoutArea.childCount);
                return;
            }

            EventSystem.current.SetSelectedGameObject(results.layoutArea.GetChild(0).gameObject);
            scrollRect.verticalNormalizedPosition = 0;
            if (CardViewer.Instance != null && CardViewer.Instance.SelectedCardModel != null)
                CardViewer.Instance.IsVisible = true;
        }

        [UsedImplicitly]
        public void PageLeft()
        {
            results.DecrementPage();
        }

        [UsedImplicitly]
        public void PageRight()
        {
            results.IncrementPage();
        }
    }
}
