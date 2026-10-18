/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

mergeInto(LibraryManager.library, {
    CgsIsMobileBrowser: function () {
        if (typeof navigator === 'undefined') return false;
        // iPadOS can identify itself as a Mac; touch hardware alone is insufficient.
        return /Android|iPhone|iPad|iPod/i.test(navigator.userAgent || '') ||
            (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);
    }
});
