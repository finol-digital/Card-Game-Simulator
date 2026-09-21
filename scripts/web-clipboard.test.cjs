/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

// Run with: node --test scripts/web-clipboard.test.cjs
const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const { resolve } = require('node:path');
const { test } = require('node:test');
const vm = require('node:vm');

// Substitute only Emscripten's function-pointer macro; execute the actual plugin.
const source = readFileSync(resolve(__dirname,
    '../Assets/Scripts/UnityExtensionMethods/WebClipboard.jslib'), 'utf8')
    .replace(/\{\{\{\s*makeDynCall\(\s*['"]vii['"]\s*,\s*['"]callback['"]\s*\)\s*\}\}\}/g, 'callback');

function bridge(clipboard, secure = true) {
    const library = {};
    vm.runInNewContext(source, {
        LibraryManager: { library },
        mergeInto: Object.assign,
        UTF8ToString: value => value,
        window: { isSecureContext: secure },
        navigator: { clipboard }
    });
    return library.CgsCopyToClipboard;
}

test('does not claim success until the browser completes its write', async () => {
    let finish;
    let written;
    const results = [];
    const copy = bridge({ writeText(text) {
        written = text;
        return new Promise(resolve => { finish = resolve; });
    } });
    copy('Deck: 日本語\n4 cards', 7, (...args) => results.push(args));
    assert.equal(written, 'Deck: 日本語\n4 cards');
    assert.deepEqual(results, []);
    finish();
    await Promise.resolve();
    assert.deepEqual(results, [[7, 1]]);
});

test('permission rejection reports failure even if Unity cached the text', async () => {
    const results = [];
    const copy = bridge({ writeText: () => Promise.reject(new Error('NotAllowedError')) });
    copy('cached value', 8, (...args) => results.push(args));
    await Promise.resolve();
    assert.deepEqual(results, [[8, 0]]);
});

test('synchronous browser exceptions report failure', () => {
    const results = [];
    bridge({ writeText() { throw new Error('Clipboard unavailable'); } })(
        'text', 9, (...args) => results.push(args));
    assert.deepEqual(results, [[9, 0]]);
});

for (const clipboard of [undefined, {}]) {
    test(`missing clipboard ${clipboard ? 'method' : 'API'} reports failure`, () => {
        const results = [];
        bridge(clipboard)('text', 10, (...args) => results.push(args));
        assert.deepEqual(results, [[10, 0]]);
    });
}

test('insecure contexts fail without attempting a write', () => {
    const results = [];
    bridge({ writeText() { assert.fail('Must not write'); } }, false)(
        'text', 11, (...args) => results.push(args));
    assert.deepEqual(results, [[11, 0]]);
});

test('out-of-order writes report against the correct request', async () => {
    const finish = [];
    const results = [];
    const copy = bridge({ writeText: () => new Promise(resolve => finish.push(resolve)) });
    copy('first', 12, (...args) => results.push(args));
    copy('second', 13, (...args) => results.push(args));
    finish[1]();
    await Promise.resolve();
    finish[0]();
    await Promise.resolve();
    assert.deepEqual(results, [[13, 1], [12, 1]]);
});
