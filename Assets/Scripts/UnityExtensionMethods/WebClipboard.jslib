/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

mergeInto(LibraryManager.library, {
    CgsCopyToClipboard: function (textPointer, requestId, callback) {
        var text = UTF8ToString(textPointer);
        var finished = false;
        function complete(succeeded) {
            if (finished) return;
            finished = true;
            {{{ makeDynCall('vii', 'callback') }}}(requestId, succeeded);
        }

        // Unity's systemCopyBuffer can read back its cached value without an OS
        // clipboard write. Only the browser's fulfilled write confirms copying.
        if (!window.isSecureContext || !navigator.clipboard || !navigator.clipboard.writeText) {
            complete(0);
            return;
        }

        try {
            navigator.clipboard.writeText(text).then(function () {
                complete(1);
            }, function () {
                complete(0);
            });
        } catch (error) {
            complete(0);
        }
    }
});
