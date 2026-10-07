/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.Localization;
using UnityExtensionMethods;

namespace Cgs.Menu
{
    public static class SharingMessages
    {
        // Adapt only the sharing service's documented result values. Arbitrary
        // dialog text and the content being shared never pass through this map.
        public static UiMessage FromFeedback(string feedback)
        {
            var key = feedback switch
            {
                TextSharing.CopiedMessage => "sharing.copied",
                TextSharing.CopyErrorMessage => "sharing.copy.failed",
                TextSharing.CopyUnconfirmedMessage => "sharing.copy.unconfirmed",
                TextSharing.BrowserCopyErrorMessage => "sharing.browser.failed",
                TextSharing.EmptyMessage => "sharing.empty",
                TextSharing.ShareErrorMessage => "sharing.failed",
                TextSharing.ShareUnavailableMessage => "sharing.unavailable",
                TextSharing.FileMissingMessage => "sharing.file.missing",
                TextSharing.FileReadErrorMessage => "sharing.file.failed",
                Cgs.Decks.DeckSaveMenu.DeckCopiedMessage => "sharing.deck.copied",
                Cgs.Play.Scoreboard.RoomIdIpCopiedMessage => "sharing.room.copied",
                _ => null
            };
            if (feedback == TextSharing.GetShareFeedback(NativeShare.ShareResult.Shared))
                key = "sharing.destination.selected";
            else if (feedback == TextSharing.GetShareFeedback(NativeShare.ShareResult.NotShared))
                key = "sharing.cancelled";
            else if (feedback == TextSharing.GetShareFeedback(NativeShare.ShareResult.Unknown))
                key = "sharing.unknown";
            return key == null ? null : new UiMessage(key, feedback);
        }
    }
}
