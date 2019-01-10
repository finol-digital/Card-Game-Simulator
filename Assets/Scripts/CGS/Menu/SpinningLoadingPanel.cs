/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;
using UnityEngine.UI;

using CardGameDef;

namespace CGS.Menu
{
    public class SpinningLoadingPanel : MonoBehaviour
    {
        public const float RotateSpeed = 200f;
        public RectTransform progressCircle;
        public Text progressText;

        private CardGame _downloadStatus;

        void Update()
        {
            if (_downloadStatus == null)
            {
                Debug.LogError("SpinningLoadingPanel::MissingCardGame");
                Hide();
            }

            progressCircle.Rotate(0f, 0f, RotateSpeed * Time.deltaTime);
            progressText.text = _downloadStatus.DownloadStatus;
        }

        public void Show(CardGame gameToDownload)
        {
            gameObject.SetActive(true);
            _downloadStatus = gameToDownload;
        }

        public void Hide()
        {
            _downloadStatus = null;
            gameObject.SetActive(false);
        }
    }
}
