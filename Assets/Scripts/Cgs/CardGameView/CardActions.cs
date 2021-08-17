/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using Cgs.CardGameView.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

namespace Cgs.CardGameView
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
            // TODO
        }

        public static void Flip(CardModel cardModel)
        {
            if (cardModel.ParentCardZone == null || !cardModel.ParentCardZone.allowsFlip)
            {
                Debug.LogWarning("Ignoring flip request since the parent card zone does not support it.");
                return;
            }

            cardModel.IsFacedown = !cardModel.isFacedown;
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

            bool isVertical = cardModel.transform.rotation.Equals(Quaternion.identity);
            cardModel.transform.rotation = isVertical
                ? Quaternion.AngleAxis(CardGameManager.Current.GameCardRotationDegrees, Vector3.back)
                : Quaternion.identity;
            if (cardModel.IsOnline)
                cardModel.CmdUpdateRotation(cardModel.transform.rotation);
        }

        public List<Transform> buttonPanels;
        public List<Button> flipButtons;
        public List<Button> rotateButtons;
        public List<Button> tapButtons;

        public void Show()
        {
            foreach (Transform panel in buttonPanels)
                panel.gameObject.SetActive(true);
        }

        public void Update()
        {
            bool isCardSelected = CardViewer.Instance != null && CardViewer.Instance.IsVisible &&
                                  CardViewer.Instance.SelectedCardModel != null;

            foreach (Button flipButton in flipButtons)
                flipButton.interactable =
                    isCardSelected && CardViewer.Instance.SelectedCardModel.ParentCardZone != null &&
                    CardViewer.Instance.SelectedCardModel.ParentCardZone.allowsFlip;

            foreach (Button rotateButton in rotateButtons)
                rotateButton.interactable =
                    isCardSelected && CardViewer.Instance.SelectedCardModel.ParentCardZone != null &&
                    CardViewer.Instance.SelectedCardModel.ParentCardZone.allowsRotation;

            foreach (Button tapButton in tapButtons)
                tapButton.interactable =
                    isCardSelected && CardViewer.Instance.SelectedCardModel.ParentCardZone != null &&
                    CardViewer.Instance.SelectedCardModel.ParentCardZone.allowsRotation;

            if (Inputs.IsFilter && flipButtons[0].interactable)
                Flip(CardViewer.Instance.SelectedCardModel);
            else if (Inputs.IsNew) // TODO
                Move(CardViewer.Instance.SelectedCardModel);
            else if (Inputs.IsLoad && rotateButtons[0].interactable)
                Rotate(CardViewer.Instance.SelectedCardModel);
            else if (Inputs.IsSave && tapButtons[0].interactable)
                Tap(CardViewer.Instance.SelectedCardModel);
        }

        public void Flip()
        {
            Flip(CardViewer.Instance.SelectedCardModel);
        }

        public void Rotate()
        {
            Rotate(CardViewer.Instance.SelectedCardModel);
        }

        public void Tap()
        {
            Tap(CardViewer.Instance.SelectedCardModel);
        }
    }
}
