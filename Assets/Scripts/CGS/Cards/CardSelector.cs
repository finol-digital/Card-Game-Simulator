/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using CardGameView;

namespace CGS.Cards
{
    public class CardSelector : MonoBehaviour
    {
        public SearchResults results;
        public ScrollRect scrollRect;

        private bool _wasDown;
        private bool _wasUp;
        private bool _wasLeft;
        private bool _wasRight;
        private bool _wasPageDown;
        private bool _wasPageUp;
        private bool _wasPageLeft;
        private bool _wasPageRight;

        void Update()
        {
            if (CardGameManager.Instance.ModalCanvas != null || results.inputField.isFocused)
                return;

            if (SwipeManager.DetectSwipe())
            {
                if ((CardViewer.Instance.IsVisible && CardViewer.Instance.Mode == CardViewerMode.Maximal)
                    || CardViewer.Instance.Zoom && CardViewer.Instance.ZoomTime > 0.5f)
                {
                    if (SwipeManager.IsSwipingRight())
                        SelectLeft();
                    else if (SwipeManager.IsSwipingLeft())
                        SelectRight();
                }
                else if (!CardViewer.Instance.Zoom)
                {
                    if (SwipeManager.IsSwipingRight())
                        PageLeft();
                    else if (SwipeManager.IsSwipingLeft())
                        PageRight();
                }
            }

            if (Input.GetButtonDown(Inputs.Vertical) || Input.GetAxis(Inputs.Vertical) != 0)
            {
                if (Input.GetAxis(Inputs.Vertical) < 0 && !_wasDown)
                    SelectDown();
                else if (Input.GetAxis(Inputs.Vertical) > 0 && !_wasUp)
                    SelectUp();
            }
            else if (Input.GetButtonDown(Inputs.Horizontal) || Input.GetAxis(Inputs.Horizontal) != 0)
            {
                if (Input.GetAxis(Inputs.Horizontal) < 0 && !_wasLeft)
                    SelectLeft();
                else if (Input.GetAxis(Inputs.Horizontal) > 0 && !_wasRight)
                    SelectRight();
            }

            if (Input.GetButtonDown(Inputs.PageVertical) || Input.GetAxis(Inputs.PageVertical) != 0)
            {
                if (!CardViewer.Instance.IsVisible || CardViewer.Instance.Mode != CardViewerMode.Maximal)
                {
                    if (Input.GetAxis(Inputs.PageVertical) < 0 && !_wasPageDown)
                        PageDown();
                    else if (Input.GetAxis(Inputs.PageVertical) > 0 && !_wasPageUp)
                        PageUp();
                }
            }
            else if ((Input.GetButtonDown(Inputs.PageHorizontal) || Input.GetAxis(Inputs.PageHorizontal) != 0))
            {
                if (Input.GetAxis(Inputs.PageHorizontal) < 0 && !_wasPageLeft)
                    PageLeft();
                else if (Input.GetAxis(Inputs.PageHorizontal) > 0 && !_wasPageRight)
                    PageRight();
            }

            _wasDown = Input.GetAxis(Inputs.Vertical) < 0;
            _wasUp = Input.GetAxis(Inputs.Vertical) > 0;
            _wasLeft = Input.GetAxis(Inputs.Horizontal) < 0;
            _wasRight = Input.GetAxis(Inputs.Horizontal) > 0;
            _wasPageDown = Input.GetAxis(Inputs.PageVertical) < 0;
            _wasPageUp = Input.GetAxis(Inputs.PageVertical) > 0;
            _wasPageLeft = Input.GetAxis(Inputs.PageHorizontal) < 0;
            _wasPageRight = Input.GetAxis(Inputs.PageHorizontal) > 0;
        }

        public void SelectDown()
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
                if (results.layoutArea.GetChild(i).GetComponent<CardModel>() != CardViewer.Instance.SelectedCardModel)
                    continue;
                i += results.CardsPerRow;
                if (i >= results.layoutArea.childCount)
                {
                    results.IncrementPage();
                    i = 0;
                }
                EventSystem.current.SetSelectedGameObject(results.layoutArea.GetChild(i).gameObject);
                scrollRect.verticalNormalizedPosition = 1.0f - ((float)i / (float)results.layoutArea.childCount);
                return;
            }
            EventSystem.current.SetSelectedGameObject(results.layoutArea.GetChild(0).gameObject);
            scrollRect.verticalNormalizedPosition = 0;
            if (CardViewer.Instance?.SelectedCardModel != null)
                CardViewer.Instance.IsVisible = true;
        }

        public void SelectUp()
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
                if (results.layoutArea.GetChild(i).GetComponent<CardModel>() != CardViewer.Instance.SelectedCardModel)
                    continue;
                i -= results.CardsPerRow;
                if (i < 0)
                {
                    results.DecrementPage();
                    i = results.layoutArea.childCount - 1;
                }
                EventSystem.current.SetSelectedGameObject(results.layoutArea.GetChild(i).gameObject);
                scrollRect.verticalNormalizedPosition = 1.0f - ((float)i / (float)results.layoutArea.childCount);
                return;
            }
            EventSystem.current.SetSelectedGameObject(results.layoutArea.GetChild(0).gameObject);
            scrollRect.verticalNormalizedPosition = 0;
            if (CardViewer.Instance?.SelectedCardModel != null)
                CardViewer.Instance.IsVisible = true;
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
                if (results.layoutArea.GetChild(i).GetComponent<CardModel>() != CardViewer.Instance.SelectedCardModel)
                    continue;
                i--;
                if (i < 0)
                {
                    results.DecrementPage();
                    i = results.layoutArea.childCount - 1;
                }
                EventSystem.current.SetSelectedGameObject(results.layoutArea.GetChild(i).gameObject);
                scrollRect.verticalNormalizedPosition = 1.0f - ((float)i / (float)results.layoutArea.childCount);
                return;
            }
            EventSystem.current.SetSelectedGameObject(results.layoutArea.GetChild(0).gameObject);
            scrollRect.verticalNormalizedPosition = 0;
            if (CardViewer.Instance?.SelectedCardModel != null)
                CardViewer.Instance.IsVisible = true;
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
                if (results.layoutArea.GetChild(i).GetComponent<CardModel>() != CardViewer.Instance.SelectedCardModel)
                    continue;
                i++;
                if (i == results.layoutArea.childCount)
                {
                    results.IncrementPage();
                    i = 0;
                }
                EventSystem.current.SetSelectedGameObject(results.layoutArea.GetChild(i).gameObject);
                scrollRect.verticalNormalizedPosition = 1.0f - ((float)i / (float)results.layoutArea.childCount);
                return;
            }
            EventSystem.current.SetSelectedGameObject(results.layoutArea.GetChild(0).gameObject);
            scrollRect.verticalNormalizedPosition = 0;
            if (CardViewer.Instance?.SelectedCardModel != null)
                CardViewer.Instance.IsVisible = true;
        }

        public void PageDown()
        {
            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition + 0.1f);
        }

        public void PageUp()
        {
            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition - 0.1f);
        }

        public void PageLeft()
        {
            results.DecrementPage();
        }

        public void PageRight()
        {
            results.IncrementPage();
        }
    }
}
