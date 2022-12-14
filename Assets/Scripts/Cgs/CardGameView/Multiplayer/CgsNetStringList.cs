/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Netcode;

namespace Cgs.CardGameView.Multiplayer
{
    public struct CgsNetStringList : INetworkSerializeByMemcpy, IEquatable<CgsNetStringList>
    {
        private const string Delimiter = "/";

        private ForceNetworkSerializeByMemcpy<FixedString4096Bytes> _info;

        [UsedImplicitly]
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _info);
        }

        public bool Equals(CgsNetStringList other)
        {
            return ToListString().Equals(other.ToListString());
        }

        public List<string> ToListString()
        {
            var toString = ToString();
            return new List<string>(toString.Split(Delimiter));
        }

        public override string ToString()
        {
            return _info.Value.ToString();
        }

        public static CgsNetStringList Of(IEnumerable<CgsNetString> cardIds)
        {
            var cardIdStrings = cardIds.Select(cgsNetString => (string) cgsNetString).ToList();
            return Of(cardIdStrings);
        }

        private static CgsNetStringList Of(IEnumerable<string> cardIds)
        {
            var s = cardIds.Aggregate(string.Empty, (current, next)
                => current + next.Replace(Delimiter, string.Empty) + Delimiter);
            CgsNetStringList result = new() {_info = new FixedString4096Bytes(s)};
            return result;
        }
    }
}
