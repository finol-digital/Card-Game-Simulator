/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.Play.Multiplayer;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;

namespace Tests.PlayMode
{
    public class BrowserLanTests
    {
        [Test]
        public void SelectingAnUnspawnedDeckDoesNotSendAnInvalidNetworkReference()
        {
            var playerObject = new GameObject("Browser LAN player test", typeof(NetworkObject));
            var deckObject = new GameObject("Unspawned deck test");
            try
            {
                var player = playerObject.AddComponent<CgsNetPlayer>();
                var deck = deckObject.AddComponent<NetworkObject>();
                Assert.DoesNotThrow(() => player.RequestSetCurrentDeck(deck));
                Assert.DoesNotThrow(() => player.RequestSetCurrentDeck(null));
            }
            finally
            {
                Object.DestroyImmediate(deckObject);
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void BrowserRoomIdCanBeDisplayedWithoutAnIpTransportOrDns()
        {
            var previous = NetworkManager.Singleton;
            var gameObject = new GameObject("Browser LAN test");
            try
            {
                var manager = gameObject.AddComponent<CgsNetManager>();
                manager.enabled = false;
                var transport = manager.BrowserTransport;
                transport.RoomId = "a123456789abcdef0123456789abcdef";
                manager.NetworkConfig = new NetworkConfig { NetworkTransport = transport };

                Assert.That(manager.RoomIdIp, Is.EqualTo(transport.RoomId));
                Assert.That(manager.BrowserTransport, Is.SameAs(transport));
                Assert.That(gameObject.GetComponents<BrowserLanTransport>(), Has.Length.EqualTo(1));
                manager.StopLanDiscovery();
                Assert.That(gameObject.GetComponent<CgsNetDiscovery>(), Is.Null,
                    "Browser discovery cleanup must not instantiate UDP discovery.");
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
                if (previous != null)
                    previous.SetSingleton();
            }
        }
    }
}
