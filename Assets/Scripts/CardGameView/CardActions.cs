/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;
using UnityEngine.EventSystems;

namespace CardGameView
{
    public delegate void CardAction(CardModel cardModel);

    public static class CardActions
    {
        public static void ResetRotation(CardModel cardModel)
        {
            if (cardModel == null || (cardModel.IsOnline && !cardModel.hasAuthority))
                return;

            cardModel.transform.rotation = Quaternion.identity;
            if (cardModel.IsOnline)
                cardModel.CmdUpdateRotation(cardModel.transform.rotation);
        }

        public static void Rotate90(CardModel cardModel)
        {
            if (cardModel == null || (cardModel.IsOnline && !cardModel.hasAuthority))
                return;

            cardModel.transform.rotation *= Quaternion.Euler(0, 0, -90);
            if (cardModel.IsOnline)
                cardModel.CmdUpdateRotation(cardModel.transform.rotation);
        }

        public static void ToggleRotation90(CardModel cardModel)
        {
            if (cardModel == null || (cardModel.IsOnline && !cardModel.hasAuthority))
                return;

            bool isVertical = cardModel.transform.rotation.Equals(Quaternion.identity);
            cardModel.transform.rotation = isVertical ? Quaternion.AngleAxis(90, Vector3.back) : Quaternion.identity;
            if (cardModel.IsOnline)
                cardModel.CmdUpdateRotation(cardModel.transform.rotation);
        }

        public static void FlipFace(CardModel cardModel)
        {
            if (cardModel == null || (cardModel.IsOnline && !cardModel.hasAuthority))
                return;

            cardModel.IsFacedown = !cardModel.IsFacedown;
            EventSystem.current.SetSelectedGameObject(null, cardModel.CurrentPointerEventData);
        }

        public static void ShowFacedown(CardModel cardModel)
        {
            if (cardModel == null || (cardModel.IsOnline && !cardModel.hasAuthority))
                return;

            cardModel.IsFacedown = true;
            EventSystem.current.SetSelectedGameObject(null, cardModel.CurrentPointerEventData);
        }

        public static void ShowFaceup(CardModel cardModel)
        {
            if (cardModel == null || (cardModel.IsOnline && !cardModel.hasAuthority))
                return;

            cardModel.IsFacedown = false;
        }
    }
}
