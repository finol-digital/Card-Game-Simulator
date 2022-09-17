/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.Menu;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace Cgs.Play
{
    public class PlaySettingsMenu : Modal
    {
        public Toggle autoStackCardsToggle;
        public Dropdown stackViewerOverlapDropdown;
        public InputField dieFaceCountInputField;

        public override void Show()
        {
            base.Show();
            autoStackCardsToggle.isOn = PlaySettings.AutoStackCards;
            stackViewerOverlapDropdown.value = PlaySettings.StackViewerOverlap;
            dieFaceCountInputField.text = PlaySettings.DieFaceCount.ToString();
        }

        private void Update()
        {
            if (!IsFocused || dieFaceCountInputField.isFocused)
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

        [UsedImplicitly]
        public void SetAutoStackCards(bool autoStackCards)
        {
            PlaySettings.AutoStackCards = autoStackCards;
        }

        [UsedImplicitly]
        public void SetStackViewerOverlap(int stackViewerOverlap)
        {
            PlaySettings.StackViewerOverlap = stackViewerOverlap;
        }

        [UsedImplicitly]
        public void SetDieFaceCount(string dieFaceCount)
        {
            if (int.TryParse(dieFaceCount, out var intValue))
                PlaySettings.DieFaceCount = intValue;
        }
    }
}
