/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.GameContent;

namespace Cgs.Play
{
    // Local creation settings only; legacy RPC arguments and named-message fields stay unchanged.
    public sealed class CardStackCreationOptions
    {
        public bool IsFaceup { get; set; }
        public bool? AutoStackCards { get; set; }
        public ulong? OwnerClientId { get; set; }
        public GameContentLabel Label { get; set; }
    }
}
