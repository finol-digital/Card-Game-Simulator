/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using Cgs.CardGameView.Multiplayer;
using Cgs.Menu;
using Cgs.Play.Multiplayer;
using JetBrains.Annotations;
using UnityEngine.UI;

namespace Cgs.Play
{
    public delegate Die CreateDieDelegate(int min, int max);

    public class DiceMenu : Modal
    {
        private const int DefaultMin = 1;
        private const int DefaultMax = 6;

        public Text maxText;

        private int Max
        {
            get => _max;
            set
            {
                _max = Math.Max(value, DefaultMin);
                maxText.text = _max.ToString();
            }
        }

        private int _max = DefaultMax;

        private CreateDieDelegate _createDieCallback;

        private void Update()
        {
            if (!IsFocused)
                return;

            if (Inputs.IsVertical)
            {
                if (Inputs.IsDown && !Inputs.WasDown)
                    DecrementMax();
                else if (Inputs.IsUp && !Inputs.WasUp)
                    IncrementMax();
            }

            if (Inputs.IsSubmit)
                CreateAndHide();
            else if (Inputs.IsCancel)
                Hide();
        }

        public void Show(CreateDieDelegate createDieCallback)
        {
            Show();
            _createDieCallback = createDieCallback;
        }

        [UsedImplicitly]
        public void DecrementMax()
        {
            Max--;
        }

        [UsedImplicitly]
        public void IncrementMax()
        {
            Max++;
        }

        [UsedImplicitly]
        public void CreateAndHide()
        {
            if (CgsNetManager.Instance.isNetworkActive && CgsNetManager.Instance.LocalPlayer != null)
                CgsNetManager.Instance.LocalPlayer.RequestNewDie(DefaultMin, Max);
            else
                _createDieCallback?.Invoke(DefaultMin, Max);

            Hide();
        }
    }
}
