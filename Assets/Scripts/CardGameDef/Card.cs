/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace CardGameDef
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Card : IComparable<Card>, IEquatable<Card>
    {
        [JsonProperty] public string Id { get; private set; }
        [JsonProperty] public string Name { get; private set; }
        [JsonProperty] public string SetCode { get; private set; }
        protected Dictionary<string, PropertyDefValuePair> Properties { get; private set; }
        public bool IsReprint { get; private set; }

        public string ImageWebUrl
        {
            get
            {
                var url = _imageWebUrl;
                var cardImageUrl = SourceGame.CardImageUrl;
                if (!string.IsNullOrEmpty(url) && !url.Equals(cardImageUrl))
                    return url;
                // NOTE: cardImageUrl uses this custom implementation of uri-template to allow for more versatility
                url = cardImageUrl;
                url = url.Replace("{cardId}", Id);
                url = url.Replace("{cardName}", Name);
                url = url.Replace("{cardSet}", SetCode);
                url = url.Replace("{cardImageFileType}", SourceGame.CardImageFileType);
                var propertyRegex = new Regex(@"\{(?<property>[\w\.]+)\}");
                foreach (Match match in propertyRegex.Matches(url))
                    url = url.Replace(match.Value, GetPropertyValueString(match.Groups["property"].Value));
                var listPropertyRegex = new Regex(@"\{(?<property>[\w\.]+)\[(?<index>\d+)\](?<child>[\w\.]*)\}");
                foreach (Match match in listPropertyRegex.Matches(url))
                {
                    var stringList =
                        GetPropertyValueString(match.Groups["property"].Value + match.Groups["child"].Value);
                    var splitStringList = stringList.Split(new[] {EnumDef.Delimiter}, StringSplitOptions.None);
                    if (int.TryParse(match.Groups["index"].Value, out var index) && index >= 0 &&
                        index < splitStringList.Length)
                        url = url.Replace(match.Value, splitStringList[index]);
                }

                return url;
            }
            set => _imageWebUrl = value;
        }

        private string _imageWebUrl;

        protected CardGame SourceGame { get; set; }

        public Card(CardGame sourceGame, string id, string name, string setCode,
            Dictionary<string, PropertyDefValuePair> properties, bool isReprint)
        {
            SourceGame = sourceGame ?? CardGame.Invalid;
            Id = id.Clone() as string;
            Name = !string.IsNullOrEmpty(name) ? name.Clone() as string : string.Empty;
            SetCode = !string.IsNullOrEmpty(setCode) ? setCode.Clone() as string : SourceGame.SetCodeDefault;
            Properties = properties ?? new Dictionary<string, PropertyDefValuePair>();
            Properties = CloneProperties();
            IsReprint = isReprint;
        }

        public Dictionary<string, PropertyDefValuePair> CloneProperties()
        {
            return Properties.ToDictionary(property => (string) property.Key.Clone(),
                property => property.Value.Clone() as PropertyDefValuePair);
        }

        public string GetPropertyValueString(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) ||
                !Properties.TryGetValue(propertyName, out var property))
                return string.Empty;

            var enumDef = SourceGame.Enums.FirstOrDefault(def => def.Property.Equals(propertyName));
            if (enumDef == null || string.IsNullOrEmpty(property.Value))
                return !string.IsNullOrEmpty(property.Value)
                    ? property.Value
                    : property.Def.DisplayEmpty ?? string.Empty;
            return enumDef.GetStringFromPropertyValue(property.Value);
        }

        public int GetPropertyValueInt(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) ||
                !Properties.TryGetValue(propertyName, out var property))
                return 0;

            return EnumDef.TryParseInt(property.Value, out var intValue) ? intValue : 0;
        }

        public bool GetPropertyValueBool(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) ||
                !Properties.TryGetValue(propertyName, out var property))
                return false;

            return "true".Equals(property.Value, StringComparison.OrdinalIgnoreCase)
                   || "yes".Equals(property.Value, StringComparison.OrdinalIgnoreCase)
                   || "y".Equals(property.Value, StringComparison.OrdinalIgnoreCase)
                   || "1".Equals(property.Value, StringComparison.OrdinalIgnoreCase);
        }

        public int GetPropertyValueEnum(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) ||
                !Properties.TryGetValue(propertyName, out var property))
                return 0;

            var enumDef = SourceGame.Enums.FirstOrDefault(def => def.Property.Equals(propertyName));
            return enumDef?.GetEnumFromPropertyValue(property.Value) ?? 0;
        }

        public int CompareTo(Card other)
        {
            if (other == null)
                return -1;

            var propertyToCompare = Properties.FirstOrDefault().Value;
            if (propertyToCompare == null)
                return 0;

            switch (propertyToCompare.Def.Type)
            {
                case PropertyType.ObjectEnum:
                case PropertyType.ObjectEnumList:
                case PropertyType.StringEnum:
                case PropertyType.StringEnumList:
                case PropertyType.Boolean:
                    var thisBool = GetPropertyValueBool(propertyToCompare.Def.Name);
                    var otherBool = other.GetPropertyValueBool(propertyToCompare.Def.Name);
                    return otherBool.CompareTo(thisBool);
                case PropertyType.Integer:
                    var thisInt = GetPropertyValueInt(propertyToCompare.Def.Name);
                    var otherInt = other.GetPropertyValueInt(propertyToCompare.Def.Name);
                    return thisInt.CompareTo(otherInt);
                case PropertyType.Object:
                case PropertyType.ObjectList:
                case PropertyType.StringList:
                case PropertyType.EscapedString:
                case PropertyType.String:
                default:
                    return string.Compare(propertyToCompare.Value, other.Properties[propertyToCompare.Def.Name].Value,
                        StringComparison.Ordinal);
            }
        }

        public virtual bool Equals(Card other)
        {
            return other != null && Id.Equals(other.Id);
        }
    }
}
