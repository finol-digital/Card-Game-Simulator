/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.Play.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

namespace Cgs.Play
{
    public class PointsCounter : MonoBehaviour
    {
        public Text pointsText;

        public int CurrentDisplayValue
        {
            get => int.Parse(pointsText.text);
            set => pointsText.text = value.ToString();
        }

        public void Decrement()
        {
            CurrentDisplayValue--;
            UpdateNetScore();
        }

        public void Increment()
        {
            CurrentDisplayValue++;
            UpdateNetScore();
        }

        private void UpdateNetScore()
        {
            if (CgsNetManager.Instance != null && CgsNetManager.Instance.isNetworkActive &&
                CgsNetManager.Instance.LocalPlayer != null &&
                CgsNetManager.Instance.LocalPlayer.CurrentScore != CurrentDisplayValue)
                CgsNetManager.Instance.LocalPlayer.RequestScoreUpdate(CurrentDisplayValue);
        }
    }
}
