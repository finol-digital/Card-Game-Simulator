/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

mergeInto(LibraryManager.library, {
    CgsPickGameZip: function (directoryPointer, titlePointer, cancelPointer, callback) {
        var directory = UTF8ToString(directoryPointer);
        var title = UTF8ToString(titlePointer);
        var cancel = UTF8ToString(cancelPointer);
        var dialog;
        var reader;
        var finished = false;
        var previousFocus = document.activeElement;

        function complete(status, value) {
            if (finished) return;
            finished = true;
            if (reader && reader.readyState === 1) reader.abort();
            if (dialog) dialog.remove();
            if (previousFocus && previousFocus.isConnected) previousFocus.focus();
            var pointer = 0;
            try {
                if (value) {
                    var size = lengthBytesUTF8(value) + 1;
                    pointer = _malloc(size);
                    stringToUTF8(value, pointer, size);
                }
                {{{ makeDynCall('vii', 'callback') }}}(status, pointer);
            } finally {
                if (pointer) _free(pointer);
            }
        }

        try {
            // Unity button events can run after the browser's user gesture. A visible
            // native input ensures the picker always opens from a real click or tap.
            dialog = document.createElement('dialog');
            dialog.style.cssText = 'padding:24px;max-width:calc(100vw - 48px);color:#111;background:#fff;border-radius:8px;';
            dialog.setAttribute('aria-label', title);
            var label = document.createElement('label');
            label.textContent = title;
            var input = document.createElement('input');
            input.type = 'file';
            input.accept = '.cgs.zip,.zip,application/zip,application/x-zip-compressed';
            input.style.cssText = 'display:block;margin:16px 0;max-width:100%;';
            label.appendChild(input);
            dialog.appendChild(label);
            var cancelButton = document.createElement('button');
            cancelButton.type = 'button';
            cancelButton.textContent = cancel;
            cancelButton.onclick = function () { complete(0); };
            dialog.appendChild(cancelButton);
            dialog.addEventListener('cancel', function (event) {
                event.preventDefault();
                complete(0);
            });
            input.addEventListener('cancel', function () { complete(0); });
            input.addEventListener('change', function () {
                var file = input.files && input.files[0];
                if (!file) {
                    complete(0);
                    return;
                }
                // File names are used only under the request's private cache directory.
                if (!/^.+\.cgs\.zip$/i.test(file.name) || /[\/\\\x00-\x1f]/.test(file.name)) {
                    complete(3, file.name);
                    return;
                }
                input.disabled = true;
                try {
                    reader = new FileReader();
                    reader.onerror = function () { complete(2); };
                    reader.onabort = function () { complete(0); };
                    reader.onload = function () {
                        if (finished) return;
                        try {
                            FS.writeFile(directory + '/' + file.name, new Uint8Array(reader.result));
                        } catch (error) {
                            complete(2);
                            return;
                        }
                        complete(1, file.name);
                    };
                    reader.readAsArrayBuffer(file);
                } catch (error) {
                    complete(2);
                }
            });
            document.body.appendChild(dialog);
            dialog.showModal();
            input.focus();
        } catch (error) {
            complete(2);
        }
    }
});
