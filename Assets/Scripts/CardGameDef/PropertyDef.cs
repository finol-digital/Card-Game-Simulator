/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using Newtonsoft.Json;

namespace CardGameDef
{
    public enum PropertyType
    {
        String,
        EscapedString,
        Integer,
        Boolean,
        Object,
        StringEnum,
        StringList,
        StringEnumList,
        ObjectEnum,
        ObjectList,
        ObjectEnumList
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class PropertyDef : ICloneable
    {
        [JsonProperty]
        public string Name { get; set; }

        [JsonProperty]
        public string Display { get; set; }

        [JsonProperty]
        public PropertyType Type { get; set; }

        [JsonProperty]
        public string Empty { get; set; }

        public object Clone()
        {
            PropertyDef ret = new PropertyDef() { Name = Name.Clone() as string, Type = Type, Empty = Empty };
            return ret;
        }
    }

    public class PropertyDefValuePair : ICloneable
    {
        public PropertyDef Def { get; set; }

        public string Value { get; set; }

        public object Clone()
        {
            PropertyDefValuePair ret = new PropertyDefValuePair()
            {
                Def = Def.Clone() as PropertyDef,
                Value = Value.Clone() as string
            };
            return ret;
        }
    }
}
