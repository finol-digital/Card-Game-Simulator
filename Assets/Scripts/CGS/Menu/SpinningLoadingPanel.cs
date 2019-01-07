/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;
using UnityEngine.UI;

namespace CGS.Menu
{
    public class SpinningLoadingPanel : MonoBehaviour
    {
        public const float RotateSpeed = 200f;
        public RectTransform progressCircle;
        public Text progressText;

        void Update()
        {
            progressCircle.Rotate(0f, 0f, RotateSpeed * Time.deltaTime);
            progressText.text = CardGameManager.Current.DownloadStatus;
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
