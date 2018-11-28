/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

using CardGameView;

namespace CGS.Decks
{
    public class CardSelector : MonoBehaviour
    {
        public DeckEditor editor;
        public SearchResults results;

        private bool _wasDown;
        private bool _wasUp;
        private bool _wasLeft;
        private bool _wasRight;
        private bool _wasPageLeft;
        private bool _wasPageRight;
        private bool _wasPageDown;
        private bool _wasPageUp;

        void Update()
        {
            if (CardGameManager.Instance.TopMenuCanvas != null || editor.searchResults.nameInputField.isFocused)
                return;

            if (CardInfoViewer.Instance.zoomPanel.gameObject.activeSelf && SwipeManager.DetectSwipe())
            {
                if (SwipeManager.IsSwipingDown())
                    SelectUp();
                else if (SwipeManager.IsSwipingUp())
                    SelectDown();
                else if (SwipeManager.IsSwipingRight())
                    SelectLeft();
                else if (SwipeManager.IsSwipingLeft())
                    SelectRight();
            }

            if (Input.GetButtonDown(Inputs.Vertical) || Input.GetAxis(Inputs.Vertical) != 0)
            {
                if (Input.GetAxis(Inputs.Vertical) > 0 && !_wasUp)
                    SelectUp();
                else if (Input.GetAxis(Inputs.Vertical) < 0 && !_wasDown)
                    SelectDown();
            }
            else if (Input.GetButtonDown(Inputs.Horizontal) || Input.GetAxis(Inputs.Horizontal) != 0)
            {
                if (Input.GetAxis(Inputs.Horizontal) > 0 && !_wasRight)
                    SelectRight();
                else if (Input.GetAxis(Inputs.Horizontal) < 0 && !_wasLeft)
                    SelectLeft();
            }

            if ((Input.GetButtonDown(Inputs.PageVertical) || Input.GetAxis(Inputs.PageVertical) != 0) && !CardInfoViewer.Instance.IsVisible)
            {
                if (Input.GetAxis(Inputs.PageHorizontal) > 0 && !_wasPageUp)
                    PageUp();
                else if (Input.GetAxis(Inputs.PageHorizontal) < 0 && !_wasPageDown)
                    PageDown();
            }
            else if (Input.GetButtonDown(Inputs.PageHorizontal) || Input.GetAxis(Inputs.PageHorizontal) != 0)
            {
                if (Input.GetAxis(Inputs.PageVertical) > 0 && !_wasPageRight)
                    PageRight();
                else if (Input.GetAxis(Inputs.PageVertical) < 0 && !_wasPageLeft)
                    PageLeft();
            }

            _wasDown = Input.GetAxis(Inputs.Vertical) < 0;
            _wasUp = Input.GetAxis(Inputs.Vertical) > 0;
            _wasLeft = Input.GetAxis(Inputs.Horizontal) < 0;
            _wasRight = Input.GetAxis(Inputs.Horizontal) > 0;
            _wasPageLeft = Input.GetAxis(Inputs.PageVertical) < 0;
            _wasPageRight = Input.GetAxis(Inputs.PageVertical) > 0;
            _wasPageDown = Input.GetAxis(Inputs.PageHorizontal) < 0;
            _wasPageUp = Input.GetAxis(Inputs.PageHorizontal) > 0;
        }

        public void SelectDown()
        {
            if (EventSystem.current.alreadySelecting)
                return;

            List<CardModel> editorCards = editor.CardModels;
            if (editorCards.Count < 1)
            {
                EventSystem.current.SetSelectedGameObject(null);
                return;
            }

            for (int i = 0; i < editorCards.Count; i++)
            {
                if (editorCards[i] != CardInfoViewer.Instance.SelectedCardModel)
                    continue;
                i++;
                if (i == editorCards.Count)
                    i = 0;
                EventSystem.current.SetSelectedGameObject(editorCards[i].gameObject);
                return;
            }
            EventSystem.current.SetSelectedGameObject(editorCards[0].gameObject);
            if (CardInfoViewer.Instance?.SelectedCardModel != null)
                CardInfoViewer.Instance.IsVisible = true;
        }

        public void SelectUp()
        {
            if (EventSystem.current.alreadySelecting)
                return;

            List<CardModel> editorCards = editor.CardModels;
            if (editorCards.Count < 1)
            {
                EventSystem.current.SetSelectedGameObject(null);
                return;
            }

            for (int i = editorCards.Count - 1; i >= 0; i--)
            {
                if (editorCards[i] != CardInfoViewer.Instance.SelectedCardModel)
                    continue;
                i--;
                if (i < 0)
                    i = editorCards.Count - 1;
                EventSystem.current.SetSelectedGameObject(editorCards[i].gameObject);
                return;
            }
            EventSystem.current.SetSelectedGameObject(editorCards[editorCards.Count - 1].gameObject);
            if (CardInfoViewer.Instance?.SelectedCardModel != null)
                CardInfoViewer.Instance.IsVisible = true;
        }

