/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.ComponentModel;
using Newtonsoft.Json;

namespace CardGameDef
{
    [JsonObject(MemberSerialization.OptIn)]
    public class GameBoard
    {
        [JsonProperty]
        [Description("The id of the board")]
        public string Id { get; private set; }

        [JsonProperty]
        [Description("Indicates the position (in inches) of the bottom-left corner")]
        public Float2 OffsetMin { get; private set; }

        [JsonProperty]
        [Description("Indicates the board's width and height in inches")]
        public Float2 Size { get; private set; }

        [JsonConstructor]
        public GameBoard(string id, Float2 offsetMin, Float2 size)
        {
            Id = id ?? string.Empty;
            OffsetMin = offsetMin;
            Size = size;
        }
    }
}
