/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Newtonsoft.Json;

namespace CardGameDef
{
    [JsonObject(MemberSerialization.OptIn)]
    public class GameBoardUrl
    {
        [JsonProperty]
        public string Id { get; private set; }

        [JsonProperty]
        public string Url { get; private set; }

        [JsonConstructor]
        public GameBoardUrl(string id, string url)
        {
            Id = id ?? string.Empty;
            Url = url ?? string.Empty;
        }
    }
}
