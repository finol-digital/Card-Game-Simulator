/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CardGameDef
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CardAction
    {
        [EnumMember(Value = "move")] Move,
        [EnumMember(Value = "flip")] Flip,
        [EnumMember(Value = "rotate")] Rotate,
        [EnumMember(Value = "tap")] Tap,
        [EnumMember(Value = "zoom")] Zoom
    }
}
