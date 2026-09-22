/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

// Run with: node --test scripts/browser-lan.test.cjs
const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const { resolve } = require('node:path');
const { test } = require('node:test');
const vm = require('node:vm');
const { webcrypto } = require('node:crypto');
const source = readFileSync(resolve(__dirname,
    '../Assets/Scripts/Cgs/Play/Multiplayer/BrowserLan.jslib'), 'utf8');
const room = 'a'.repeat(32);

function tab(t, options = {}) {
    const library = {};
    const listeners = new Map();
    const clock = { now: 1000 };
    const context = {
        LibraryManager: { library }, mergeInto: Object.assign,
        BroadcastChannel: options.unavailable ? undefined : BroadcastChannel,
        crypto: webcrypto, Date: { now: () => clock.now },
        setInterval: () => 1, clearInterval() {},
        window: {
            addEventListener: (name, fn) => listeners.set(name, fn),
            removeEventListener: name => listeners.delete(name)
        },
        UTF8ToString: value => value,
        lengthBytesUTF8: value => Buffer.byteLength(value),
        _malloc: () => 123,
        stringToUTF8: value => { context.result = value; }
    };
    vm.runInNewContext(source, context);
    context.CgsBrowserLan = library.$CgsBrowserLan;
    const bridge = context.CgsBrowserLan;
    t.after(() => { bridge.shutdown(); bridge.stopDiscovery(); });
    return { bridge, library, clock, listeners, context };
}

async function until(condition) {
    const deadline = Date.now() + 2000;
    while (!condition()) {
        assert.ok(Date.now() < deadline, 'BroadcastChannel response timed out');
        await new Promise(resolve => setTimeout(resolve, 5));
    }
}

async function pair(t) {
    const host = tab(t);
    const client = tab(t);
    assert.equal(host.bridge.start(room, 'Cards 日本語', true), true);
    assert.equal(client.bridge.start(room, '', false), true);
    await until(() => client.bridge.events.length && host.bridge.events.length);
    assert.equal(client.bridge.events.shift().ClientId, 0);
    assert.equal(host.bridge.events.shift().ClientId, 1);
    return { host, client };
}

test('discovers a hosting tab and removes its room when hosting stops', async t => {
    const host = tab(t).bridge;
    const browser = tab(t);
    host.start(room, 'Cards 日本語', true);
    browser.library.CgsBrowserLanDiscover();
    await until(() => browser.bridge.rooms[room]);
    browser.library.CgsBrowserLanRooms();
    assert.equal(JSON.parse(browser.context.result)[room].ServerName, 'Cards 日本語');
    host.shutdown();
    await until(() => !browser.bridge.rooms[room]);
});

test('room heartbeats refresh expiry without rebuilding an unchanged lobby list', async t => {
    const host = tab(t).bridge;
    const browser = tab(t);
    host.start(room, 'Cards', true);
    browser.bridge.discover();
    await until(() => browser.bridge.rooms[room]);
    browser.library.CgsBrowserLanRooms();
    browser.clock.now += 5000;
    host.advertise();
    await until(() => browser.bridge.rooms[room].seen === browser.clock.now);
    assert.equal(browser.bridge.dirty, false);
    host.connection.name = 'Renamed';
    host.advertise();
    await until(() => browser.bridge.dirty);
    assert.equal(browser.bridge.rooms[room].ServerName, 'Renamed');
});

test('carries ordered binary Netcode packets both ways, including large payloads', async t => {
    const { host, client } = await pair(t);
    const bytes = Buffer.alloc(512 * 1024);
    for (let i = 0; i < bytes.length; i++) bytes[i] = i % 256;
    client.library.CgsBrowserLanSend(0, bytes.toString('base64'));
    client.library.CgsBrowserLanSend(0, 'AQID');
    await until(() => host.bridge.events.length === 2);
    host.library.CgsBrowserLanPoll();
    const received = JSON.parse(host.context.result);
    assert.equal(received.Type, 0);
    assert.equal(received.ClientId, 1);
    assert.deepEqual(Buffer.from(received.Data, 'base64'), bytes);
    assert.equal(host.bridge.events.shift().Data, 'AQID');
    host.library.CgsBrowserLanSend(1, 'BAUG');
    await until(() => client.bridge.events.length === 1);
    assert.equal(client.bridge.events.shift().Data, 'BAUG');
});

test('assigns distinct client IDs and targets packets only to their recipient', async t => {
    const { host, client } = await pair(t);
    const second = tab(t).bridge;
    second.start(room, '', false);
    await until(() => second.events.length && host.bridge.events.length);
    assert.equal(host.bridge.events.shift().ClientId, 2);
    second.events.shift();
    host.bridge.send(2, 'AQ==');
    await until(() => second.events.length);
    assert.equal(client.bridge.events.length, 0);
    assert.equal(second.events.shift().Data, 'AQ==');
    // Retried joins must not generate another Netcode Connect event.
    second.post('join', 'host');
    await new Promise(resolve => setTimeout(resolve, 20));
    assert.equal(host.bridge.events.length, 0);
    assert.equal(second.events.length, 0);
});

