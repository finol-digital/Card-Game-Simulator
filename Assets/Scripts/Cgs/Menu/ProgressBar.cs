/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using FinolDigital.Cgs.Json.Unity;
using Cgs.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace Cgs.Menu
{
    public class ProgressBar : Modal
    {
        [SerializeField] Image progressBar;
        [SerializeField] Text progressText;

        private UnityCardGame _game;
        private IProgressible _progressible;
        private UiMessage _progressMessage;
        private string _progressLiteral;
        private GameDownloadStage _stage;
        private int _count = -1;
        private int _total = -1;

        protected void Update()
        {
            if (_game != null)
            {
                progressBar.fillAmount = _game.DownloadProgress;
                if (_stage != _game.DownloadStage || _count != _game.DownloadItemCount || _total != _game.DownloadItemTotal)
                {
                    _stage = _game.DownloadStage;
                    _count = _game.DownloadItemCount;
                    _total = _game.DownloadItemTotal;
                    var key = _stage switch
                    {
                        GameDownloadStage.Specification => "download.specification",
                        GameDownloadStage.Banner => "download.banner",
                        GameDownloadStage.CardBack => "download.back",
                        GameDownloadStage.CardBacks => "download.backs",
                        GameDownloadStage.Playmat => "download.playmat",
                        GameDownloadStage.Boards => "download.boards",
                        GameDownloadStage.Decks => "download.decks",
                        GameDownloadStage.DeckProgress => "download.decks.progress",
                        GameDownloadStage.Sets => "download.sets",
                        GameDownloadStage.Cards => "download.cards",
                        GameDownloadStage.Complete => "download.complete",
                        _ => "common.downloading"
                    };
                    LocalizedUiText.Set(progressText, UiMessage.With(key, _game.DownloadStatus,
                        ("count", _count), ("total", _total)));
                }
            }
            else if (_progressible != null)
            {
                progressBar.fillAmount = _progressible.ProgressPercentage;
                var message = _progressible.LocalizedProgressStatus;
                if (_progressMessage != message || _progressLiteral != _progressible.ProgressStatus)
                {
                    _progressMessage = message;
                    _progressLiteral = _progressible.ProgressStatus;
                    if (message != null)
                        LocalizedUiText.Set(progressText, message);
                    else
                        LocalizedUiText.SetLiteral(progressText, _progressLiteral);
                }
            }
            else
            {
                Debug.LogError("ProgressBar::MissingIProgressible");
                Hide();
            }
        }

        public void Show(UnityCardGame gameToDownload)
        {
            Show();
            _game = gameToDownload;
            _progressible = null;
            _count = -1;
        }

        public void Show(IProgressible progressible)
        {
            Show();
            _progressible = progressible;
            _progressMessage = null;
            _progressLiteral = null;
            _game = null;
        }

        public override void Hide()
        {
            _game = null;
            _progressible = null;
            base.Hide();
        }
    }
}
