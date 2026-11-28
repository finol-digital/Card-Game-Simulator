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
// Emscripten expands makeDynCall when building the Web player; tests call the function directly.
const source = readFileSync(resolve(__dirname,
    '../Assets/Scripts/Cgs/Play/Multiplayer/BrowserLan.jslib'), 'utf8')
    .replace(/\{\{\{ makeDynCall\('vii', 'callback'\) \}\}\}/g, 'callback');
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

// WebRTC links between devices. Each device gets its own BroadcastChannel stub that reaches no one, so every
// message must travel through the (fake) data channels created by signaling.
class IsolatedChannel { postMessage() {} close() {} }

function fakeRtc() {
    const offers = new Map();
    let nextId = 1;
    class FakeChannel {
        constructor(label) { this.label = label; this.readyState = 'connecting'; this.bufferedAmount = 0; this.sent = []; }
        send(data) {
            if (this.readyState !== 'open') throw new Error('InvalidStateError');
            this.sent.push(data);
            const peer = this.peer;
            setTimeout(() => { if (peer.readyState === 'open' && peer.onmessage) peer.onmessage({ data }); }, 0);
        }
        close() {
            for (const channel of [this, this.peer]) {
                if (!channel || channel.readyState === 'closed') continue;
                channel.readyState = 'closed';
                if (channel.onclose) setTimeout(() => channel.onclose(), 0);
            }
        }
    }
    class FakePeerConnection {
        constructor(config) {
            this.config = config; this.iceGatheringState = 'new'; this.localDescription = null;
            this.listeners = {}; this.closed = false;
        }
        addEventListener(name, fn) { this.listeners[name] = fn; }
        createDataChannel(label) { this.channel = new FakeChannel(label); return this.channel; }
        async createOffer() { const sdp = 'offer-' + nextId++; offers.set(sdp, this); return { type: 'offer', sdp }; }
        async createAnswer() { return { type: 'answer', sdp: 'answer-' + nextId++ }; }
        async setLocalDescription(description) {
            this.localDescription = description;
            this.iceGatheringState = 'complete';
        }
        async setRemoteDescription(description) {
            if (description.type === 'offer') {
                const client = offers.get(description.sdp);
                if (!client) throw new Error('unknown offer');
                this.client = client;
                client.host = this;
            } else if (description.type === 'answer') {
                const host = this.host;
                if (!host || !host.localDescription || host.localDescription.sdp !== description.sdp)
                    throw new Error('mismatched answer');
                const remote = new FakeChannel(this.channel.label);
                this.channel.peer = remote;
                remote.peer = this.channel;
                setTimeout(() => {
                    host.ondatachannel({ channel: remote });
                    for (const channel of [this.channel, remote]) {
                        channel.readyState = 'open';
                        if (channel.onopen) channel.onopen();
                    }
                }, 0);
            }
        }
        close() {
            this.closed = true;
            if (this.channel) this.channel.close();
        }
    }
    return FakePeerConnection;
}

function device(t, RTCPeerConnection) {
    const library = {};
    const context = {
        LibraryManager: { library }, mergeInto: Object.assign, BroadcastChannel: IsolatedChannel,
        RTCPeerConnection, crypto: webcrypto, TextEncoder, Date, setTimeout, clearTimeout,
        setInterval: () => 1, clearInterval() {},
        window: { addEventListener() {}, removeEventListener() {} },
        UTF8ToString: value => value, lengthBytesUTF8: value => Buffer.byteLength(value),
        _malloc: () => 123, _free() {}, stringToUTF8: value => { context.result = value; }
    };
    vm.runInNewContext(source, context);
    const bridge = library.$CgsBrowserLan;
    context.CgsBrowserLan = bridge;
    t.after(() => bridge.shutdown());
    return { bridge, library, context };
}

async function signaledPair(t) {
    const Rtc = fakeRtc();
    const host = device(t, Rtc);
    const client = device(t, Rtc);
    assert.equal(host.bridge.start(room, 'Cards', true), true);
    const offer = await client.bridge.offer(room);
    assert.match(offer, /^offer-/);
    const answer = await host.bridge.answer(offer);
    assert.match(answer, /^answer-/);
    assert.equal(await client.bridge.accept(room, answer), true);
    assert.equal(client.bridge.start(room, '', false), true);
    await until(() => client.bridge.events.length && host.bridge.events.length);
    assert.deepEqual(JSON.parse(JSON.stringify(client.bridge.events.shift())), { Type: 1, ClientId: 0 });
    assert.deepEqual(JSON.parse(JSON.stringify(host.bridge.events.shift())), { Type: 1, ClientId: 1 });
    return { host, client };
}

