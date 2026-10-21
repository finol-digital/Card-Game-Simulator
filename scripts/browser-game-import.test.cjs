/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

// Run with: node --test scripts/browser-game-import.test.cjs
const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const { resolve } = require('node:path');
const { test } = require('node:test');
const vm = require('node:vm');

const source = readFileSync(resolve(__dirname,
    '../Assets/Scripts/Cgs/Menu/BrowserFileBrowser.jslib'), 'utf8')
    .replace(/\{\{\{\s*makeDynCall\('vii', 'callback'\)\s*\}\}\}/g, 'callback');

function picker(options = {}) {
    const library = {};
    const elements = [];
    const results = [];
    const writes = [];
    const directories = [];
    const strings = new Map();
    const previousFocus = { isConnected: true, focus() { this.restored = true; } };
    let reader;
    function element(tag) {
        const item = {
            tag, style: {}, children: [], listeners: {},
            setAttribute(name, value) { this[name] = value; },
            appendChild(child) { this.children.push(child); },
            addEventListener(name, handler) { this.listeners[name] = handler; },
            focus() {}, remove() { this.removed = true; },
            showModal() {
                if (options.dialogError) throw new Error('Dialog unavailable');
                this.open = true;
            }
        };
        elements.push(item);
        return item;
    }
    vm.runInNewContext(source, {
        LibraryManager: { library }, mergeInto: Object.assign, UTF8ToString: value => value,
        document: { activeElement: previousFocus, body: element('body'), createElement: element },
        FileReader: class {
            constructor() { reader = this; this.readyState = 0; }
            readAsArrayBuffer(file) {
                if (options.readError) throw new Error('Read denied');
                this.file = file;
                this.readyState = 1;
            }
            abort() { this.readyState = 2; this.onabort(); }
        },
        Uint8Array,
        FS: { mkdirTree(path) { directories.push(path); }, writeFile(path, bytes) {
            if (options.writeError) throw new Error('No space');
            writes.push({ path, bytes: Array.from(bytes) });
        } },
        lengthBytesUTF8: value => Buffer.byteLength(value),
        _malloc: () => 42,
        stringToUTF8: (value, pointer) => strings.set(pointer, value),
        _free: pointer => strings.delete(pointer)
    });
    library.CgsPickFiles('/cache/request1', 'Select CGS ZIP File', 'Cancel',
        options.accept || '.cgs.zip', options.folder ? 1 : 0, options.zipOnly === false ? 0 : 1,
        (status, pointer) => results.push([status, strings.get(pointer)]));
    const input = elements.find(item => item.tag === 'input');
    return {
        elements, results, writes, directories, strings, previousFocus, input,
        dialog: elements.find(item => item.tag === 'dialog'),
        cancel: () => elements.find(item => item.tag === 'button').onclick(),
        select(name) { input.files = name ? [{ name }] : []; input.listeners.change(); },
        selectFiles(files) { input.files = files; input.listeners.change(); },
        finish(bytes = [80, 75, 3, 4]) {
            reader.readyState = 2;
            reader.result = Uint8Array.from(bytes).buffer;
            reader.onload();
        },
        get reader() { return reader; }
    };
}

test('offers a visible, labelled native picker without requiring a Unity user gesture', () => {
    const view = picker();
    assert.equal(view.dialog.open, true);
    assert.equal(view.dialog['aria-label'], 'Select CGS ZIP File');
    assert.equal(view.input.type, 'file');
    assert.ok(view.input.accept.includes('.cgs.zip'));
    assert.deepEqual(view.results, []);
});

test('writes binary ZIP bytes before reporting the original Unicode filename', () => {
    const view = picker();
    view.select('日本語.CGS.ZIP');
    assert.deepEqual(view.results, []);
    view.finish([0, 255, 80, 75]);
    assert.deepEqual(view.writes, [{ path: '/cache/request1/日本語.CGS.ZIP', bytes: [0, 255, 80, 75] }]);
    assert.deepEqual(view.results, [[1, '日本語.CGS.ZIP']]);
    assert.equal(view.dialog.removed, true);
    assert.equal(view.previousFocus.restored, true);
    assert.equal(view.strings.size, 0);
});

for (const cancel of [view => view.cancel(), view => view.input.listeners.cancel(),
    view => view.dialog.listeners.cancel({ preventDefault() {} }), view => view.select(null)]) {
    test('cancellation reports once, removes the dialog, and writes nothing', () => {
        const view = picker();
        cancel(view);
        view.cancel();
        assert.deepEqual(view.results, [[0, undefined]]);
        assert.deepEqual(view.writes, []);
        assert.equal(view.dialog.removed, true);
    });
}

test('cancel during a read aborts it and ignores a late load event', () => {
    const view = picker();
    view.select('game.cgs.zip');
    view.cancel();
    view.finish();
    assert.deepEqual(view.results, [[0, undefined]]);
    assert.deepEqual(view.writes, []);
});

for (const name of ['game.zip', 'game.json', '.cgs.zip', '../game.cgs.zip', 'dir\\game.cgs.zip', 'bad\0.cgs.zip']) {
    test(`rejects invalid filename ${JSON.stringify(name)} before reading`, () => {
        const view = picker();
        view.select(name);
        assert.deepEqual(view.results, [[3, name]]);
        assert.equal(view.reader, undefined);
        assert.deepEqual(view.writes, []);
    });
}

