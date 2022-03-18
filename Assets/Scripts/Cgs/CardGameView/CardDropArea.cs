/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.CardGameView.Multiplayer;
using Cgs.Play.Drawer;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Cgs.CardGameView
{
    public interface ICardDropHandler
    {
        void OnDrop(CardModel cardModel);
    }

    public class CardDropArea : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IDropHandler
    {
        public ICardDropHandler DropHandler { get; set; }

        public int? Index { get; set; }

        public bool isBlocker;

        private void OnTriggerEnter2D(Collider2D other)
        {
            var cardModel = other.GetComponent<CardModel>();
            if (cardModel == null || cardModel.ParentCardZone == null ||
                cardModel.ParentCardZone.type != CardZoneType.Area)
                return;

            cardModel.DropTarget = this;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            var cardModel = CardModel.GetPointerDrag(eventData);
            if (cardModel == null)
                return;

            cardModel.DropTarget = this;
            cardModel.HighlightMode = HighlightMode.Off;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            var cardModel = CardModel.GetPointerDrag(eventData);
            if (cardModel != null && cardModel.DropTarget == this)
                cardModel.DropTarget = null;
        }

        public void OnDrop(PointerEventData eventData)
        {
            var cardModel = CardModel.GetPointerDrag(eventData);
            if (cardModel == null
                || cardModel.ParentCardZone != null && cardModel.ParentCardZone.type != CardZoneType.Area
                || cardModel.PlaceHolderCardZone != null && cardModel.PlaceHolderCardZone.type != CardZoneType.Area)
                return;

            var drawerViewer = DropHandler as DrawerViewer;
            if (drawerViewer != null && Index != null)
                drawerViewer.AddCard(cardModel.Value, Index ?? 0);
            else
                DropHandler.OnDrop(cardModel);
        }
    }
}
