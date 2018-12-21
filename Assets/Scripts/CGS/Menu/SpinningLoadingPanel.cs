/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;

namespace CGS.Menu
{
    public class SpinningLoadingPanel : MonoBehaviour
    {
        public const float RotateSpeed = 200f;
        public RectTransform progressCircle;

        void Start()
        {
            if (progressCircle == null)
                progressCircle = (RectTransform)transform.GetChild(transform.childCount - 1);
        }

        void Update()
        {
            progressCircle.Rotate(0f, 0f, RotateSpeed * Time.deltaTime);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
