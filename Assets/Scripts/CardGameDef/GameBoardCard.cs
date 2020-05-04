/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

namespace CardGameDef
{
    [JsonObject(MemberSerialization.OptIn)]
    public class GameBoardCard
    {
        [JsonProperty]
        [Description(
            "When a deck is loaded in Play Mode, any card with *Card:Id* = *Card* will cause *Boards* to be put into the play area.")]
        public string Card { get; private set; }

        [JsonProperty] public List<GameBoard> Boards { get; private set; }

        [JsonConstructor]
        public GameBoardCard(string card, List<GameBoard> boards)
        {
            Card = card ?? string.Empty;
            Boards = boards ?? new List<GameBoard>();
        }
    }
}