for (const option of ['dialogError', 'readError', 'writeError']) {
    test(`${option} reports failure and removes the dialog`, () => {
        const view = picker({ [option]: true });
        if (option !== 'dialogError') view.select('game.cgs.zip');
        if (option === 'writeError') view.finish();
        assert.deepEqual(view.results, [[2, undefined]]);
        assert.equal(view.dialog.removed, true);
        assert.deepEqual(view.writes, []);
    });
}

test('asynchronous read failure reports once without importing a partial file', () => {
    const view = picker();
    view.select('game.cgs.zip');
    view.reader.onerror();
    view.finish();
    assert.deepEqual(view.results, [[2, undefined]]);
    assert.deepEqual(view.writes, []);
});

test('image selection accepts image files and preserves their bytes', () => {
    const view = picker({ zipOnly: false, accept: 'image/*' });
    assert.equal(view.input.accept, 'image/*');
    view.select('card + 日本語.png');
    view.finish([137, 80, 78, 71]);
    assert.deepEqual(view.results, [[1, 'card + 日本語.png']]);
    assert.deepEqual(view.writes[0].bytes, [137, 80, 78, 71]);
});

test('folder selection preserves the root name and nested file paths until every read completes', () => {
    const view = picker({ folder: true, zipOnly: false });
    assert.equal(view.input.webkitdirectory, true);
    assert.equal(view.input.multiple, true);
    view.selectFiles([
        { name: 'one.png', webkitRelativePath: 'My Set/one.png' },
        { name: 'two.png', webkitRelativePath: 'My Set/subfolder/two.png' }
    ]);
    view.finish([1]);
    assert.deepEqual(view.results, []);
    view.finish([2]);
    assert.deepEqual(view.results, [[1, 'My Set']]);
    assert.deepEqual(view.writes, [
        { path: '/cache/request1/My Set/one.png', bytes: [1] },
        { path: '/cache/request1/My Set/subfolder/two.png', bytes: [2] }
    ]);
});

for (const path of ['../card.png', '/Set/card.png', 'Set/../card.png', 'Set//card.png', 'Set\\bad/card.png']) {
    test(`rejects unsafe folder path ${JSON.stringify(path)} before reading`, () => {
        const view = picker({ folder: true, zipOnly: false });
        view.selectFiles([{ name: 'card.png', webkitRelativePath: path }]);
        assert.deepEqual(view.results, [[3, 'card.png']]);
        assert.deepEqual(view.writes, []);
    });
}

test('canceling a folder read ignores remaining files and late callbacks', () => {
    const view = picker({ folder: true, zipOnly: false });
    view.selectFiles([
        { name: 'one.png', webkitRelativePath: 'Set/one.png' },
        { name: 'two.png', webkitRelativePath: 'Set/two.png' }
    ]);
    view.finish([1]);
    view.cancel();
    view.finish([2]);
    assert.equal(view.writes.length, 1);
    assert.deepEqual(view.results, [[0, undefined]]);
});

function download(options = {}) {
    const library = {};
    const events = [];
    const timers = [];
    let blob;
    const link = { style: {}, click() {
        if (options.clickError) throw new Error('Download failed');
        events.push(['click', this.download, this.href]);
    }, remove() { events.push(['remove']); } };
    vm.runInNewContext(source, {
        LibraryManager: { library }, mergeInto: Object.assign, UTF8ToString: value => value,
        FS: { readFile(path) {
            if (options.readError) throw new Error('File missing');
            events.push(['read', path]);
            return new Uint8Array([80, 75, 0, 255]);
        } },
        Blob,
        URL: { createObjectURL(value) { blob = value; return 'blob:test'; },
            revokeObjectURL(url) { events.push(['revoke', url]); } },
        document: { createElement: () => link, body: { appendChild() {} } },
        setTimeout(callback) { timers.push(callback); }
    });
    const result = library.CgsDownloadFile('/exports/Game.cgs.zip', 'Game.cgs.zip');
    return { result, events, timers, blob };
}

test('Share downloads a ZIP Blob with the original filename and releases its URL after consumption', async () => {
    const view = download();
    assert.equal(view.result, 1);
    assert.equal(view.blob.type, 'application/zip');
    assert.deepEqual(Array.from(new Uint8Array(await view.blob.arrayBuffer())), [80, 75, 0, 255]);
    assert.deepEqual(view.events, [['read', '/exports/Game.cgs.zip'], ['click', 'Game.cgs.zip', 'blob:test'], ['remove']]);
    view.timers[0]();
    assert.deepEqual(view.events.at(-1), ['revoke', 'blob:test']);
});

for (const option of ['readError', 'clickError']) {
    test(`Share handles ${option} without throwing into Unity`, () => {
        const view = download({ [option]: true });
        assert.equal(view.result, 0);
        if (option === 'clickError') {
            view.timers[0]();
            assert.deepEqual(view.events.at(-1), ['revoke', 'blob:test']);
        }
    });
}
