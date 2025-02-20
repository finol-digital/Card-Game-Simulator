/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.Menu;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Cgs.Play
{
    public class PlaySettingsMenu : Modal
    {
        public const string NoAutoupdateErrorMessage = "This game has not configured a valid autoupdate url!";
        public const string NoRulesErrorMessage = "Rules Url does not exist for this game!";

        public Toggle autoStackCardsToggle;
        public Toggle doubleClickToViewStacksToggle;
        public Dropdown stackViewerOverlapDropdown;
        public Toggle doubleClickToRollDiceToggle;
        public InputField dieFaceCountInputField;
        public Toggle showActionsMenuToggle;
        public Transform launchNativeButton;
        public Transform viewRulesButton;

        public override void Show()
        {
            base.Show();
            autoStackCardsToggle.isOn = PlaySettings.AutoStackCards;
            doubleClickToViewStacksToggle.isOn = PlaySettings.DoubleClickToViewStacks;
            stackViewerOverlapDropdown.value = PlaySettings.StackViewerOverlap;
            doubleClickToRollDiceToggle.isOn = PlaySettings.DoubleClickToRollDice;
            dieFaceCountInputField.text = PlaySettings.DieFaceCount.ToString();
            showActionsMenuToggle.isOn = PlaySettings.ShowActionsMenu;
#if CGS_SINGLEGAME && CGS_SINGLEPLAYER
            launchNativeButton.gameObject.SetActive(true);
            viewRulesButton.gameObject.SetActive(false);
#else
            launchNativeButton.gameObject.SetActive(false);
            viewRulesButton.gameObject.SetActive(true);
#endif
        }

        private void Update()
        {
            if (!IsFocused || dieFaceCountInputField.isFocused)
                return;

            if (Inputs.IsOption)
#if CGS_SINGLEGAME && CGS_SINGLEPLAYER
                LaunchNative();
#else
                ViewRules();
#endif
            else if (Inputs.IsCancel)
                Hide();
        }

        [UsedImplicitly]
        public void LaunchNative()
        {
            if (CardGameManager.Current.AutoUpdateUrl != null &&
                CardGameManager.Current.AutoUpdateUrl.IsWellFormedOriginalString())
                Application.OpenURL("cardgamesim://?url=" +
                                    UnityWebRequest.EscapeURL(CardGameManager.Current.AutoUpdateUrl.OriginalString));
            else
                CardGameManager.Instance.Messenger.Show(NoAutoupdateErrorMessage);
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

        [UsedImplicitly]
        public void SetShowActionsMenu(bool showActionsMenu)
        {
            PlaySettings.ShowActionsMenu = showActionsMenu;
        }
    }
}
