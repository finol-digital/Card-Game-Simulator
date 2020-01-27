/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.ComponentModel;
using Newtonsoft.Json;

namespace CardGameDef
{
    [JsonObject(MemberSerialization.OptIn)]
    public class GameBoardUrl
    {
        [JsonProperty]
        [Description("The id of the board")]
        public string Id { get; private set; }

        [JsonProperty]
        [Description("The url from which to download the board image")]
        public Uri Url { get; private set; }

        [JsonConstructor]
        public GameBoardUrl(string id, Uri url)
        {
            Id = id ?? string.Empty;
            Url = url;
        }
    }
}
