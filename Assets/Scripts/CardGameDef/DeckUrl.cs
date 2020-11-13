/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.ComponentModel;
using Newtonsoft.Json;

namespace CardGameDef
{
    [JsonObject(MemberSerialization.OptIn)]
    public class DeckUrl
    {
        [JsonProperty]
        [Description("The name of the deck")]
        public string Name { get; private set; }

        [JsonProperty]
        [Description(
            "An optional path that can be used to override the url. See @CardGameDef.json#/properties/allDecksUrlTxtRoot")]
        public string Txt { get; private set; }

        [JsonProperty]
        [Description("The url from which to download the deck")]
        public Uri Url { get; private set; }

        [JsonConstructor]
        public DeckUrl(string name, string txt, Uri url)
        {
            Name = name ?? string.Empty;
            Txt = txt ?? string.Empty;
            Url = url;
        }
    }
}
