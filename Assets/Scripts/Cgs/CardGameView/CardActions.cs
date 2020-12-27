/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using Cgs.CardGameView.Multiplayer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cgs.CardGameView
{
    public delegate void CardAction(CardModel cardModel);

    public class CardActions : MonoBehaviour
    {
        public static IReadOnlyDictionary<CardGameDef.CardAction, CardAction> ActionsDictionary =>
            _actionsDictionary ?? (_actionsDictionary = new Dictionary<CardGameDef.CardAction, CardAction>
            {
                [CardGameDef.CardAction.Move] = Move,
                [CardGameDef.CardAction.Flip] = Flip,
                [CardGameDef.CardAction.Rotate] = Rotate,
                [CardGameDef.CardAction.Tap] = Tap
            });

        private static Dictionary<CardGameDef.CardAction, CardAction> _actionsDictionary;

        public static void Move(CardModel cardModel)
        {
            // TODO
        }

        public static void Flip(CardModel cardModel)
        {
            if (cardModel.ParentCardZone == null || !cardModel.ParentCardZone.allowsFlip)
            {
                Debug.Log("Ignoring flip request since the parent card zone does not support it.");
                return;
            }

            cardModel.IsFacedown = !cardModel.isFacedown;
            EventSystem.current.SetSelectedGameObject(null, cardModel.CurrentPointerEventData);
        }

        public static void Rotate(CardModel cardModel)
        {
            if (cardModel.ParentCardZone == null || !cardModel.ParentCardZone.allowsRotation)
            {
                Debug.Log("Ignoring rotation request since the parent card zone does not support it.");
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
                Debug.Log("Ignoring rotation request since the parent card zone does not support it.");
                return;
            }

            bool isVertical = cardModel.transform.rotation.Equals(Quaternion.identity);
            cardModel.transform.rotation = isVertical
                ? Quaternion.AngleAxis(CardGameManager.Current.GameCardRotationDegrees, Vector3.back)
                : Quaternion.identity;
            if (cardModel.IsOnline)
                cardModel.CmdUpdateRotation(cardModel.transform.rotation);
        }

        public Button flipButton;
        public Button rotateButton;
        public Button tapButton;

        public void Update()
        {
            bool isCardSelected = CardViewer.Instance != null && CardViewer.Instance.IsVisible &&
                                  CardViewer.Instance.SelectedCardModel != null;

            flipButton.interactable = isCardSelected && CardViewer.Instance.SelectedCardModel.ParentCardZone != null &&
                                      CardViewer.Instance.SelectedCardModel.ParentCardZone.allowsFlip;
            rotateButton.interactable =
                isCardSelected && CardViewer.Instance.SelectedCardModel.ParentCardZone != null &&
                CardViewer.Instance.SelectedCardModel.ParentCardZone.allowsRotation;
            tapButton.interactable =
                isCardSelected && CardViewer.Instance.SelectedCardModel.ParentCardZone != null &&
                CardViewer.Instance.SelectedCardModel.ParentCardZone.allowsRotation;

            if (Inputs.IsFilter && flipButton.interactable)
                Flip(CardViewer.Instance.SelectedCardModel);
            else if (Inputs.IsNew) // TODO
                Move(CardViewer.Instance.SelectedCardModel);
            else if (Inputs.IsLoad && rotateButton.interactable)
                Rotate(CardViewer.Instance.SelectedCardModel);
            else if (Inputs.IsSave && tapButton.interactable)
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
