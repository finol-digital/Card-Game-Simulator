/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;

namespace CardGameDef
{
    public class CardSearchFilters
    {
        public string Id { get; set; } = "";

        public string Name { get; set; } = "";

        public string SetCode { get; set; } = "";

        public Dictionary<string, string> StringProperties { get; } = new Dictionary<string, string>();

        public Dictionary<string, int> IntMinProperties { get; } = new Dictionary<string, int>();

        public Dictionary<string, int> IntMaxProperties { get; } = new Dictionary<string, int>();

        public Dictionary<string, bool> BoolProperties { get; } = new Dictionary<string, bool>();

        public Dictionary<string, int> EnumProperties { get; } = new Dictionary<string, int>();

        public override string ToString()
        {
            return ToString(CardGame.Invalid);
        }

        public string ToString(CardGame forGame)
        {
            string filters = string.Empty;
            if (!string.IsNullOrEmpty(Name))
                filters += "name:\"" + Name + "\"; ";
            if (!string.IsNullOrEmpty(Id))
                filters += "id:" + Id + "; ";
            if (!string.IsNullOrEmpty(SetCode))
                filters += "set:" + SetCode + "; ";
            foreach (PropertyDef property in forGame.CardProperties)
            {
                switch (property.Type)
                {
                    case PropertyType.ObjectEnum:
                    case PropertyType.ObjectEnumList:
                    case PropertyType.StringEnum:
                    case PropertyType.StringEnumList:
                        if (!EnumProperties.ContainsKey(property.Name))
                            break;
                        EnumDef enumDef = forGame.Enums.FirstOrDefault(def => def.Property.Equals(property.Name));
                        if (enumDef != null)
                        {
                            string filterValue = enumDef.GetStringFromLookupFlags(EnumProperties[property.Name]);
                            if (filterValue.Contains(' '))
                                filterValue = "\'" + filterValue + "\'";
                            filters += property.Name + ":" + filterValue + "; ";
                        }
                        break;
                    case PropertyType.Boolean:
                        if (BoolProperties.ContainsKey(property.Name))
                            filters += (BoolProperties[property.Name] ? "IS " : "NOT ") + property.Name + "; ";
                        break;
                    case PropertyType.Integer:
                        if (IntMinProperties.ContainsKey(property.Name))
                            filters += property.Name + ">=" + IntMinProperties[property.Name] + "; ";
                        if (IntMaxProperties.ContainsKey(property.Name))
                            filters += property.Name + "<=" + IntMaxProperties[property.Name] + "; ";
                        break;
                    case PropertyType.Object:
                    case PropertyType.ObjectList:
                    case PropertyType.StringList:
                    case PropertyType.EscapedString:
                    case PropertyType.String:
                    default:
                        if (StringProperties.ContainsKey(property.Name))
                            filters += property.Name + ":\"" + StringProperties[property.Name] + "\"; ";
                        break;
                }
            }
            return filters;
        }
    }
}
