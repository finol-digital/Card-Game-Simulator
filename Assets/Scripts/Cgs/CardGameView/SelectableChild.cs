/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;
using UnityEngine.EventSystems;

namespace Cgs.CardGameView
{
    public class SelectableChild : MonoBehaviour, IPointerDownHandler
    {
        public GameObject parentObject;

        public void OnPointerDown(PointerEventData eventData)
        {
            EventSystem.current.SetSelectedGameObject(parentObject, eventData);
        }
    }
}
