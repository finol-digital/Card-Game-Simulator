/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using CardGameDef;
using Cgs.Menu;
using Cgs.Play.Multiplayer;
using JetBrains.Annotations;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Cgs.Play.Drawer
{
    public class HandDealer : Modal
    {
        private static string DealDraw
        {
            get
            {
                string result;
                switch (CardGameManager.Current.DeckSharePreference)
                {
                    case SharePreference.Ask:
                        var localPlayer = CgsNetManager.Instance.LocalPlayer;
                        result = localPlayer != null && localPlayer.IsDeckShared ? "Deal" : "Draw";
                        break;
                    case SharePreference.Share:
                        result = "Deal";
                        break;
                    case SharePreference.Individual:
                    default:
                        result = "Draw";
                        break;
                }

                return result;
            }
        }

        private string PromptMessage => $"{DealDraw} hand of {Count} cards?";

        public Text promptText;
        public Text countText;

        public int Count
        {
            get => _count;
            private set
            {
                _count = value;
                RefreshText();
            }
        }

        private int _count;

        private UnityAction _callback;

        protected override void Start()
        {
            base.Start();
            Count = CardGameManager.Current.GameStartHandCount;
        }

        private void Update()
        {
            if (!IsFocused)
                return;

            if (Inputs.IsSubmit)
                Confirm();
            else if (Inputs.IsHorizontal || Inputs.IsVertical)
            {
                if (Inputs.IsLeft && !Inputs.WasLeft || Inputs.IsDown && !Inputs.WasDown)
                    Decrement();
                else if (Inputs.IsRight && !Inputs.WasRight || Inputs.IsUp && !Inputs.WasUp)
                    Increment();
            }
            else if (Inputs.IsCancel)
                Hide();
        }

        private void RefreshText()
        {
            promptText.text = PromptMessage;
            countText.text = Count.ToString();
        }

        public void Show(UnityAction callback)
        {
            Show();
            Count = CardGameManager.Current.GameStartHandCount;
            _callback = callback;
        }

        [UsedImplicitly]
        public void Decrement()
        {
            Count--;
        }

        [UsedImplicitly]
        public void Increment()
        {
            Count++;
        }

        [UsedImplicitly]
        public void Confirm()
        {
            _callback?.Invoke();
            Hide();
        }
    }
}
