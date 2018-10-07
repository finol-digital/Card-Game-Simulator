/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

using CardGameView;

namespace CGS.EditDeck
{
    public class CardSelector : MonoBehaviour
    {
        public DeckEditor editor;
        public SearchResults results;

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

            if (Input.anyKeyDown)
            {
                if (Input.GetButtonDown(Inputs.Vertical))
                {
                    if (Input.GetAxis(Inputs.Vertical) > 0)
                        SelectUp();
                    else
                        SelectDown();
                }
                else if (Input.GetButtonDown(Inputs.Horizontal))
                {
                    if (Input.GetAxis(Inputs.Horizontal) > 0)
                        SelectRight();
                    else
                        SelectLeft();
                }
                else if (Input.GetButtonDown(Inputs.Column))
                {
                    if (Input.GetAxis(Inputs.Column) > 0)
                        ShiftRight();
                    else
                        ShiftLeft();
                }
                else if (Input.GetButtonDown(Inputs.Page) && !CardInfoViewer.Instance.IsVisible)
                {
                    if (Input.GetAxis(Inputs.Page) > 0)
                        PageRight();
                    else
                        PageLeft();
                }
            }
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
        }

        public void ShiftLeft()
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
        }

        public void ShiftRight()
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
                ShiftLeft();
            else
                PageLeft();
        }

        public void GoRight()
        {
            RectTransform rt = GetComponent<RectTransform>();
            if (rt.rect.width > rt.rect.height)
                ShiftRight();
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
