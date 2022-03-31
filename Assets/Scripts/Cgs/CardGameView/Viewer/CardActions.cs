/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using Cgs.CardGameView.Multiplayer;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace Cgs.CardGameView.Viewer
{
    public delegate void CardAction(CardModel cardModel);

    public class CardActions : MonoBehaviour
    {
        public static IReadOnlyDictionary<CardGameDef.CardAction, CardAction> ActionsDictionary =>
            _actionsDictionary ??= new Dictionary<CardGameDef.CardAction, CardAction>
            {
                [CardGameDef.CardAction.Move] = Move,
                [CardGameDef.CardAction.Flip] = Flip,
                [CardGameDef.CardAction.Rotate] = Rotate,
                [CardGameDef.CardAction.Tap] = Tap
            };

        private static Dictionary<CardGameDef.CardAction, CardAction> _actionsDictionary;

        public static void Move(CardModel cardModel)
        {
            CardGameManager.Instance.Messenger.Show("Move Feature is Coming Soon!");
        }

        public static void Flip(CardModel cardModel)
        {
            if (cardModel.ParentCardZone == null || !cardModel.ParentCardZone.allowsFlip)
            {
                Debug.LogWarning("Ignoring flip request since the parent card zone does not support it.");
                return;
            }

            cardModel.SetIsFacedown(!cardModel.isFacedown);
        }

        public static void Rotate(CardModel cardModel)
        {
            if (cardModel.ParentCardZone == null || !cardModel.ParentCardZone.allowsRotation)
            {
                Debug.LogWarning("Ignoring rotation request since the parent card zone does not support it.");
                return;
            }

            cardModel.transform.rotation *= Quaternion.Euler(0, 0, -CardGameManager.Current.GameCardRotationDegrees);
            if (cardModel.IsOnline)
                cardModel.CmdUpdateRotation(cardModel.transform.rotation);
        }

        public static void Tap(CardModel cardModel)
        {
            if (cardModel.ParentCardZone == null || !cardModel.ParentCardZone.allowsRotation)
            {
                Debug.LogWarning("Ignoring rotation request since the parent card zone does not support it.");
                return;
            }

            var isVertical = cardModel.transform.rotation.Equals(Quaternion.identity);
            cardModel.transform.rotation = isVertical
                ? Quaternion.AngleAxis(CardGameManager.Current.GameCardRotationDegrees, Vector3.back)
                : Quaternion.identity;
            if (cardModel.IsOnline)
                cardModel.CmdUpdateRotation(cardModel.transform.rotation);
        }

        public List<Transform> buttonPanels;
        public List<Button> flipButtons;
        public List<Button> moveButtons;
        public List<Button> rotateButtons;
        public List<Button> tapButtons;

        public void Show()
        {
            foreach (var panel in buttonPanels)
                panel.gameObject.SetActive(true);
        }

        public void Update()
        {
            var isCardSelected = CardViewer.Instance != null && CardViewer.Instance.IsVisible &&
                                 CardViewer.Instance.SelectedCardModel != null;

            foreach (var flipButton in flipButtons)
                flipButton.interactable =
                    isCardSelected && CardViewer.Instance.SelectedCardModel.ParentCardZone != null &&
                    CardViewer.Instance.SelectedCardModel.ParentCardZone.allowsFlip;

            foreach (var rotateButton in rotateButtons)
                rotateButton.interactable =
                    isCardSelected && CardViewer.Instance.SelectedCardModel.ParentCardZone != null &&
                    CardViewer.Instance.SelectedCardModel.ParentCardZone.allowsRotation;

            foreach (var tapButton in tapButtons)
                tapButton.interactable =
                    isCardSelected && CardViewer.Instance.SelectedCardModel.ParentCardZone != null &&
                    CardViewer.Instance.SelectedCardModel.ParentCardZone.allowsRotation;

            if (Inputs.IsFilter && flipButtons[0].interactable)
                Flip(CardViewer.Instance.SelectedCardModel);
            else if (Inputs.IsNew && moveButtons[0].interactable)
                Move(CardViewer.Instance.SelectedCardModel);
            else if (Inputs.IsLoad && rotateButtons[0].interactable)
                Rotate(CardViewer.Instance.SelectedCardModel);
            else if (Inputs.IsSave && tapButtons[0].interactable)
                Tap(CardViewer.Instance.SelectedCardModel);
        }

        [UsedImplicitly]
        public void Flip()
        {
            Flip(CardViewer.Instance.SelectedCardModel);
        }

        [UsedImplicitly]
        public void Move()
        {
            Move(CardViewer.Instance.SelectedCardModel);
        }

        [UsedImplicitly]
        public void Rotate()
        {
            Rotate(CardViewer.Instance.SelectedCardModel);
        }

        [UsedImplicitly]
        public void Tap()
        {
            Tap(CardViewer.Instance.SelectedCardModel);
        }
    }
}
