/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Reflection;
using Cgs.CardGameView.Multiplayer;
using Cgs.Menu;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;

namespace Tests.PlayMode
{
    public class LifecycleDispatchTests
    {
        private const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _testObject;

        [TearDown]
        public void TearDown()
        {
            if (_testObject != null)
                Object.DestroyImmediate(_testObject);
        }

        [Test]
        public void PlayableUpdateResetsHoldTimeWithoutPointers()
        {
            _testObject = new GameObject("Playable", typeof(NetworkObject));
            var playable = _testObject.AddComponent<CgsNetPlayable>();
            var holdTime = typeof(CgsNetPlayable).GetProperty("HoldTime", InstanceMembers);
            holdTime.SetValue(playable, 5f);

            typeof(CgsNetPlayable).GetMethod("Update", InstanceMembers).Invoke(playable, null);

            Assert.AreEqual(0f, holdTime.GetValue(playable));
        }

        [Test]
        public void BaseUpdateDispatchesToDiceZoneWithoutPlayableDragTracking()
        {
            _testObject = new GameObject("DiceZone", typeof(NetworkObject), typeof(BoxCollider2D));
            var zone = _testObject.AddComponent<DiceZone>();
            var holdTime = typeof(CgsNetPlayable).GetProperty("HoldTime", InstanceMembers);
            holdTime.SetValue(zone, 5f);
            ((List<Die>)zone.DiceInZone).Add(null);

            // Invoke through the base contract to detect accidental method hiding.
            typeof(CgsNetPlayable).GetMethod("Update", InstanceMembers).Invoke(zone, null);

            Assert.IsEmpty(zone.DiceInZone, "The dice-zone update should refresh its overlap results.");
            Assert.AreEqual(5f, holdTime.GetValue(zone), "The playable drag loop must not run for dice zones.");
        }

        [Test]
        public void BaseLateUpdateDispatchesToDialogWithoutModalFocusBookkeeping()
        {
            _testObject = new GameObject("Dialog");
            var dialog = _testObject.AddComponent<Dialog>();
            var newMessage = typeof(Dialog).GetField("_isNewMessage", InstanceMembers);
            newMessage.SetValue(dialog, true);
            typeof(Modal).GetProperty(nameof(Modal.WasFocused)).SetValue(dialog, true);

            typeof(Modal).GetMethod("LateUpdate", InstanceMembers).Invoke(dialog, null);

            Assert.IsFalse((bool)newMessage.GetValue(dialog), "The dialog should consume its new-message frame.");
            Assert.IsTrue(dialog.WasFocused, "The replacement callback should not run modal focus bookkeeping.");
        }
    }
}
