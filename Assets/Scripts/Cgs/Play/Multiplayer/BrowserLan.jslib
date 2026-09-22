/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

mergeInto(LibraryManager.library, {
    $CgsBrowserLan: {
        discovery: null,
        browsing: false,
        rooms: {},
        dirty: false,
        connection: null,
        events: [],
        timer: null,
        pageHide: null,

        open: function () {
            if (this.discovery) return true;
            try {
                this.discovery = new BroadcastChannel('cgs-lan-discovery-v1');
                var self = this;
                this.discovery.onmessage = function (event) {
                    var m = event.data;
                    if (!m || typeof m.room !== 'string') return;
                    var c = self.connection;
                    if (m.type === 'query' && c && c.server) self.advertise();
                    if (!self.browsing || !/^[a-f0-9]{32}$/.test(m.room)) return;
                    if (m.type === 'room' && typeof m.name === 'string') {
                        var previous = self.rooms[m.room];
                        self.rooms[m.room] = { ServerName: m.name, Port: 0, seen: Date.now() };
                        // Heartbeats refresh expiry without rebuilding the selected lobby row.
                        if (!previous || previous.ServerName !== m.name) self.dirty = true;
                    } else if (m.type === 'gone') {
                        delete self.rooms[m.room];
                        self.dirty = true;
                    }
                };
                this.timer = setInterval(function () { self.tick(); }, 2000);
                this.pageHide = function () { self.shutdown(); self.stopDiscovery(); };
                window.addEventListener('pagehide', this.pageHide);
                return true;
            } catch (error) {
                this.closeIfUnused();
                return false;
            }
        },

        advertise: function () {
            var c = this.connection;
            if (c && c.server) this.discovery.postMessage({ type: 'room', room: c.room, name: c.name });
        },

        discover: function () {
            if (!this.open()) return false;
            this.browsing = true;
            this.discovery.postMessage({ type: 'query', room: '' });
            return true;
        },

        stopDiscovery: function () {
            this.browsing = false;
            this.rooms = {};
            this.dirty = true;
            this.closeIfUnused();
        },

        start: function (room, name, server) {
            this.shutdown();
            if (!/^[a-f0-9]{32}$/.test(room) || !this.open()) return false;
            try {
                var channel = new BroadcastChannel('cgs-lan-room-v1-' + room);
                var token = server ? 'host' : Array.from(crypto.getRandomValues(new Uint32Array(4))).join('-');
                var c = { channel: channel, room: room, name: name, server: server,
                    token: token, peers: new Map(), nextId: 1, connected: false, started: Date.now() };
                this.connection = c;
                var self = this;
                channel.onmessage = function (event) {
                    if (self.connection === c) self.receive(event.data);
                };
                if (server) this.advertise();
                else this.post('join', 'host');
                return true;
            } catch (error) {
                this.shutdown();
                return false;
            }
        },

        post: function (type, to, data) {
            var c = this.connection;
            if (c) c.channel.postMessage({ type: type, from: c.token, to: to, data: data });
        },

        enqueue: function (type, clientId, data) {
            // Values match Unity.Netcode.NetworkEvent: Data=0, Connect=1, Disconnect=2.
            this.events.push({ Type: type, ClientId: clientId, Data: data });
        },

        receive: function (m) {
            var c = this.connection;
            if (!c || !m || m.to !== c.token || typeof m.from !== 'string' || m.from === c.token) return;
            if (c.server && m.type === 'join') {
                var existing = c.peers.get(m.from);
                if (!existing) {
                    if (c.peers.size >= 9) { this.post('leave', m.from); return; }
                    existing = { id: c.nextId++, seen: Date.now() };
                    c.peers.set(m.from, existing);
                    this.enqueue(1, existing.id);
                }
                this.post('welcome', m.from);
                return;
            }
            if (!c.server && m.from === 'host' && m.type === 'welcome' && !c.connected) {
                c.connected = true;
                c.peers.set('host', { id: 0, seen: Date.now() });
                this.enqueue(1, 0);
                return;
            }
            if (!c.server && !c.connected && m.from === 'host' && m.type === 'leave') {
                this.shutdown();
                this.enqueue(2, 0);
                return;
            }
            var peer = c.peers.get(m.from);
            if (!peer) return;
            peer.seen = Date.now();
            if (m.type === 'data' && typeof m.data === 'string' &&
                m.data.length <= 16 * 1024 * 1024 && m.data.length % 4 === 0 &&
                !/[^A-Za-z0-9+/]/.test(m.data.replace(/={1,2}$/, ''))) {
                this.enqueue(0, peer.id, m.data);
            } else if (m.type === 'leave') {
                c.peers.delete(m.from);
                if (!c.server) this.shutdown();
                this.enqueue(2, peer.id);
            }
        },

        send: function (id, data) {
            var c = this.connection;
            if (!c) return;
            var self = this;
            c.peers.forEach(function (peer, token) {
                if (peer.id === id) self.post('data', token, data);
            });
        },

        disconnect: function (id) {
            var c = this.connection;
            if (!c) return;
            if (!c.server) { this.shutdown(); return; }
            var self = this;
            c.peers.forEach(function (peer, token) {
                if (peer.id === id) {
                    self.post('leave', token);
                    c.peers.delete(token);
                }
            });
        },

        tick: function () {
            var now = Date.now();
            var self = this;
            Object.keys(this.rooms).forEach(function (room) {
                if (now - self.rooms[room].seen > 15000) {
                    delete self.rooms[room];
                    self.dirty = true;
                }
            });
            var c = this.connection;
            if (!c) return;
            if (c.server) this.advertise();
            if (!c.server && !c.connected) {
                if (now - c.started > 10000) {
                    this.shutdown();
                    this.enqueue(2, 0);
                } else this.post('join', 'host');
                return;
            }
            c.peers.forEach(function (peer, token) {
                // Background tabs may throttle timers. Allow a full minute of silence.
                if (now - peer.seen > 60000) {
                    self.post('leave', token);
                    c.peers.delete(token);
                    if (!c.server) self.shutdown();
                    self.enqueue(2, peer.id);
                } else self.post('ping', token);
            });
        },

        shutdown: function () {
            var c = this.connection;
            if (c) {
                var self = this;
                c.peers.forEach(function (peer, token) { self.post('leave', token); });
                // A pending join must also be withdrawn from the host.
                if (!c.server && !c.connected) this.post('leave', 'host');
                if (c.server) this.discovery.postMessage({ type: 'gone', room: c.room });
                c.channel.close();
                this.connection = null;
            }
            this.events = [];
            this.closeIfUnused();
        },

        closeIfUnused: function () {
            if (this.connection || this.browsing) return;
            if (this.discovery) this.discovery.close();
            this.discovery = null;
            if (this.timer !== null) clearInterval(this.timer);
            this.timer = null;
            if (this.pageHide) window.removeEventListener('pagehide', this.pageHide);
            this.pageHide = null;
        },

        stringResult: function (value) {
            if (!value) return 0;
            var length = lengthBytesUTF8(value) + 1;
            var pointer = _malloc(length);
            stringToUTF8(value, pointer, length);
            return pointer;
        }
    },

    CgsBrowserLanStart__deps: ['$CgsBrowserLan'],
    CgsBrowserLanStart: function (room, name, server) {
        return CgsBrowserLan.start(UTF8ToString(room), UTF8ToString(name), server !== 0) ? 1 : 0;
    },
    CgsBrowserLanSend__deps: ['$CgsBrowserLan'],
    CgsBrowserLanSend: function (id, data) { CgsBrowserLan.send(id, UTF8ToString(data)); },
    CgsBrowserLanPoll__deps: ['$CgsBrowserLan'],
    CgsBrowserLanPoll: function () {
        var event = CgsBrowserLan.events.shift();
        return CgsBrowserLan.stringResult(event ? JSON.stringify(event) : null);
    },
    CgsBrowserLanDiscover__deps: ['$CgsBrowserLan'],
    CgsBrowserLanDiscover: function () { return CgsBrowserLan.discover() ? 1 : 0; },
    CgsBrowserLanRooms__deps: ['$CgsBrowserLan'],
    CgsBrowserLanRooms: function () {
        if (!CgsBrowserLan.dirty) return 0;
        CgsBrowserLan.dirty = false;
        return CgsBrowserLan.stringResult(JSON.stringify(CgsBrowserLan.rooms));
    },
    CgsBrowserLanStopDiscovery__deps: ['$CgsBrowserLan'],
    CgsBrowserLanStopDiscovery: function () { CgsBrowserLan.stopDiscovery(); },
    CgsBrowserLanDisconnect__deps: ['$CgsBrowserLan'],
    CgsBrowserLanDisconnect: function (id) { CgsBrowserLan.disconnect(id); },
    CgsBrowserLanShutdown__deps: ['$CgsBrowserLan'],
    CgsBrowserLanShutdown: function () { CgsBrowserLan.shutdown(); }
});
