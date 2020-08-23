/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;
using UnityEngine.EventSystems;

namespace CardGameView
{
    public interface ICardDropHandler
    {
        void OnDrop(CardModel cardModel);
    }

    public class CardDropArea : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IDropHandler
    {
        public ICardDropHandler DropHandler { get; set; }

        public bool isBlocker;

        public void OnPointerEnter(PointerEventData eventData)
        {
            CardModel cardModel = CardModel.GetPointerDrag(eventData);
            if (cardModel == null)
                return;

            cardModel.DropTarget = this;
            cardModel.IsHighlighted = false;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            CardModel cardModel = CardModel.GetPointerDrag(eventData);
            if (cardModel != null && cardModel.DropTarget == this)
                cardModel.DropTarget = null;
        }

        public void OnDrop(PointerEventData eventData)
        {
            Debug.Log("should drop");
            CardModel cardModel = CardModel.GetPointerDrag(eventData);
            if (cardModel == null
                || cardModel.ParentCardZone != null && cardModel.ParentCardZone.type != CardZoneType.Area
                || cardModel.PlaceHolderCardZone != null && cardModel.PlaceHolderCardZone.type != CardZoneType.Area)
                return;

            Debug.Log("do drop");
            DropHandler.OnDrop(cardModel);
            if (cardModel.DropTarget == this)
                cardModel.DropTarget = null;
        }
    }
}
