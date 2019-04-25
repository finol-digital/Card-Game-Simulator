/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;

namespace CardGameDef
{
    public class CardSearchFilters
    {
        public void Parse(string input)
        {
            // TODO: SET FILTERS BASED OFF INPUT
        }

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
            foreach (var filter in StringProperties)
                filters += $"{filter.Key}:\"{filter.Value}\"; ";
            foreach (var filter in IntMinProperties)
                filters += $"{filter.Key}>={filter.Value}; ";
            foreach (var filter in IntMaxProperties)
                filters += $"{filter.Key}<={filter.Value}; ";
            foreach (var filter in BoolProperties)
            {
                if (filter.Value)
                    filters += $"is:{filter.Key}; ";
                else
                    filters += $"not:{filter.Key}; ";
            }
            foreach (var filter in EnumProperties)
            {
                string filterValue = filter.Value.ToString();
                EnumDef enumDef = forGame.Enums.FirstOrDefault(def => def.Property.Equals(filter.Key));
                if (enumDef != null)
                {
                    filterValue = enumDef.GetStringFromLookupFlags(EnumProperties[filter.Key]);
                    if (filterValue.Contains(' '))
                        filterValue = "\'" + filterValue + "\'";
                }
                filters += $"{filter.Key}:{filterValue}; ";
            }
            return filters;
        }
    }
}
