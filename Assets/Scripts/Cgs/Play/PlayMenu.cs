/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.CardGameView.Viewer;
using JetBrains.Annotations;
using UnityEngine;

namespace Cgs.Play
{
    public class PlayMenu : MonoBehaviour
    {
        public const string NoRulesErrorMessage = "Rules Url does not exist for this game!";

        public GameObject panels;

        public PlayController controller;

        private void Update()
        {
            if (!panels.activeSelf || CardViewer.Instance.IsVisible || CardViewer.Instance.Zoom || !Input.anyKeyDown ||
                CardGameManager.Instance.ModalCanvas != null || controller.scoreboard.nameInputField.isFocused)
                return;

            if (Inputs.IsNew)
                ViewRules();
            else if (Inputs.IsLoad)
                ShowDeckMenu();
            else if (Inputs.IsFilter)
                ShowCardsMenu();
            else if (Inputs.IsSave)
                CreateDie();
        }

        public void Show()
        {
            panels.SetActive(true);
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
        public void ShowDeckMenu()
        {
            controller.ShowDeckMenu();
        }

        [UsedImplicitly]
        public void ShowCardsMenu()
        {
            controller.ShowCardsMenu();
        }

        [UsedImplicitly]
        public void CreateDie()
        {
            controller.CreateDefaultDie();
        }

        [UsedImplicitly]
        public void ToggleMenu()
        {
            if (panels.gameObject.activeSelf)
                Hide();
            else
                Show();
        }

        public void Hide()
        {
            panels.SetActive(false);
        }
    }
}
