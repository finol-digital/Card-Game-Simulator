/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Newtonsoft.Json;
using UnityEngine;

namespace CardGameDef
{
    [JsonObject(MemberSerialization.OptIn)]
    public class GameBoard
    {
        [JsonProperty]
        public string Id { get; private set; }

        [JsonProperty]
        public Vector2 OffsetMin { get; private set; }

        [JsonProperty]
        public Vector2 Size { get; private set; }
    }
}
