/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Newtonsoft.Json;

namespace CardGameDef
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Float2
    {
        [JsonProperty] public float X { get; private set; }

        [JsonProperty] public float Y { get; private set; }

        [JsonConstructor]
        public Float2(float x, float y)
        {
            X = x;
            Y = y;
        }
    }
}
