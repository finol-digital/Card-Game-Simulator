/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;
using UnityEngine.EventSystems;
using CardGameView;

namespace CGS.Play.Zones
{
    public interface ICardDropHandler
    {
        void OnDrop(CardModel cardModel);
    }

    public class CardDropZone : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IDropHandler
    {
        // HACK: UNITY CAN'T PASS INTERFACES THROUGH UI, SO THIS WILL NEED TO BE SET THROUGH CODE
        public ICardDropHandler dropHandler;

        public void OnPointerEnter(PointerEventData eventData)
        {
            CardModel cardModel = CardModel.GetPointerDrag(eventData);
            if (cardModel != null)
                cardModel.DropTarget = this;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            CardModel cardModel = CardModel.GetPointerDrag(eventData);
            if (cardModel != null && cardModel.DropTarget == this)
                cardModel.DropTarget = null;
        }

        public void OnDrop(PointerEventData eventData)
        {
            CardModel cardModel = CardModel.GetPointerDrag(eventData);
            if (cardModel != null && cardModel.PlaceHolder == null && cardModel.ParentCardStack == null)
                dropHandler.OnDrop(cardModel);
        }
    }
}
