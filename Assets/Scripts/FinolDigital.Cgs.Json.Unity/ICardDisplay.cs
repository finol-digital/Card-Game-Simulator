/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;

namespace FinolDigital.Cgs.Json.Unity
{
    public interface ICardDisplay
    {
        // Displays that size themselves to the game's card size should use the oriented image,
        // while displays that fit the image itself, like the card viewer, should not
        bool UsesOrientedImage { get; }

        void SetImageSprite(Sprite imageSprite);
    }
}
