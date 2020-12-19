/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Linq;
using Cgs.CardGameView;
using Cgs.CardGameView.Multiplayer;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cgs.Cards
{
    public class CardSelector : MonoBehaviour
    {
        private const float GameSelectorHeight = 160;

        public SearchResults results;
        public ScrollRect scrollRect;

        private void Update()
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
                    if (Input.touches.All(touch => touch.position.y > Screen.height - GameSelectorHeight))
                    {
                        if (SwipeManager.IsSwipingRight())
                            CardGameManager.Instance.Select(CardGameManager.Instance.Previous.Id);
                        else if (SwipeManager.IsSwipingLeft())
                            CardGameManager.Instance.Select(CardGameManager.Instance.Next.Id);
                    }
                    else
                    {
                        if (SwipeManager.IsSwipingRight())
                            PageLeft();
                        else if (SwipeManager.IsSwipingLeft())
                            PageRight();
                    }
                }
            }

            if (Inputs.IsVertical)
            {
                if (Inputs.IsDown && !Inputs.WasDown)
                    SelectDown();
                else if (Inputs.IsUp && !Inputs.WasUp)
                    SelectUp();
            }
            else if (Inputs.IsHorizontal)
            {
                if (Inputs.IsLeft && !Inputs.WasLeft)
                    SelectLeft();
                else if (Inputs.IsRight && !Inputs.WasRight)
                    SelectRight();
            }

            if (Inputs.IsPageVertical)
            {
                if (CardViewer.Instance.IsVisible && CardViewer.Instance.Mode == CardViewerMode.Maximal)
                    return;
                if (Inputs.IsPageDown && !Inputs.WasPageDown)
                    PageDown();
                else if (Inputs.IsPageUp && !Inputs.WasPageUp)
                    PageUp();
            }
            else if (Inputs.IsPageHorizontal)
            {
                if (Inputs.IsPageLeft && !Inputs.WasPageLeft)
                    PageLeft();
                else if (Inputs.IsPageRight && !Inputs.WasPageRight)
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
                scrollRect.verticalNormalizedPosition = 1.0f - ((float) i / results.layoutArea.childCount);
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
                scrollRect.verticalNormalizedPosition = 1.0f - ((float) i / results.layoutArea.childCount);
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
                scrollRect.verticalNormalizedPosition = 1.0f - ((float) i / results.layoutArea.childCount);
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
                scrollRect.verticalNormalizedPosition = 1.0f - ((float) i / results.layoutArea.childCount);
                return;
            }

            EventSystem.current.SetSelectedGameObject(results.layoutArea.GetChild(0).gameObject);
            scrollRect.verticalNormalizedPosition = 0;
            if (CardViewer.Instance != null && CardViewer.Instance.SelectedCardModel != null)
                CardViewer.Instance.IsVisible = true;
        }

        [UsedImplicitly]
        public void PageDown()
        {
            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition + 0.1f);
        }

        [UsedImplicitly]
        public void PageUp()
        {
            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition - 0.1f);
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
