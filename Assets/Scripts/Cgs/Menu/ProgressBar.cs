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

        private UnityCardGame _game;

        private void Update()
        {
            if (_game == null)
            {
                Debug.LogError("ProgressBar::MissingCardGame");
                Hide();
            }

            progressBar.fillAmount = _game.DownloadProgress;
            progressText.text = _game.DownloadStatus;
        }

        public void Show(UnityCardGame gameToDownload)
        {
            Show();
            _game = gameToDownload;
        }

        public override void Hide()
        {
            _game = null;
            base.Hide();
        }
    }
}
