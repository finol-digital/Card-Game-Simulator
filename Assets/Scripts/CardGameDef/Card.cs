/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace CardGameDef
{
    public class Card : IComparable<Card>, IEquatable<Card>
    {
        public static readonly Card Blank = new Card(CardGame.Invalid,
            string.Empty, string.Empty, string.Empty, new Dictionary<string, PropertyDefValuePair>(), false);

        public string Id { get; private set; }
        public string Name { get; private set; }
        public string SetCode { get; private set; }
        protected Dictionary<string, PropertyDefValuePair> Properties { get; private set; }
        public bool IsReprint { get; private set; }
        public bool IsLoadingImage { get; private set; }

        public string ImageFileName => UnityExtensionMethods.GetSafeFileName(Id + "." + SourceGame.CardImageFileType);
        public string ImageFilePath => UnityExtensionMethods.GetSafeFilePath(SourceGame.GameFolderPath + "/sets/" + SetCode + "/") + ImageFileName;
        public string ImageWebUrl
        {
            get
            {
                string url = GetPropertyValueString(SourceGame.CardImageProperty);
                if (!string.IsNullOrEmpty(url) && !url.Equals(SourceGame.CardImageUrl))
                    return url;

                url = SourceGame.CardImageUrl;
                url = url.Replace("{cardId}", Id);
                url = url.Replace("{cardName}", Name);
                url = url.Replace("{cardSet}", SetCode);
                Regex rgx = new Regex("{card\\.(?<property>\\w+)}");
                foreach (Match match in rgx.Matches(url))
                    url = url.Replace(match.Value, GetPropertyValueString(match.Groups["property"].Value));
                url = url.Replace("{cardImageFileType}", SourceGame.CardImageFileType);
                return url;
            }
        }

        public UnityEngine.Sprite ImageSprite
        {
            get { return _imageSprite; }
            set
            {
                if (_imageSprite != null)
                {
                    UnityEngine.Object.Destroy(_imageSprite.texture);
                    UnityEngine.Object.Destroy(_imageSprite);
                }
                _imageSprite = value;
                foreach (ICardDisplay cardDisplay in DisplaysUsingImage)
                    cardDisplay.SetImageSprite(_imageSprite);
            }
        }
        private UnityEngine.Sprite _imageSprite;

        protected HashSet<ICardDisplay> DisplaysUsingImage { get; private set; }

        protected CardGame SourceGame { get; private set; }

        public Card(CardGame sourceGame, string id, string name, string setCode, Dictionary<string, PropertyDefValuePair> properties, bool isReprint)
        {
            SourceGame = sourceGame ?? CardGame.Invalid;
            Id = id.Clone() as string;
            Name = !string.IsNullOrEmpty(name) ? name.Clone() as string : string.Empty;
            SetCode = !string.IsNullOrEmpty(setCode) ? setCode.Clone() as string : Set.DefaultCode;
            Properties = properties ?? new Dictionary<string, PropertyDefValuePair>();
            Properties = CloneProperties();
            IsReprint = isReprint;
            IsLoadingImage = false;
            DisplaysUsingImage = new HashSet<ICardDisplay>();
        }

        public Dictionary<string, PropertyDefValuePair> CloneProperties()
        {
            return Properties.ToDictionary(property => (string)property.Key.Clone(), property => property.Value.Clone() as PropertyDefValuePair);
        }

        public string GetPropertyValueString(string propertyName)
        {
            PropertyDefValuePair property;
            if (string.IsNullOrEmpty(propertyName) || !Properties.TryGetValue(propertyName, out property))
                return string.Empty;

            EnumDef enumDef = SourceGame.Enums.FirstOrDefault(def => def.Property.Equals(propertyName));
            if (enumDef == null || string.IsNullOrEmpty(property.Value))
                return !string.IsNullOrEmpty(property.Value) ? property.Value : property.Def.Empty ?? string.Empty;
            return enumDef.GetStringFromPropertyValue(property.Value);
        }

        public int GetPropertyValueInt(string propertyName)
        {
            PropertyDefValuePair property;
            if (string.IsNullOrEmpty(propertyName) || !Properties.TryGetValue(propertyName, out property))
                return 0;

            int intValue;
            if (!EnumDef.TryParseInt(property.Value, out intValue))
                return 0;
            return intValue;
        }

        public int GetPropertyValueEnum(string propertyName)
        {
            PropertyDefValuePair property;
            if (string.IsNullOrEmpty(propertyName) || !Properties.TryGetValue(propertyName, out property))
                return 0;

            EnumDef enumDef = SourceGame.Enums.FirstOrDefault(def => def.Property.Equals(propertyName));
            if (enumDef == null)
                return 0;
            return enumDef.GetEnumFromPropertyValue(property.Value);
        }

        public void RegisterDisplay(ICardDisplay cardDisplay)
        {
            DisplaysUsingImage.Add(cardDisplay);
            if (ImageSprite != null)
                cardDisplay.SetImageSprite(ImageSprite);
            else if (!IsLoadingImage)
            {
                if (SourceGame.CoroutineRunner != null)
                    SourceGame.CoroutineRunner.StartCoroutine(GetAndSetImageSprite());
                else
                    UnityEngine.Debug.LogWarning("RegisterDisplay::NoImageOrImageLoader");
            }
        }

        public IEnumerator GetAndSetImageSprite()
        {
            if (IsLoadingImage)
                yield break;

            IsLoadingImage = true;
            UnityEngine.Sprite newSprite = null;
            yield return UnityExtensionMethods.RunOutputCoroutine<UnityEngine.Sprite>(
                UnityExtensionMethods.CreateAndOutputSpriteFromImageFile(ImageFilePath, ImageWebUrl)
                , output => newSprite = output);
            if (newSprite != null)
                ImageSprite = newSprite;
            IsLoadingImage = false;
        }

        public void UnregisterDisplay(ICardDisplay cardDisplay)
        {
            cardDisplay.SetImageSprite(null);
            DisplaysUsingImage.Remove(cardDisplay);
            if (DisplaysUsingImage.Count < 1)
                ImageSprite = null;
        }

        public int CompareTo(Card other)
        {
            if (other == null)
                return -1;

            // TODO: FIXME: THIS IS NONDETERMINISTIC
            foreach (PropertyDefValuePair property in Properties.Values)
            {
                switch (property.Def.Type)
                {
                    case PropertyType.ObjectEnum:
                    case PropertyType.ObjectEnumList:
                    case PropertyType.StringEnum:
                    case PropertyType.StringEnumList:
                    case PropertyType.Integer:
                        int thisValue = GetPropertyValueInt(property.Def.Name);
                        int otherValue = other.GetPropertyValueInt(property.Def.Name);
                        return thisValue.CompareTo(otherValue);
                    case PropertyType.Object:
                    case PropertyType.ObjectList:
                    case PropertyType.Number:
                    case PropertyType.Boolean:
                    case PropertyType.StringList:
                    case PropertyType.EscapedString:
                    case PropertyType.String:
                    default:
                        return string.Compare(property.Value, other.Properties[property.Def.Name].Value, StringComparison.Ordinal);
                }
            }
            return 0;
        }

        public bool Equals(Card other)
        {
            return other != null && Id.Equals(other.Id);
        }
    }
}
