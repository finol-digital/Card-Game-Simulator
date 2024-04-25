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
        public const string NoRulesErrorMessage = "Rules Url does not exist for this game!";

        public Toggle autoStackCardsToggle;
        public Toggle doubleClickToViewStacksToggle;
        public Dropdown stackViewerOverlapDropdown;
        public Toggle doubleClickToRollDiceToggle;
        public InputField dieFaceCountInputField;

        public override void Show()
        {
            base.Show();
            autoStackCardsToggle.isOn = PlaySettings.AutoStackCards;
            doubleClickToViewStacksToggle.isOn = PlaySettings.DoubleClickToViewStacks;
            stackViewerOverlapDropdown.value = PlaySettings.StackViewerOverlap;
            doubleClickToRollDiceToggle.isOn = PlaySettings.DoubleClickToRollDice;
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
                CardGameManager.Instance.Messenger.Show(NoRulesErrorMessage);
        }

        [UsedImplicitly]
        public void SetAutoStackCards(bool autoStackCards)
        {
            PlaySettings.AutoStackCards = autoStackCards;
        }

        [UsedImplicitly]
        public void SetDoubleClickToViewStacks(bool doubleClickToViewStacks)
        {
            PlaySettings.DoubleClickToViewStacks = doubleClickToViewStacks;
        }

        [UsedImplicitly]
        public void SetStackViewerOverlap(int stackViewerOverlap)
        {
            PlaySettings.StackViewerOverlap = stackViewerOverlap;
        }

        [UsedImplicitly]
        public void SetDoubleClickToRollDice(bool doubleClickToRollDice)
        {
            PlaySettings.DoubleClickToRollDice = doubleClickToRollDice;
        }

        [UsedImplicitly]
        public void SetDieFaceCount(string dieFaceCount)
        {
            if (int.TryParse(dieFaceCount, out var intValue))
                PlaySettings.DieFaceCount = intValue;
        }
    }
}
