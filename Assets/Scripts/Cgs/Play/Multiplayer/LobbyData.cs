/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

namespace Cgs.Play.Multiplayer
{
    public class LobbyData
    {
        public const string KeyRelayJoinCode = nameof(RelayJoinCode);

        public string Id { get; set; }
        public string Name { get; set; }
        public int PlayerCount { get; set; }
        public int MaxPlayers { get; set; }
        public string RelayJoinCode { get; set; }

        public override string ToString()
        {
            return $"{Name}\n{PlayerCount} / {MaxPlayers}";
        }
    }
}
