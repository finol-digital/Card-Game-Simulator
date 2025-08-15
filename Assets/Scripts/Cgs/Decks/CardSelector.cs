/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Linq;
using Cgs.CardGameView.Multiplayer;
using Cgs.CardGameView.Viewer;
using Cgs.Cards;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Cgs.Decks
{
    public class CardSelector : MonoBehaviour
    {
        public DeckEditor editor;
        public SearchResults results;

        private void Update()
        {
            if (CardGameManager.Instance.ModalCanvas != null || editor.searchResults.inputField.isFocused)
                return;

            if (CardViewer.Instance.Zoom && CardViewer.Instance.ZoomTime > 0.5f && SwipeManager.DetectSwipe())
            {
                if (SwipeManager.IsSwipingUp())
                    SelectEditorDown();
                else if (SwipeManager.IsSwipingDown())
                    SelectEditorUp();
                else if (SwipeManager.IsSwipingRight())
                    SelectEditorLeft();
                else if (SwipeManager.IsSwipingLeft())
                    SelectEditorRight();
            }

            if (Inputs.IsVertical)
            {
                if (Inputs.IsDown && !Inputs.WasDown)
                    SelectEditorDown();
                else if (Inputs.IsUp && !Inputs.WasUp)
                    SelectEditorUp();
            }
            else if (Inputs.IsHorizontal)
            {
                if (Inputs.IsLeft && !Inputs.WasLeft)
                    SelectEditorLeft();
                else if (Inputs.IsRight && !Inputs.WasRight)
                    SelectEditorRight();
            }

            if (Inputs.IsPageVertical)
            {
                if (Inputs.IsPageDown && !Inputs.WasPageDown)
                    SelectResultsDown();
                else if (Inputs.IsPageUp && !Inputs.WasPageUp)
                    SelectResultsUp();
            }
            else if (Inputs.IsPageHorizontal)
            {
                if (Inputs.IsPageLeft && !Inputs.WasPageLeft)
                    SelectResultsLeft();
                else if (Inputs.IsPageRight && !Inputs.WasPageRight)
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

            Transform startParent = null;
            foreach (var cardModel in editorCardModels.Where(t =>
                         startParent != null || t == CardViewer.Instance.SelectedCardModel))
            {
                if (cardModel == CardViewer.Instance.SelectedCardModel)
                    startParent = cardModel.transform.parent;
                if (startParent == cardModel.transform.parent)
                    continue;
                EventSystem.current.SetSelectedGameObject(cardModel.gameObject);
                editor.FocusScrollRectOn(cardModel);
                return;
            }

            editor.scrollRect.horizontalNormalizedPosition = 0;
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

            Transform startParent = null;
            for (var i = editorCardModels.Count - 1; i >= 0; i--)
            {
                if (startParent == null && editorCardModels[i] != CardViewer.Instance.SelectedCardModel)
                    continue;
                if (editorCardModels[i] == CardViewer.Instance.SelectedCardModel)
                    startParent = editorCardModels[i].transform.parent;
                if (startParent == editorCardModels[i].transform.parent)
                    continue;
                EventSystem.current.SetSelectedGameObject(editorCardModels[i].gameObject);
                editor.FocusScrollRectOn(editorCardModels[i]);
                return;
            }

            editor.scrollRect.horizontalNormalizedPosition = 1;
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
    }
}
