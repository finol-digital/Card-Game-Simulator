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
    },

    CgsReadClipboard: function (requestId, callback) {
        var finished = false;
        function complete(text) {
            if (finished) return;
            finished = true;
            var pointer = 0;
            try {
                if (typeof text === 'string') {
                    var size = lengthBytesUTF8(text) + 1;
                    pointer = _malloc(size);
                    stringToUTF8(text, pointer, size);
                }
                {{{ makeDynCall('vii', 'callback') }}}(requestId, pointer);
            } finally {
                if (pointer) _free(pointer);
            }
        }

        // Unity's systemCopyBuffer reads only text copied inside CGS, never the
        // browser clipboard. The browser may ask the player to allow reading it.
        if (!window.isSecureContext || !navigator.clipboard || !navigator.clipboard.readText) {
            complete(null);
            return;
        }

        try {
            navigator.clipboard.readText().then(function (text) {
                complete(text);
            }, function () {
                complete(null);
            });
        } catch (error) {
            complete(null);
        }
    }
});
