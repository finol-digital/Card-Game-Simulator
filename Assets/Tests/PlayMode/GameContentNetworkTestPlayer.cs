/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.Play.Multiplayer;

namespace Tests.PlayMode
{
    // Exercise real network variables and stack creation without lobby/menu startup.
    public class GameContentNetworkTestPlayer : CgsNetPlayer
    {
        public override void OnNetworkSpawn() { }
        public override void OnNetworkDespawn() { }
    }
}
