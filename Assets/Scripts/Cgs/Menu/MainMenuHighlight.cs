/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cgs.Menu
{
    public class MainMenuHighlight : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        private Outline _outline;

        private void Start()
        {
            _outline = GetComponent<Outline>();
        }

        public void OnSelect(BaseEventData eventData)
        {
            _outline.enabled = true;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _outline.enabled = false;
        }
    }
}
