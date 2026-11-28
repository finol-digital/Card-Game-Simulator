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
        // Players on other devices connect over WebRTC data channels; signaling travels through Unity Lobby.
        // STUN only reveals this network's public address for discovery; LAN peers connect directly.
        rtcConfig: { iceServers: [{ urls: ['stun:stun.l.google.com:19302', 'stun:stun.cloudflare.com:3478'] }] },
        pendingLinks: {},
        networkKeyPromise: null,
        chunkSize: 16000,
        maxBuffered: 4 * 1024 * 1024,
        maxMessage: 24 * 1024 * 1024,

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
                    token: token, peers: new Map(), nextId: 1, connected: false, started: Date.now(),
                    links: new Map(), rtc: [] };
                this.connection = c;
                var pending = this.pendingLinks[room];
                if (!server && pending) {
                    // A signaled WebRTC link reaches a host on another device; use it instead of BroadcastChannel.
                    delete this.pendingLinks[room];
                    c.links.set('host', pending.link);
                    c.rtc.push(pending.pc);
                }
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
            if (!c) return;
            var message = { type: type, from: c.token, to: to, data: data };
            var link = c.links.get(to);
            if (link) this.linkSend(link, message);
            else c.channel.postMessage(message);
        },

        enqueue: function (type, clientId, data) {
            // Values match Unity.Netcode.NetworkEvent: Data=0, Connect=1, Disconnect=2.
            this.events.push({ Type: type, ClientId: clientId, Data: data });
        },

        receive: function (m, link) {
            var c = this.connection;
            if (!c || !m || m.to !== c.token || typeof m.from !== 'string' || m.from === c.token) return;
            if (link) {
                // A data channel speaks only for the peer that joined through it.
                if (c.server && m.type === 'join' && !link.token && !c.links.has(m.from)) {
                    link.token = m.from;
                    c.links.set(m.from, link);
                }
                if (c.links.get(m.from) !== link) return;
            }
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
                    self.closeLink(c.links.get(token));
                    c.links.delete(token);
                }
            });
        },

        tick: function () {
            var now = Date.now();
            var self = this;
            Object.keys(this.pendingLinks).forEach(function (room) {
                if (now - self.pendingLinks[room].created > 60000) self.discardPending(room);
            });
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
                if (c.rtc.length) {
                    // Let queued leave messages reach the data channels before closing them.
                    c.links.forEach(function (link) { self.flush(link); });
                    setTimeout(function () {
                        c.rtc.forEach(function (pc) { try { pc.close(); } catch (error) { } });
                    }, 250);
                }
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

        makeLink: function (pc, channel) {
            var self = this;
            var link = { pc: pc, channel: channel, partial: '', queue: [], token: null };
            channel.bufferedAmountLowThreshold = 1024 * 1024;
            channel.onmessage = function (event) {
                if (typeof event.data !== 'string' || !event.data.length) return;
                var kind = event.data.charAt(0);
                link.partial += event.data.substring(1);
                if (link.partial.length > self.maxMessage) { link.partial = ''; return; }
                if (kind !== 'F') return;
                var text = link.partial;
                link.partial = '';
                var message;
                try { message = JSON.parse(text); } catch (error) { return; }
                self.receive(message, link);
            };
            channel.onopen = function () { self.flush(link); };
            channel.onbufferedamountlow = function () { self.flush(link); };
            channel.onclose = function () { self.linkClosed(link); };
            return link;
        },

        linkSend: function (link, message) {
            // Data channels limit message sizes, so long messages travel in ordered chunks: P(artial)... F(inal).
            var text = JSON.stringify(message);
            var offset = 0;
            do {
                var end = offset + this.chunkSize;
                link.queue.push((end >= text.length ? 'F' : 'P') + text.substring(offset, end));
                offset = end;
            } while (offset < text.length);
            this.flush(link);
        },

        flush: function (link) {
            var channel = link.channel;
            while (link.queue.length && channel.readyState === 'open' && channel.bufferedAmount < this.maxBuffered) {
                try { channel.send(link.queue.shift()); } catch (error) { return; }
            }
        },

        closeLink: function (link) {
            if (!link) return;
            this.flush(link);
            setTimeout(function () { try { link.pc.close(); } catch (error) { } }, 250);
        },

        linkClosed: function (link) {
            var c = this.connection;
            if (!c || !link) return;
            var self = this;
            c.links.forEach(function (candidate, token) {
                if (candidate !== link) return;
                c.links.delete(token);
                var peer = c.peers.get(token);
                if (!peer) return;
                c.peers.delete(token);
                if (!c.server) self.shutdown();
                self.enqueue(2, peer.id);
            });
        },

        gathered: function (pc) {
            // Signaling sends one complete description, so wait for ICE candidates (briefly).
            return new Promise(function (resolve) {
                var finished = false;
                function done() {
                    if (finished) return;
                    finished = true;
                    resolve(pc.localDescription ? pc.localDescription.sdp : null);
                }
                if (pc.iceGatheringState === 'complete') { done(); return; }
                pc.addEventListener('icegatheringstatechange', function () {
                    if (pc.iceGatheringState === 'complete') done();
                });
                setTimeout(done, 3000);
            });
        },

        offer: function (room) {
            if (!/^[a-f0-9]{32}$/.test(room) || typeof RTCPeerConnection === 'undefined')
                return Promise.resolve(null);
            this.discardPending(room);
            var pc = new RTCPeerConnection(this.rtcConfig);
            var link = this.makeLink(pc, pc.createDataChannel('cgs', { ordered: true }));
            this.pendingLinks[room] = { pc: pc, link: link, created: Date.now() };
            var self = this;
            return pc.createOffer()
                .then(function (description) { return pc.setLocalDescription(description); })
                .then(function () { return self.gathered(pc); })
                .catch(function () { self.discardPending(room); return null; });
        },

        accept: function (room, answer) {
            var pending = this.pendingLinks[room];
            if (!pending || typeof answer !== 'string' || !answer) return Promise.resolve(false);
            var self = this;
            return pending.pc.setRemoteDescription({ type: 'answer', sdp: answer })
                .then(function () { return true; }, function () { self.discardPending(room); return false; });
        },

        answer: function (offer) {
            var c = this.connection;
            if (!c || !c.server || typeof offer !== 'string' || !offer || typeof RTCPeerConnection === 'undefined')
                return Promise.resolve(null);
            var self = this;
            var pc = new RTCPeerConnection(this.rtcConfig);
            c.rtc.push(pc);
            pc.ondatachannel = function (event) {
                if (self.connection !== c) { pc.close(); return; }
                self.makeLink(pc, event.channel);
            };
            return pc.setRemoteDescription({ type: 'offer', sdp: offer })
                .then(function () { return pc.createAnswer(); })
                .then(function (description) { return pc.setLocalDescription(description); })
                .then(function () { return self.gathered(pc); })
                .catch(function () { try { pc.close(); } catch (error) { } return null; });
        },

        discardPending: function (room) {
            var pending = this.pendingLinks[room];
            if (!pending) return;
            delete this.pendingLinks[room];
            try { pending.pc.close(); } catch (error) { }
        },

        networkPrefix: function (address) {
            // IPv4 networks share one public address. IPv6 devices each have their own, so use the /64 prefix.
            if (address.indexOf(':') < 0) return address;
            var halves = address.toLowerCase().split('::');
            var head = halves[0] ? halves[0].split(':') : [];
            var tail = halves.length > 1 && halves[1] ? halves[1].split(':') : [];
            var groups = head.concat(new Array(Math.max(0, 8 - head.length - tail.length)).fill('0'), tail);
            return groups.slice(0, 4).map(function (group) { return parseInt(group, 16).toString(16); }).join(':');
        },

        networkKey: function () {
            if (this.networkKeyPromise) return this.networkKeyPromise;
            var self = this;
            var promise = new Promise(function (resolve) {
                if (typeof RTCPeerConnection === 'undefined' || typeof crypto === 'undefined' || !crypto.subtle) {
                    resolve(null);
                    return;
                }
                var pc = new RTCPeerConnection(self.rtcConfig);
                var addresses = [];
                var finished = false;
                function finish() {
                    if (finished) return;
                    finished = true;
                    try { pc.close(); } catch (error) { }
                    var ipv4 = addresses.filter(function (a) { return a.indexOf(':') < 0; })[0];
                    var address = ipv4 || addresses[0];
                    if (!address) { resolve(null); return; }
                    // Lobby data is public, so publish a salted hash instead of the address itself.
                    var text = 'cgs-lan-network-v1:' + self.networkPrefix(address);
                    crypto.subtle.digest('SHA-256', new TextEncoder().encode(text)).then(function (hash) {
                        resolve(Array.from(new Uint8Array(hash)).slice(0, 16)
                            .map(function (b) { return ('0' + b.toString(16)).slice(-2); }).join(''));
                    }, function () { resolve(null); });
                }
                pc.onicecandidate = function (event) {
                    if (!event.candidate) { finish(); return; }
                    var parts = event.candidate.candidate.split(' ');
                    if (parts[7] === 'srflx' && parts[4]) addresses.push(parts[4]);
                };
                pc.createDataChannel('probe');
                pc.createOffer()
                    .then(function (description) { return pc.setLocalDescription(description); })
                    .catch(finish);
                setTimeout(finish, 4000);
            });
            this.networkKeyPromise = promise;
            // Retry later if this attempt found no public address (offline, or STUN blocked).
            promise.then(function (key) { if (!key && self.networkKeyPromise === promise) self.networkKeyPromise = null; });
            return promise;
        },

        complete: function (value, callback) {
            var pointer = this.stringResult(value);
            try { callback(pointer); } finally { if (pointer) _free(pointer); }
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
    CgsBrowserLanShutdown: function () { CgsBrowserLan.shutdown(); },

    // Asynchronous WebRTC signaling helpers report a string (or null) to callback(requestId, value).
    CgsBrowserLanNetworkKey__deps: ['$CgsBrowserLan'],
    CgsBrowserLanNetworkKey: function (requestId, callback) {
        CgsBrowserLan.networkKey().then(function (key) {
            CgsBrowserLan.complete(key, function (pointer) { {{{ makeDynCall('vii', 'callback') }}}(requestId, pointer); });
        });
    },
    CgsBrowserLanOffer__deps: ['$CgsBrowserLan'],
    CgsBrowserLanOffer: function (requestId, room, callback) {
        CgsBrowserLan.offer(UTF8ToString(room)).then(function (offer) {
            CgsBrowserLan.complete(offer, function (pointer) { {{{ makeDynCall('vii', 'callback') }}}(requestId, pointer); });
        });
    },
    CgsBrowserLanAccept__deps: ['$CgsBrowserLan'],
    CgsBrowserLanAccept: function (requestId, room, answer, callback) {
        CgsBrowserLan.accept(UTF8ToString(room), UTF8ToString(answer)).then(function (accepted) {
            CgsBrowserLan.complete(accepted ? 'ok' : null, function (pointer) { {{{ makeDynCall('vii', 'callback') }}}(requestId, pointer); });
        });
    },
    CgsBrowserLanAnswer__deps: ['$CgsBrowserLan'],
    CgsBrowserLanAnswer: function (requestId, offer, callback) {
        CgsBrowserLan.answer(UTF8ToString(offer)).then(function (answer) {
            CgsBrowserLan.complete(answer, function (pointer) { {{{ makeDynCall('vii', 'callback') }}}(requestId, pointer); });
        });
    },
    CgsBrowserLanDiscardOffer__deps: ['$CgsBrowserLan'],
    CgsBrowserLanDiscardOffer: function (room) { CgsBrowserLan.discardPending(UTF8ToString(room)); }
});
