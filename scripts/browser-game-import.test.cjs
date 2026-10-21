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
    '../Assets/Scripts/Cgs/Menu/BrowserGameImporter.jslib'), 'utf8')
    .replace(/\{\{\{\s*makeDynCall\('vii', 'callback'\)\s*\}\}\}/g, 'callback');

function picker(options = {}) {
    const library = {};
    const elements = [];
    const results = [];
    const writes = [];
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
        FS: { writeFile(path, bytes) {
            if (options.writeError) throw new Error('No space');
            writes.push({ path, bytes: Array.from(bytes) });
        } },
        lengthBytesUTF8: value => Buffer.byteLength(value),
        _malloc: () => 42,
        stringToUTF8: (value, pointer) => strings.set(pointer, value),
        _free: pointer => strings.delete(pointer)
    });
    library.CgsPickGameZip('/cache/request1', 'Select CGS ZIP File', 'Cancel',
        (status, pointer) => results.push([status, strings.get(pointer)]));
    const input = elements.find(item => item.tag === 'input');
    return {
        elements, results, writes, strings, previousFocus, input,
        dialog: elements.find(item => item.tag === 'dialog'),
        cancel: () => elements.find(item => item.tag === 'button').onclick(),
        select(name) { input.files = name ? [{ name }] : []; input.listeners.change(); },
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
