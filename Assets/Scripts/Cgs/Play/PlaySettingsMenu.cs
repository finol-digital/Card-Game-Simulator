/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.Menu;
using JetBrains.Annotations;
using UnityEngine;

namespace Cgs.Play
{
    public class PlaySettingsMenu : Modal
    {
        private void Update()
        {
            if (!IsFocused)
                return;

            if (Inputs.IsOption)
                ViewRules();
            else if (Inputs.IsCancel)
                Hide();
        }

        [UsedImplicitly]
        public void ViewRules()
        {
            if (CardGameManager.Current.RulesUrl != null &&
                CardGameManager.Current.RulesUrl.IsWellFormedOriginalString())
                Application.OpenURL(CardGameManager.Current.RulesUrl.OriginalString);
            else
                CardGameManager.Instance.Messenger.Show("NoRulesErrorMessage");
        }
    }
}