test('a signaled WebRTC link connects devices that share no BroadcastChannel', async t => {
    await signaledPair(t);
});

test('WebRTC links carry large packets intact and in order in both directions', async t => {
    const { host, client } = await signaledPair(t);
    const large = Buffer.alloc(300 * 1024, 7).toString('base64');
    client.bridge.send(0, 'AQ==');
    client.bridge.send(0, large);
    client.bridge.send(0, 'Ag==');
    await until(() => host.bridge.events.length === 3);
    assert.deepEqual(Array.from(host.bridge.events, e => e.Data), ['AQ==', large, 'Ag==']);
    host.bridge.send(1, large);
    await until(() => client.bridge.events.length === 1);
    assert.equal(client.bridge.events[0].Data, large);
    const link = host.bridge.connection.links.values().next().value;
    assert.ok(link.channel.sent.length > 20, 'large packets are split into chunks');
    assert.ok(link.channel.sent.every(chunk => chunk.length <= 16001), 'chunks stay within data channel limits');
});

test('sending waits while a data channel buffer is full', async t => {
    const { host, client } = await signaledPair(t);
    const link = client.bridge.connection.links.get('host');
    link.channel.bufferedAmount = 5 * 1024 * 1024;
    client.bridge.send(0, 'AQ==');
    assert.equal(link.queue.length, 1);
    link.channel.bufferedAmount = 0;
    link.channel.onbufferedamountlow();
    await until(() => host.bridge.events.length === 1);
    assert.equal(link.queue.length, 0);
});

test('a data channel cannot speak for a different player', async t => {
    const { host, client } = await signaledPair(t);
    const link = host.bridge.connection.links.values().next().value;
    host.bridge.receive({ type: 'data', from: 'someone-else', to: 'host', data: 'AQ==' }, link);
    host.bridge.receive({ type: 'join', from: 'another-player', to: 'host' }, link);
    assert.equal(host.bridge.events.length, 0);
    assert.equal(host.bridge.connection.peers.size, 1);
    assert.ok(client);
});

test('disconnects travel over WebRTC, and a closed data channel removes its player', async t => {
    const first = await signaledPair(t);
    first.host.bridge.disconnect(1);
    await until(() => first.client.bridge.events.length);
    assert.equal(first.client.bridge.events.shift().Type, 2);

    const second = await signaledPair(t);
    second.client.bridge.connection.links.get('host').channel.close();
    await until(() => second.host.bridge.events.length);
    assert.deepEqual(JSON.parse(JSON.stringify(second.host.bridge.events.shift())), { Type: 2, ClientId: 1 });
});

test('signaling fails cleanly without WebRTC or with a wrong answer', async t => {
    const plain = device(t, undefined);
    assert.equal(await plain.bridge.offer(room), null);
    assert.equal(await plain.bridge.networkKey(), null);
    const Rtc = fakeRtc();
    const client = device(t, Rtc);
    await client.bridge.offer(room);
    assert.equal(await client.bridge.accept(room, 'answer-999'), false);
    assert.equal(client.bridge.pendingLinks[room], undefined);
    assert.equal(await client.bridge.accept(room, 'anything'), false);
});

test('network keys hash the public IPv4 address, or the IPv6 /64 prefix', async t => {
    const { bridge } = device(t, undefined);
    assert.equal(bridge.networkPrefix('203.0.113.7'), '203.0.113.7');
    assert.equal(bridge.networkPrefix('2001:DB8:0:1::5'), '2001:db8:0:1');
    assert.equal(bridge.networkPrefix('2001:db8::1'), '2001:db8:0:0');
    assert.equal(bridge.networkPrefix('2001:0db8:0000:0001:aaaa:bbbb:cccc:dddd'), '2001:db8:0:1');

    function stun(candidates) {
        return class {
            createDataChannel() {}
            async createOffer() { return {}; }
            async setLocalDescription() {
                for (const candidate of candidates) this.onicecandidate({ candidate: { candidate } });
                this.onicecandidate({ candidate: null });
            }
            close() {}
        };
    }
    const host = 'candidate:1 1 udp 2122260223 abc.local 50000 typ host generation 0';
    const v4 = 'candidate:2 1 udp 1686052607 203.0.113.7 50001 typ srflx raddr 0.0.0.0 rport 0 generation 0';
    const v6 = 'candidate:3 1 udp 1686052863 2001:db8:0:1::5 50002 typ srflx raddr :: rport 0 generation 0';
    const v6Other = 'candidate:3 1 udp 1686052863 2001:db8:0:1::9 50002 typ srflx raddr :: rport 0 generation 0';
    const key = async candidates => device(t, stun(candidates)).bridge.networkKey();
    const both = await key([host, v6, v4]);
    assert.match(both, /^[a-f0-9]{32}$/);
    assert.equal(await key([v4]), both, 'IPv4 is preferred when both are available');
    assert.equal(await key([v6]), await key([v6Other]), 'devices on one IPv6 network share a key');
    assert.notEqual(await key([v6]), both);
    assert.equal(await key([host]), null, 'no public address means no network discovery');
});

