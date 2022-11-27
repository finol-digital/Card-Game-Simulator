/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using Unity.Netcode;

namespace Cgs.CardGameView.Multiplayer
{
    public struct CgsNetStringList : INetworkSerializeByMemcpy, IEquatable<CgsNetStringList>
    {
        public bool Equals(CgsNetStringList other)
        {
            // TODO:
            throw new NotImplementedException();
        }

        public static CgsNetStringList Of(CgsNetString[] cardIds)
        {
            throw new NotImplementedException();
        }

        public static CgsNetStringList Of(string[] strings)
        {
            // TODO:
            throw new NotImplementedException();
        }

        public List<string> ToListString()
        {
            // TODO:
            return null;
        }
    }
}
