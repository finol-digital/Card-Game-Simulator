/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Newtonsoft.Json;
using System.Collections.Generic;

namespace CardGameDef
{
    [JsonObject(MemberSerialization.OptIn)]
    public class GameBoardCard
    {
        [JsonProperty]
        public string Card { get; private set; }

        [JsonProperty]
        public List<GameBoard> Boards { get; private set; }
    }
}
