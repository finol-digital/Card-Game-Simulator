/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

mergeInto(LibraryManager.library, {
    CgsPickFiles: function (directoryPointer, titlePointer, cancelPointer, acceptPointer, folder, zipOnly, callback) {
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
            input.accept = UTF8ToString(acceptPointer);
            if (folder) {
                input.webkitdirectory = true;
                input.multiple = true;
            }
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
                var files = input.files ? Array.from(input.files) : [];
                if (!files.length) {
                    complete(0);
                    return;
                }
                var paths = [];
                var root;
                for (var i = 0; i < files.length; i++) {
                    var file = files[i];
                    var path = folder ? file.webkitRelativePath : file.name;
                    var parts = path ? path.split('/') : [];
                    // Never let user-controlled names escape the request's private cache directory.
                    if (!parts.length || parts.some(function (part) {
                        return !part || part === '.' || part === '..' || /[\\\x00-\x1f]/.test(part);
                    }) || parts[parts.length - 1] !== file.name ||
                        (folder && (parts.length < 2 || (root && root !== parts[0]))) ||
                        (!folder && parts.length !== 1) ||
                        (zipOnly && !/^.+\.cgs\.zip$/i.test(file.name))) {
                        complete(3, file.name);
                        return;
                    }
                    root = parts[0];
                    paths.push(path);
                }
                input.disabled = true;
                var index = 0;
                function readNext() {
                    if (finished) return;
                    if (index === files.length) {
                        complete(1, folder ? root : paths[0]);
                        return;
                    }
                    try {
                        reader = new FileReader();
                        reader.onerror = function () { complete(2); };
                        reader.onabort = function () { complete(0); };
                        reader.onload = function () {
                            if (finished) return;
                            try {
                                var path = directory + '/' + paths[index];
                                FS.mkdirTree(path.substring(0, path.lastIndexOf('/')));
                                FS.writeFile(path, new Uint8Array(reader.result));
                            } catch (error) {
                                complete(2);
                                return;
                            }
                            index++;
                            readNext();
                        };
                        reader.readAsArrayBuffer(files[index]);
                    } catch (error) {
                        complete(2);
                    }
                }
                readNext();
            });
            document.body.appendChild(dialog);
            dialog.showModal();
            input.focus();
        } catch (error) {
            complete(2);
        }
    },

    CgsDownloadFile: function (pathPointer, namePointer) {
        var url;
        var link;
        try {
            var bytes = FS.readFile(UTF8ToString(pathPointer));
            url = URL.createObjectURL(new Blob([bytes], { type: 'application/zip' }));
            link = document.createElement('a');
            link.href = url;
            link.download = UTF8ToString(namePointer);
            link.style.display = 'none';
            document.body.appendChild(link);
            link.click();
            return 1;
        } catch (error) {
            return 0;
        } finally {
            if (link) link.remove();
            // Keep the object URL alive long enough for the browser to consume the download.
            if (url) setTimeout(function () { URL.revokeObjectURL(url); }, 60000);
        }
    }
});
