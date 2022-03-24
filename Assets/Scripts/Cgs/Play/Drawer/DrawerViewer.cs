/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using CardGameDef.Unity;
using Cgs.CardGameView.Viewer;
using Cgs.Play.Multiplayer;
using UnityEngine;

namespace Cgs.Play.Drawer
{
    public class DrawerViewer : StackViewer
    {
        public void AddCard(UnityCard card, int handIndex)
        {
            if (handIndex == CgsNetManager.Instance.LocalPlayer.CurrentHand)
            {
                AddCard(card);
                return;
            }

            Debug.Log($"[DrawerViewer] Add {card.Id} to tab {handIndex}!");
            var cards = new List<string>(CgsNetManager.Instance.LocalPlayer.GetHandCards()[handIndex]
                .Select(unityCard => unityCard.Id).ToList()) {card.Id};
            CgsNetManager.Instance.LocalPlayer.RequestSyncHand(handIndex, cards.ToArray());
        }
    }
}
