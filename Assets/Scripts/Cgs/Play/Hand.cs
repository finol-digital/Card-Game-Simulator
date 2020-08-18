/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using CardGameView;
using JetBrains.Annotations;
using UnityEngine;

namespace Cgs.Play
{
    public class Hand : ZoneViewer
    {
        private static readonly Vector2 ShownPosition = new Vector2(0, 0);
        private static readonly Vector2 HiddenPosition = new Vector2(0, -360);

        public Transform toggle;

        public void Show()
        {
            ((RectTransform) transform).anchoredPosition = ShownPosition;
            toggle.rotation = Quaternion.identity;
        }

        [UsedImplicitly]
        public void Toggle()
        {
            var rectTransform = (RectTransform) transform;
            bool wasShown = ShownPosition.Equals(rectTransform.anchoredPosition);
            rectTransform.anchoredPosition = wasShown ? HiddenPosition : ShownPosition;
            toggle.rotation = wasShown ? Quaternion.Euler(0, 0, 180) : Quaternion.identity;
        }

        public void Hide()
        {
            ((RectTransform) transform).anchoredPosition = HiddenPosition;
            toggle.rotation = Quaternion.Euler(0, 0, 180);
        }
    }
}
