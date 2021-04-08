/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using CardGameDef.Unity;
using Cgs.CardGameView;
using UnityEngine;
using UnityEngine.UI;

namespace Cgs.Play
{
    [RequireComponent(typeof(StackViewer))]
    public class HandController : MonoBehaviour
    {
        private static readonly Vector2 ShownPosition = new Vector2(0, 0);
        private static readonly Vector2 HiddenPosition = new Vector2(0, -360);

        public Button downButton;
        public Button upButton;

        private StackViewer _handViewer;

        private void Start()
        {
            _handViewer = GetComponent<StackViewer>();
        }

        private void Update()
        {
            if (CardGameManager.Instance.ModalCanvas != null)
                return;

            if (!Inputs.IsSort)
                return;

            if (upButton.interactable)
                Show();
            else
                Hide();
        }

        public void Show()
        {
            ((RectTransform) transform).anchoredPosition = ShownPosition;
            downButton.interactable = true;
            upButton.interactable = false;
        }

        public void AddCard(UnityCard card)
        {
            _handViewer.AddCard(card);
        }

        public void Clear()
        {
            _handViewer.Clear();
        }

        public void Hide()
        {
            ((RectTransform) transform).anchoredPosition = HiddenPosition;
            downButton.interactable = false;
            upButton.interactable = true;
        }
    }
}
