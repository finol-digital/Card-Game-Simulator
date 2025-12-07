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
    public class CardSelector : MonoBehaviour, IDragHandler, IEndDragHandler
    {
        public DeckEditor editor;
        public SearchResults results;

        private void OnEnable()
        {
            InputSystem.actions.FindAction(Tags.PlayerMove).performed += InputMove;
            InputSystem.actions.FindAction(Tags.PlayerPage).performed += InputPage;
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Just required by the interface to get OnEndDrag called
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (CardGameManager.Instance.ModalCanvas != null || editor.searchResults.inputField.isFocused
                                                             || !CardViewer.Instance.Zoom ||
                                                             CardViewer.Instance.ZoomTime <= 0.5f)
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

        private void InputMove(InputAction.CallbackContext callbackContext)
        {
            if (CardGameManager.Instance.ModalCanvas != null || editor.searchResults.inputField.isFocused)
                return;

            var moveVector2 = InputSystem.actions.FindAction(Tags.PlayerMove).ReadValue<Vector2>();
            if (moveVector2.y > 0)
                SelectEditorDown();
            else if (moveVector2.y < 0)
                SelectEditorUp();
            else if (moveVector2.x < 0)
                SelectEditorLeft();
            else if (moveVector2.x > 0)
                SelectEditorRight();
        }

        private void InputPage(InputAction.CallbackContext callbackContext)
        {
            if (CardGameManager.Instance.ModalCanvas != null || editor.searchResults.inputField.isFocused)
                return;

            var pageVector2 = InputSystem.actions.FindAction(Tags.PlayerPage).ReadValue<Vector2>();
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

        private void SelectEditorLeft()
        {
            if (EventSystem.current.alreadySelecting)
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
                i--;
                if (i < 0)
                    i = editorCardModels.Count - 1;
                EventSystem.current.SetSelectedGameObject(editorCardModels[i].gameObject);
                editor.FocusScrollRectOn(editorCardModels[i]);
                return;
            }

            EventSystem.current.SetSelectedGameObject(editorCardModels[^1].gameObject);
            editor.FocusScrollRectOn(editorCardModels[^1]);
            if (CardViewer.Instance != null && CardViewer.Instance.SelectedCardModel != null)
                CardViewer.Instance.IsVisible = true;
        }

        private void SelectEditorRight()
        {
            if (EventSystem.current.alreadySelecting)
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
                i++;
                if (i == editorCardModels.Count)
                    i = 0;
                EventSystem.current.SetSelectedGameObject(editorCardModels[i].gameObject);
                editor.FocusScrollRectOn(editorCardModels[i]);
                return;
            }

            EventSystem.current.SetSelectedGameObject(editorCardModels[0].gameObject);
            editor.FocusScrollRectOn(editorCardModels[0]);
            if (CardViewer.Instance != null && CardViewer.Instance.SelectedCardModel != null)
                CardViewer.Instance.IsVisible = true;
        }

        private void SelectEditorDown()
        {
            if (EventSystem.current.alreadySelecting)
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
                i += DeckEditor.CardsPerZoneHorizontal;
                if (i >= editorCardModels.Count)
                    i = 0;
                EventSystem.current.SetSelectedGameObject(editorCardModels[i].gameObject);
                editor.FocusScrollRectOn(editorCardModels[i]);
                return;
            }

            editor.scrollRect.verticalNormalizedPosition = 1;
            EventSystem.current.SetSelectedGameObject(editorCardModels[0].gameObject);
            if (CardViewer.Instance != null && CardViewer.Instance.SelectedCardModel != null)
                CardViewer.Instance.IsVisible = true;
        }

        private void SelectEditorUp()
        {
            if (EventSystem.current.alreadySelecting)
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
                i -= DeckEditor.CardsPerZoneHorizontal;
                if (i < 0)
                    i = editorCardModels.Count - 1;
                EventSystem.current.SetSelectedGameObject(editorCardModels[i].gameObject);
                editor.FocusScrollRectOn(editorCardModels[i]);
                return;
            }

            editor.scrollRect.verticalNormalizedPosition = 0;
            EventSystem.current.SetSelectedGameObject(editorCardModels[^1].gameObject);
            if (CardViewer.Instance != null && CardViewer.Instance.SelectedCardModel != null)
                CardViewer.Instance.IsVisible = true;
        }

        [UsedImplicitly]
        public void NavigateLeft()
        {
            results.DecrementPage();
        }

        [UsedImplicitly]
        public void NavigateRight()
        {
            results.IncrementPage();
        }

        private void SelectResultsLeft()
        {
            results.DecrementPage();
        }

        private void SelectResultsRight()
        {
            results.IncrementPage();
        }

        private void SelectResultsDown()
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
                return;
            }

            EventSystem.current.SetSelectedGameObject(results.layoutArea.GetChild(0).gameObject);
            if (CardViewer.Instance != null && CardViewer.Instance.SelectedCardModel != null)
                CardViewer.Instance.IsVisible = true;
        }

        private void SelectResultsUp()
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
                return;
            }

            EventSystem.current.SetSelectedGameObject(results.layoutArea.GetChild(0).gameObject);
            if (CardViewer.Instance != null && CardViewer.Instance.SelectedCardModel != null)
                CardViewer.Instance.IsVisible = true;
        }

        private void OnDisable()
        {
            InputSystem.actions.FindAction(Tags.PlayerMove).performed -= InputMove;
            InputSystem.actions.FindAction(Tags.PlayerPage).performed -= InputPage;
        }
    }
}
