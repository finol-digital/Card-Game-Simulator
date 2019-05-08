/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;
using UnityEngine.UI;

namespace CGS.Menu
{
    [RequireComponent(typeof(Canvas))]
    public class Modal : MonoBehaviour
    {
        public bool IsFocused
        {
            get { return CardGameManager.Instance.ModalCanvas?.gameObject == gameObject; }
        }

        void Start()
        {
            CardGameManager.Instance.ModalCanvases.Add(GetComponent<Canvas>());
            OnStart();
        }

        protected virtual void OnStart()
        {
            // Override in child classes if needed
        }
    }
}
