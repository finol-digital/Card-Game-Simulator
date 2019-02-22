/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
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
        public const string ObjectDelimiter = ".";
        public const string EscapeCharacter = "\\";

        [JsonProperty]
        public string Name { get; set; }

        [JsonProperty]
        public PropertyType Type { get; set; }

        [JsonProperty]
        public string Display { get; set; }

        [JsonProperty]
        public string DisplayEmpty { get; set; }

        [JsonProperty]
        public bool DisplayEmptyFirst { get; set; }

        [JsonProperty]
        public List<PropertyDef> Properties { get; set; }

        [JsonConstructor]
        public PropertyDef(string name, PropertyType type, string display = "", string displayEmpty = "", bool displayEmptyFirst = false, List<PropertyDef> properties = null)
        {
            Name = name ?? string.Empty;
            int objectDelimiterIdx = Name.IndexOf(ObjectDelimiter);
            if (objectDelimiterIdx != -1)
                Name = Name.Substring(0, objectDelimiterIdx);
            Type = objectDelimiterIdx != -1 ? PropertyType.Object : type;
            Display = display ?? string.Empty;
            DisplayEmpty = displayEmpty ?? string.Empty;
            DisplayEmptyFirst = displayEmptyFirst;
            Properties = properties != null ? new List<PropertyDef>(properties) : new List<PropertyDef>();
            if (objectDelimiterIdx != -1)
                Properties.Add(new PropertyDef(name.Substring(objectDelimiterIdx + 1), type, display, displayEmpty, displayEmptyFirst, properties));
        }

        public object Clone()
        {
            PropertyDef ret = new PropertyDef(Name, Type, Display, DisplayEmpty, DisplayEmptyFirst, Properties);
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
