/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using CardGameView;
using JetBrains.Annotations;
using UnityEngine;

namespace Cgs.Play
{
    public class PlayMenu : MonoBehaviour
    {
        public const string NoRulesErrorMessage = "Rules Url does not exist for this game!";

        public PlayMode controller;
        public GameObject hand;

        private void Update()
        {
            if (CardViewer.Instance.IsVisible || CardViewer.Instance.Zoom || !Input.anyKeyDown ||
                CardGameManager.Instance.ModalCanvas != null)
                return;

            if (Inputs.IsNew)
                ViewRules();
            else if (Inputs.IsLoad)
                ShowDeckMenu();
            else if (Inputs.IsFilter)
                ShowCardsMenu();
            else if (Inputs.IsSort)
                ShowDiceMenu();
            else if (Inputs.IsSave)
                ToggleHand();
        }

        public void Show()
        {
            gameObject.SetActive(transform);
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
        public void ShowDiceMenu()
        {
            controller.ShowDiceMenu();
        }

        [UsedImplicitly]
        public void ToggleHand()
        {
            hand.SetActive(!hand.activeSelf);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
