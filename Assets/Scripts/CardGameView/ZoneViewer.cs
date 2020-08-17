/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using CardGameDef.Unity;
using UnityEngine;

namespace CardGameView
{
    public class ZoneViewer : MonoBehaviour
    {

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Toggle()
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void AddCard(UnityCard card)
        {
            // TODO:
        }

        public void Clear()
        {
            // TODO:
        }
    }
}
