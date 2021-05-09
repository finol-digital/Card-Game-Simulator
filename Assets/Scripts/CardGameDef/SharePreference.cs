/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CardGameDef
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SharePreference
    {
        [EnumMember(Value = "ask")] Ask,
        [EnumMember(Value = "share")] Share,
        [EnumMember(Value = "individual")] Individual
    }
}