test('a host releases the peer connection of a departed WebRTC client', async t => {
    const closed = await signaledPair(t);
    const closedPc = closed.host.bridge.connection.rtc[0];
    closed.client.bridge.connection.links.get('host').channel.close();
    await until(() => closed.host.bridge.events.length);
    assert.equal(closed.host.bridge.connection.rtc.length, 0);
    await until(() => closedPc.closed);

    const kicked = await signaledPair(t);
    const kickedPc = kicked.host.bridge.connection.rtc[0];
    kicked.host.bridge.disconnect(1);
    assert.equal(kicked.host.bridge.connection.rtc.length, 0);
    await until(() => kickedPc.closed);
});

test('long descriptions keep only host candidates, and fail if still too long for Lobby', t => {
    const { bridge } = device(t, undefined);
    const host = 'a=candidate:1 1 udp 2122260223 abc.local 50000 typ host generation 0';
    const srflx = 'a=candidate:2 1 udp 1686052607 203.0.113.7 50001 typ srflx raddr 0.0.0.0 rport 0';
    const header = 'v=0\r\no=- 1 2 IN IP4 127.0.0.1\r\ns=-';
    const short = [header, host, srflx].join('\r\n');
    assert.equal(bridge.compact(short), short, 'descriptions that fit are unchanged');
    const long = [header, host].concat(new Array(40).fill(srflx)).join('\r\n');
    assert.equal(bridge.compact(long), [header, host].join('\r\n'));
    assert.equal(bridge.compact([header].concat(new Array(40).fill(host)).join('\r\n')), null);
});

test('network keys retry after a WebRTC setup error and expire after a while', async t => {
    let failing = true;
    class Stun {
        constructor() { if (failing) throw new Error('NotSupportedError'); }
        createDataChannel() {}
        async createOffer() { return {}; }
        async setLocalDescription() {
            this.onicecandidate({ candidate: { candidate:
                'candidate:2 1 udp 1686052607 203.0.113.7 50001 typ srflx raddr 0.0.0.0 rport 0' } });
            this.onicecandidate({ candidate: null });
        }
        close() {}
    }
    const { bridge } = device(t, Stun);
    assert.equal(await bridge.networkKey(), null);
    failing = false;
    const first = bridge.networkKey();
    assert.match(await first, /^[a-f0-9]{32}$/);
    assert.equal(bridge.networkKey(), first, 'a recent key is reused');
    bridge.networkKeyFound -= bridge.networkKeyMilliseconds;
    assert.notEqual(bridge.networkKey(), first, 'an old key is looked up again');
});

test('a fresh network key request looks the key up again', async t => {
    let lookups = 0;
    class Stun {
        constructor() { lookups++; }
        createDataChannel() {}
        async createOffer() { return {}; }
        async setLocalDescription() {
            this.onicecandidate({ candidate: { candidate:
                'candidate:2 1 udp 1686052607 203.0.113.7 50001 typ srflx raddr 0.0.0.0 rport 0' } });
            this.onicecandidate({ candidate: null });
        }
        close() {}
    }
    const { bridge } = device(t, Stun);
    const first = bridge.networkKey();
    assert.equal(bridge.networkKey(), first, 'a lookup in progress is shared');
    const fresh = bridge.networkKey(true);
    assert.notEqual(fresh, first, 'a fresh lookup does not join one that began on another network');
    await Promise.all([first, fresh]);
    assert.equal(bridge.networkKey(), fresh, 'later requests reuse the newest key');
    assert.notEqual(bridge.networkKey(true), fresh);
    assert.equal(lookups, 3);
});
