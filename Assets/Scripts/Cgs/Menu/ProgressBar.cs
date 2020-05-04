/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using CardGameDef.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace Cgs.Menu
{
    public class ProgressBar : Modal
    {
        public Image progressBar;
        public Text progressText;

        private UnityCardGame _downloadStatus;

        void Update()
        {
            if (_downloadStatus == null)
            {
                Debug.LogError("ProgressBar::MissingCardGame");
                Hide();
            }

            progressBar.fillAmount = _downloadStatus.DownloadProgress;
            progressText.text = _downloadStatus.DownloadStatus;
        }

        public void Show(UnityCardGame gameToDownload)
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
