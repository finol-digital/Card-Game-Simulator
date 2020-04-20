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

        protected int Count
        {
            get { return CgsNetManager.Instance.LocalPlayer.CurrentScore; }
            set { CgsNetManager.Instance.LocalPlayer.RequestScoreUpdate(value); }
        }

        public void Decrement()
        {
            Count--;
        }

        public void Increment()
        {
            Count++;
        }

        public void UpdateText()
        {
            pointsText.text = Count.ToString();
        }
    }
}
