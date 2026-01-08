/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.CardGameView.Multiplayer;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Cgs.CardGameView
{
    public interface ICounterDropHandler
    {
        void OnDrop(Counter counter);
    }

    public class CounterDropArea : MonoBehaviour, IDropHandler
    {
        public ICounterDropHandler DropHandler { get; set; }

        public void OnDrop(PointerEventData eventData)
        {
            var counter = Counter.GetPointerDrag(eventData);
            if (counter == null)
                return;

            DropHandler?.OnDrop(counter);
        }
    }
}
