/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;
using UnityEngine.UI;
using CGS.Play.Multiplayer;

namespace CGS.Play
{
    public class PointsCounter : MonoBehaviour
    {
        public Text pointsText;

        public int CurrentDisplayValue
        {
            get => _currentDisplayValue;
            set
            {
                _currentDisplayValue = value;
                pointsText.text = _currentDisplayValue.ToString();
                if (CgsNetManager.Instance != null && CgsNetManager.Instance.isNetworkActive &&
                    CgsNetManager.Instance.LocalPlayer != null &&
                    CgsNetManager.Instance.LocalPlayer.CurrentScore != _currentDisplayValue)
                    CgsNetManager.Instance.LocalPlayer.RequestScoreUpdate(_currentDisplayValue);
            }
        }

        private int _currentDisplayValue;

        public void Decrement()
        {
            CurrentDisplayValue--;
        }

        public void Increment()
        {
            CurrentDisplayValue++;
        }
    }
}
