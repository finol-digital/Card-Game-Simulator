/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.CardGameView.Multiplayer;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Cgs.CardGameView
{
    public interface IStackDropHandler
    {
        void OnDrop(CardStack cardStack);
    }

    public class StackDropArea : MonoBehaviour, IDropHandler
    {
        public IStackDropHandler DropHandler { get; set; }

        public void OnDrop(PointerEventData eventData)
        {
            var cardStack = CardStack.GetPointerDrag(eventData);
            if (cardStack == null)
                return;

            DropHandler.OnDrop(cardStack);
        }
    }
}
