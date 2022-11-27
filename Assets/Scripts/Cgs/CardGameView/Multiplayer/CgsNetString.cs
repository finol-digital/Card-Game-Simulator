/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using Unity.Collections;
using Unity.Netcode;

namespace Cgs.CardGameView.Multiplayer
{
    public struct CgsNetString : INetworkSerializeByMemcpy, IEquatable<CgsNetString>
    {
        private ForceNetworkSerializeByMemcpy<FixedString32Bytes> _info;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _info);
        }

        public bool Equals(CgsNetString other)
        {
            return ToString().Equals(other.ToString());
        }

        public override string ToString()
        {
            return _info.Value.ToString();
        }

        public static implicit operator string(CgsNetString s) => s.ToString();

        public static implicit operator CgsNetString(string s) =>
            new() {_info = new FixedString32Bytes(s)};
    }
}
