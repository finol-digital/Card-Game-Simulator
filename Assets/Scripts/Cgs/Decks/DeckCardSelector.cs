/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.CardGameView.Multiplayer;
using Cgs.CardGameView.Viewer;
using Cgs.Cards;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Cgs.Decks
{
    public class DeckCardSelector : MonoBehaviour, IDragHandler, IEndDragHandler
    {
        public DeckEditor editor;
        public SearchResults results;

        private InputAction _moveAction;
        private InputAction _pageAction;

        private bool IsBlocked =>
            CardGameManager.Instance.ModalCanvas != null || editor.searchResults.inputField.isFocused;

        private bool IsSelectedCardInDeck =>
            CardViewer.Instance != null && CardViewer.Instance.SelectedCardModel != null &&
            editor.CardModels.Contains(CardViewer.Instance.SelectedCardModel);

        private void OnEnable()
        {
            InputSystem.actions.FindAction(Tags.CardsPagePrevious).performed += InputPagePrevious;
            InputSystem.actions.FindAction(Tags.CardsPageNext).performed += InputPageNext;
        }

        private void Start()
        {
            CardViewer.Instance.buttonsPanel.gameObject.SetActive(true);
            CardViewer.Instance.previousButton.onClick.AddListener(SelectPrevious);
            CardViewer.Instance.nextButton.onClick.AddListener(SelectNext);

            _moveAction = InputSystem.actions.FindAction(Tags.PlayerMove);
            _pageAction = InputSystem.actions.FindAction(Tags.PlayerPage);
        }

        // Poll for Vector2 inputs
        private void Update()
        {
            if (IsBlocked)
                return;

            if (_moveAction?.WasPressedThisFrame() ?? false)
            {
                var moveVector2 = _moveAction.ReadValue<Vector2>();
                if (moveVector2.y < 0)
                    SelectEditorDown();
                else if (moveVector2.y > 0)
                    SelectEditorUp();
                else if (moveVector2.x < 0)
                    SelectEditorLeft();
                else if (moveVector2.x > 0)
                    SelectEditorRight();
            }
            else if (_pageAction?.WasPressedThisFrame() ?? false)
            {
                var pageVector2 = _pageAction.ReadValue<Vector2>();
                if (Mathf.Abs(pageVector2.y) > 0)
                {
                    if (pageVector2.y < 0)
                        SelectResultsDown();
                    else if (pageVector2.y > 0)
                        SelectResultsUp();
                }
                else
                {
                    if (pageVector2.x < 0)
                        SelectResultsLeft();
                    else if (pageVector2.x > 0)
                        SelectResultsRight();
                }
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Just required by the interface to get OnEndDrag called
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (IsBlocked || !CardViewer.Instance.Zoom || CardViewer.Instance.ZoomTime <= 0.5f)
                return;

            var dragDelta = eventData.position - eventData.pressPosition;
            var swipeDirection = UnityExtensionMethods.UnityExtensionMethods.GetSwipeDirection(dragDelta);
            switch (swipeDirection)
            {
                case UnityExtensionMethods.UnityExtensionMethods.SwipeDirection.Up:
                    SelectEditorDown();
                    break;
                case UnityExtensionMethods.UnityExtensionMethods.SwipeDirection.Down:
                    SelectEditorUp();
                    break;
                case UnityExtensionMethods.UnityExtensionMethods.SwipeDirection.Right:
                    SelectEditorLeft();
                    break;
                case UnityExtensionMethods.UnityExtensionMethods.SwipeDirection.Left:
                    SelectEditorRight();
                    break;
                case UnityExtensionMethods.UnityExtensionMethods.SwipeDirection.None:
                default:
                    Debug.Log("Swipe direction none or unrecognized.");
                    break;
            }
        }

        // CardModels is ordered along each zone: left-to-right within horizontal zones,
        // top-to-bottom within vertical zones, so the stride for each direction depends on the layout
        private void SelectEditorLeft() =>
            SelectEditorPrevious(editor.IsHorizontalLayout ? DeckEditor.CardsPerZoneVertical : 1);

        private void SelectEditorRight() =>
            SelectEditorNext(editor.IsHorizontalLayout ? DeckEditor.CardsPerZoneVertical : 1);

        private void SelectEditorDown() =>
            SelectEditorNext(editor.IsHorizontalLayout ? 1 : DeckEditor.CardsPerZoneHorizontal);

        private void SelectEditorUp() =>
            SelectEditorPrevious(editor.IsHorizontalLayout ? 1 : DeckEditor.CardsPerZoneHorizontal);

        private void SelectEditorPrevious(int stride)
        {
            if (IsBlocked || EventSystem.current.alreadySelecting)
                return;

            var editorCardModels = editor.CardModels;
            if (editorCardModels.Count < 1)
            {
                EventSystem.current.SetSelectedGameObject(null);
                return;
            }

            for (var i = editorCardModels.Count - 1; i >= 0; i--)
            {
                if (editorCardModels[i] != CardViewer.Instance.SelectedCardModel)
                    continue;
                var previous = i - stride;
                if (previous < 0)
                    previous = editorCardModels.Count - 1;
                EventSystem.current.SetSelectedGameObject(editorCardModels[previous].gameObject);
                editor.FocusScrollRectOn(editorCardModels[previous]);
                return;
            }

            EventSystem.current.SetSelectedGameObject(editorCardModels[^1].gameObject);
            editor.FocusScrollRectOn(editorCardModels[^1]);
            if (CardViewer.Instance != null && CardViewer.Instance.SelectedCardModel != null)
                CardViewer.Instance.IsVisible = true;
        }

        private void SelectEditorNext(int stride)
        {
            if (IsBlocked || EventSystem.current.alreadySelecting)
                return;

            var editorCardModels = editor.CardModels;
            if (editorCardModels.Count < 1)
            {
                EventSystem.current.SetSelectedGameObject(null);
                return;
            }

            for (var i = 0; i < editorCardModels.Count; i++)
            {
                if (editorCardModels[i] != CardViewer.Instance.SelectedCardModel)
                    continue;
                var next = i + stride;
                if (next >= editorCardModels.Count)
                    next = 0;
                EventSystem.current.SetSelectedGameObject(editorCardModels[next].gameObject);
                editor.FocusScrollRectOn(editorCardModels[next]);
                return;
            }

            EventSystem.current.SetSelectedGameObject(editorCardModels[0].gameObject);
            editor.FocusScrollRectOn(editorCardModels[0]);
            if (CardViewer.Instance != null && CardViewer.Instance.SelectedCardModel != null)
                CardViewer.Instance.IsVisible = true;
        }

        private void InputPagePrevious(InputAction.CallbackContext context)
        {
            if (IsBlocked)
                return;

            if (CardViewer.Instance != null && CardViewer.Instance.Zoom)
                SelectPrevious();
            else
                NavigateLeft();
        }

        private void SelectPrevious()
        {
            if (IsSelectedCardInDeck)
                SelectEditorPrevious(1);
            else
                SelectResultsLeft();
        }

        [UsedImplicitly]
        public void NavigateLeft()
        {
            results.DecrementPage();
        }

        private void InputPageNext(InputAction.CallbackContext context)
        {
            if (IsBlocked)
                return;

            if (CardViewer.Instance != null && CardViewer.Instance.Zoom)
                SelectNext();
            else
                NavigateRight();
        }

        private void SelectNext()
        {
            if (IsSelectedCardInDeck)
                SelectEditorNext(1);
            else
                SelectResultsRight();
        }

        [UsedImplicitly]
        public void NavigateRight()
        {
            results.IncrementPage();
        }

        private void SelectResultsLeft()
        {
            SelectResults(-1);
        }

        private void SelectResultsRight()
        {
            SelectResults(1);
        }

        private void SelectResultsDown()
        {
            SelectResults(Mathf.Max(1, results.CardsPerRow));
        }

        private void SelectResultsUp()
        {
            SelectResults(-Mathf.Max(1, results.CardsPerRow));
        }

        private void SelectResults(int step)
        {
            if (step == 0 || IsBlocked || EventSystem.current.alreadySelecting)
                return;

            var childCount = results.layoutArea.childCount;
            if (childCount < 1)
            {
                EventSystem.current.SetSelectedGameObject(null);
                return;
            }

            var selectedIndex = -1;
            for (var i = 0; i < childCount; i++)
            {
                if (results.layoutArea.GetChild(i).GetComponent<CardModel>() != CardViewer.Instance.SelectedCardModel)
                    continue;
                selectedIndex = i;
                break;
            }

            if (selectedIndex < 0)
            {
                EventSystem.current.SetSelectedGameObject(results.layoutArea.GetChild(0).gameObject);
                if (CardViewer.Instance != null && CardViewer.Instance.SelectedCardModel != null)
                    CardViewer.Instance.IsVisible = true;
                return;
            }

            var target = selectedIndex + step;
            if (target >= childCount)
            {
                results.IncrementPage();
                childCount = results.layoutArea.childCount;
                target = 0;
            }
            else if (target < 0)
            {
                results.DecrementPage();
                childCount = results.layoutArea.childCount;
                target = childCount - 1;
            }

            if (target < 0 || target >= childCount)
            {
                EventSystem.current.SetSelectedGameObject(null);
                return;
            }

            EventSystem.current.SetSelectedGameObject(results.layoutArea.GetChild(target).gameObject);
        }

        private void OnDisable()
        {
            InputSystem.actions.FindAction(Tags.CardsPagePrevious).performed -= InputPagePrevious;
            InputSystem.actions.FindAction(Tags.CardsPageNext).performed -= InputPageNext;
        }
    }
}