        public void SelectLeft()
        {
            if (EventSystem.current.alreadySelecting)
                return;

            if (results.layoutArea.childCount < 1)
            {
                EventSystem.current.SetSelectedGameObject(null);
                return;
            }

            for (int i = results.layoutArea.childCount - 1; i >= 0; i--)
            {
                if (results.layoutArea.GetChild(i).GetComponent<CardModel>() != CardInfoViewer.Instance.SelectedCardModel)
                    continue;
                i--;
                if (i < 0)
                {
                    results.PageLeft();
                    i = results.layoutArea.childCount - 1;
                }
                EventSystem.current.SetSelectedGameObject(results.layoutArea.GetChild(i).gameObject);
                return;
            }
            EventSystem.current.SetSelectedGameObject(results.layoutArea.GetChild(0).gameObject);
            if (CardInfoViewer.Instance?.SelectedCardModel != null)
                CardInfoViewer.Instance.IsVisible = true;
        }

        public void SelectRight()
        {
            if (EventSystem.current.alreadySelecting)
                return;

            if (results.layoutArea.childCount < 1)
            {
                EventSystem.current.SetSelectedGameObject(null);
                return;
            }

            for (int i = 0; i < results.layoutArea.childCount; i++)
            {
                if (results.layoutArea.GetChild(i).GetComponent<CardModel>() != CardInfoViewer.Instance.SelectedCardModel)
                    continue;
                i++;
                if (i == results.layoutArea.childCount)
                {
                    results.PageRight();
                    i = 0;
                }
                EventSystem.current.SetSelectedGameObject(results.layoutArea.GetChild(i).gameObject);
                return;
            }
            EventSystem.current.SetSelectedGameObject(results.layoutArea.GetChild(0).gameObject);
            if (CardInfoViewer.Instance?.SelectedCardModel != null)
                CardInfoViewer.Instance.IsVisible = true;
        }

        public void PageDown()
        {
            if (EventSystem.current.alreadySelecting)
                return;

            List<CardModel> editorCards = editor.CardModels;
            if (editorCards.Count < 1)
            {
                EventSystem.current.SetSelectedGameObject(null);
                return;
            }

            Transform startParent = null;
            for (int i = editorCards.Count - 1; i >= 0; i--)
            {
                if (startParent == null && editorCards[i] != CardInfoViewer.Instance.SelectedCardModel)
                    continue;
                if (editorCards[i] == CardInfoViewer.Instance.SelectedCardModel)
                    startParent = editorCards[i].transform.parent;
                if (startParent != editorCards[i].transform.parent)
                {
                    editor.scrollRect.horizontalNormalizedPosition = editorCards[i].transform.parent.GetSiblingIndex() / (editorCards[i].transform.parent.parent.childCount - 1f);
                    EventSystem.current.SetSelectedGameObject(editorCards[i].gameObject);
                    return;
                }
            }
            editor.scrollRect.horizontalNormalizedPosition = 1;
            EventSystem.current.SetSelectedGameObject(editorCards[editorCards.Count - 1].gameObject);
            if (CardInfoViewer.Instance?.SelectedCardModel != null)
                CardInfoViewer.Instance.IsVisible = true;
        }

        public void PageUp()
        {
            if (EventSystem.current.alreadySelecting)
                return;

            List<CardModel> editorCards = editor.CardModels;
            if (editorCards.Count < 1)
            {
                EventSystem.current.SetSelectedGameObject(null);
                return;
            }

            Transform startParent = null;
            for (int i = 0; i < editorCards.Count; i++)
            {
                if (startParent == null && editorCards[i] != CardInfoViewer.Instance.SelectedCardModel)
                    continue;
                if (editorCards[i] == CardInfoViewer.Instance.SelectedCardModel)
                    startParent = editorCards[i].transform.parent;
                if (startParent != editorCards[i].transform.parent)
                {
                    editor.scrollRect.horizontalNormalizedPosition = editorCards[i].transform.parent.GetSiblingIndex() / (editorCards[i].transform.parent.parent.childCount - 1f);
                    EventSystem.current.SetSelectedGameObject(editorCards[i].gameObject);
                    return;
                }
            }
            editor.scrollRect.horizontalNormalizedPosition = 0;
            EventSystem.current.SetSelectedGameObject(editorCards[0].gameObject);
            if (CardInfoViewer.Instance?.SelectedCardModel != null)
                CardInfoViewer.Instance.IsVisible = true;
        }

        public void PageLeft()
        {
            results.PageLeft();
        }

        public void PageRight()
        {
            results.PageRight();
        }

        public void GoLeft()
        {
            RectTransform rt = GetComponent<RectTransform>();
            if (rt.rect.width > rt.rect.height)
                PageDown();
            else
                PageLeft();
        }

        public void GoRight()
        {
            RectTransform rt = GetComponent<RectTransform>();
            if (rt.rect.width > rt.rect.height)
                PageUp();
            else
                PageRight();
        }

        public void MoveLeft()
        {
            RectTransform rt = GetComponent<RectTransform>();
            if (rt.rect.width > rt.rect.height)
                PageLeft();
            else
                SelectLeft();
        }

        public void MoveRight()
        {
            RectTransform rt = GetComponent<RectTransform>();
            if (rt.rect.width > rt.rect.height)
                PageRight();
            else
                SelectRight();
        }
    }
}
