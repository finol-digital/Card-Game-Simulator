/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using JetBrains.Annotations;

namespace CardGameDef
{
    [PublicAPI]
    public class Set : IEquatable<Set>
    {
        public const string DefaultCode = "_CGSDEFAULT_";
        public const string DefaultName = "_CGSDEFAULT_";

        public string Code { get; }
        public string Name { get; set; }
        public string CardsUrl { get; set; }

        public Set(string code, string name, string cardsUrl = null)
        {
            Code = !string.IsNullOrEmpty(code) ? code.Clone() as string : DefaultCode;
            Name = !string.IsNullOrEmpty(name) ? name.Clone() as string : DefaultName;
            CardsUrl = !string.IsNullOrEmpty(cardsUrl) ? cardsUrl.Clone() as string : string.Empty;
        }

        public virtual bool Equals(Set other)
        {
            return other != null && Code.Equals(other.Code);
        }

        public override string ToString()
        {
            return Code.Equals(Name) ? Code : string.Format("{1} ({0})", Code, Name);
        }
    }
}
