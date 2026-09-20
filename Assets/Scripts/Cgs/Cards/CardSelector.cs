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
        [SerializeField] SearchResults results;
        [SerializeField] ScrollRect scrollRect;

        private InputAction _moveAction;
        private InputAction _pageAction;

        private bool IsBlocked => results.InputField.isFocused || CardGameManager.Instance.ModalCanvas != null;

        protected void OnEnable()
        {
            InputSystem.actions.FindAction(Tags.CardsPagePrevious).performed += InputPagePrevious;
            InputSystem.actions.FindAction(Tags.CardsPageNext).performed += InputPageNext;
        }

        protected void Start()
        {
            CardViewer.Instance.ButtonsPanel.gameObject.SetActive(true);
            CardViewer.Instance.PreviousButton.onClick.AddListener(SelectLeft);
            CardViewer.Instance.NextButton.onClick.AddListener(SelectRight);

            _moveAction = InputSystem.actions.FindAction(Tags.PlayerMove);
            _pageAction = InputSystem.actions.FindAction(Tags.PlayerPage);
        }

        // Poll for Vector2 inputs
        protected void Update()
        {
            if (IsBlocked)
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
            else if (!CardViewer.Instance.IsVisible || CardViewer.Instance.Mode != CardViewerMode.Maximal)
            {
                var delta = pageVector2.y * Time.deltaTime;
                if (Mathf.Abs(delta) > 0)
                    scrollRect.verticalNormalizedPosition =
                        Mathf.Clamp01(scrollRect.verticalNormalizedPosition + delta);
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
            if (IsBlocked)
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

        private void FocusScrollRectOn(int index)
        {
            var cardsPerRow = Mathf.Max(1, results.CardsPerRow);
            var rowCount = Mathf.CeilToInt((float)results.LayoutArea.childCount / cardsPerRow);
            var rowIndex = index / cardsPerRow;
            scrollRect.verticalNormalizedPosition = rowCount > 1 ? 1f - rowIndex / (rowCount - 1f) : 1f;
        }

        [UsedImplicitly]
        public void SelectDown()
        {
            Select(Mathf.Max(1, results.CardsPerRow));
        }

        [UsedImplicitly]
        public void SelectUp()
        {
            Select(-Mathf.Max(1, results.CardsPerRow));
        }

        [UsedImplicitly]
        public void SelectLeft()
        {
            Select(-1, true);
        }

        [UsedImplicitly]
        public void SelectRight()
        {
            Select(1, true);
        }

        private void Select(int step, bool isHorizontal = false)
        {
            if (step == 0 || IsBlocked || EventSystem.current.alreadySelecting)
                return;

            var childCount = results.LayoutArea.childCount;
            if (childCount < 1)
            {
                EventSystem.current.SetSelectedGameObject(null);
                return;
            }

            // In a single-column grid, left/right move across pages rather than within the column
            var pagesHorizontally = isHorizontal && results.CardsPerRow <= 1;

            var forward = step > 0;
            for (var i = forward ? 0 : childCount - 1; forward ? i < childCount : i >= 0; i += forward ? 1 : -1)
            {
                if (results.LayoutArea.GetChild(i).GetComponent<CardModel>() != CardViewer.Instance.SelectedCardModel)
                    continue;
                i += step;
                if ((pagesHorizontally && step > 0) || i >= childCount)
                {
                    results.IncrementPage();
                    childCount = results.LayoutArea.childCount;
                    i = 0;
                }
                else if ((pagesHorizontally && step < 0) || i < 0)
                {
                    results.DecrementPage();
                    childCount = results.LayoutArea.childCount;
                    i = childCount - 1;
                }

                if (i < 0 || i >= childCount)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                    return;
                }

                EventSystem.current.SetSelectedGameObject(results.LayoutArea.GetChild(i).gameObject);
                FocusScrollRectOn(i);
                return;
            }

            EventSystem.current.SetSelectedGameObject(results.LayoutArea.GetChild(0).gameObject);
            FocusScrollRectOn(0);
            if (CardViewer.Instance != null && CardViewer.Instance.SelectedCardModel != null)
                CardViewer.Instance.IsVisible = true;
        }

        private void InputPagePrevious(InputAction.CallbackContext context)
        {
            if (IsBlocked)
                return;

            if (CardViewer.Instance != null && CardViewer.Instance.Zoom)
                SelectLeft();
            else
                PageLeft();
        }

        [UsedImplicitly]
        public void PageLeft()
        {
            results.DecrementPage();
        }

        private void InputPageNext(InputAction.CallbackContext context)
        {
            if (IsBlocked)
                return;

            if (CardViewer.Instance != null && CardViewer.Instance.Zoom)
                SelectRight();
            else
                PageRight();
        }

        [UsedImplicitly]
        public void PageRight()
        {
            results.IncrementPage();
        }

        protected void OnDisable()
        {
            InputSystem.actions.FindAction(Tags.CardsPagePrevious).performed -= InputPagePrevious;
            InputSystem.actions.FindAction(Tags.CardsPageNext).performed -= InputPageNext;
        }
    }
}