test('isolates rooms and rejects non-member or malformed data', async t => {
    const { host, client } = await pair(t);
    const other = tab(t).bridge;
    other.start('b'.repeat(32), '', true);
    other.send(1, 'AQ==');
    host.bridge.receive({ to: 'host', from: 'unknown', type: 'data', data: 'AQ==' });
    client.bridge.post('data', 'host', 'not base64!');
    await new Promise(resolve => setTimeout(resolve, 20));
    assert.equal(host.bridge.events.length, 0);
    assert.equal(client.bridge.events.length, 0);
});

test('host shutdown disconnects clients and permits a clean new connection', async t => {
    const { host, client } = await pair(t);
    host.bridge.shutdown();
    await until(() => client.bridge.events.length);
    assert.equal(client.bridge.events.shift().Type, 2);
    assert.equal(client.bridge.connection, null);
    host.bridge.start(room, 'Rehost', true);
    client.bridge.start(room, '', false);
    await until(() => client.bridge.events.length);
    assert.equal(client.bridge.events.shift().Type, 1);
});

test('closing a client tab notifies the host and releases channels and listeners', async t => {
    const { host, client } = await pair(t);
    client.listeners.get('pagehide')();
    await until(() => host.bridge.events.length);
    assert.equal(host.bridge.events.shift().Type, 2);
    assert.equal(host.bridge.connection.peers.size, 0);
    assert.equal(client.bridge.discovery, null);
    assert.equal(client.listeners.size, 0);
});

test('host can disconnect one client without stopping hosting', async t => {
    const { host, client } = await pair(t);
    host.library.CgsBrowserLanDisconnect(1);
    await until(() => client.bridge.events.length);
    assert.equal(client.bridge.events.shift().Type, 2);
    assert.equal(host.bridge.connection.server, true);
    assert.equal(host.bridge.connection.peers.size, 0);
});

test('missing hosts time out and stale rooms expire', t => {
    const client = tab(t);
    client.bridge.start(room, '', false);
    client.clock.now += 11000;
    client.bridge.tick();
    assert.equal(client.bridge.events.shift().Type, 2);
    assert.equal(client.bridge.connection, null);
    client.bridge.rooms[room] = { seen: 1000 };
    client.clock.now += 16000;
    client.bridge.tick();
    assert.equal(Object.keys(client.bridge.rooms).length, 0);
});

test('silent peers time out but brief background throttling is tolerated', async t => {
    const { host } = await pair(t);
    host.clock.now += 30000;
    host.bridge.tick();
    assert.equal(host.bridge.events.length, 0);
    host.clock.now += 31000;
    host.bridge.tick();
    assert.equal(host.bridge.events.shift().Type, 2);
    assert.equal(host.bridge.connection.peers.size, 0);
});

test('unsupported browsers and invalid room IDs fail without leaked resources', t => {
    const unsupported = tab(t, { unavailable: true }).bridge;
    assert.equal(unsupported.start(room, '', true), false);
    assert.equal(unsupported.discover(), false);
    assert.equal(unsupported.discovery, null);
    const client = tab(t).bridge;
    assert.equal(client.start('127.0.0.1', '', false), false);
    assert.equal(client.discovery, null);
});

test('a full room rejects the extra client and can accept another after a departure', async t => {
    const host = tab(t).bridge;
    host.start(room, 'Full room', true);
    const clients = Array.from({ length: 10 }, () => tab(t).bridge);
    for (let i = 0; i < 9; i++) {
        clients[i].start(room, '', false);
        await until(() => clients[i].events.length);
        assert.equal(clients[i].events.shift().Type, 1);
    }
    clients[9].start(room, '', false);
    await until(() => clients[9].events.length);
    assert.equal(clients[9].events.shift().Type, 2);
    assert.equal(host.connection.peers.size, 9);
    clients[0].shutdown();
    await until(() => host.connection.peers.size === 8);
    clients[9].start(room, '', false);
    await until(() => clients[9].events.length);
    assert.equal(clients[9].events.shift().Type, 1);
    assert.equal(host.connection.peers.size, 9);
});

test('discovery sees a host started later and does not close its active network channel', async t => {
    const browser = tab(t).bridge;
    browser.discover();
    const host = tab(t).bridge;
    host.start(room, 'New host', true);
    await until(() => browser.rooms[room]);
    browser.start(room, '', false);
    await until(() => browser.events.length);
    browser.events.shift();
    browser.stopDiscovery();
    host.send(1, 'AQ==');
    await until(() => browser.events.length);
    assert.equal(browser.events.shift().Data, 'AQ==');
});
