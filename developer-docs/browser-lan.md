# Browser LAN multiplayer

Web builds use a Netcode `BrowserLanTransport` for the LAN option. Browsers cannot listen for incoming
UDP/WebSocket connections, so Unity Transport plus UDP discovery cannot host a LAN game inside a tab. The
transport speaks one small message protocol (join, welcome, data, ping, leave) over two kinds of link:

- **Same browser:** `BroadcastChannel` connects tabs/windows in the same browser profile and website origin.
  It needs no server, sign-in, or Internet connection after the game has loaded.
- **Other devices on the same network:** WebRTC data channels (`BrowserNetworkLan`). The game data travels
  directly over the local network; Unity Lobby is used only to find rooms and exchange connection details.

A hosting browser offers both. Native builds retain UDP LAN networking and cannot join browser LAN rooms (they
have no WebRTC); use Internet multiplayer between Web and native players.

## How network rooms work

1. Each browser asks public STUN servers (`stun.l.google.com`, `stun.cloudflare.com`) for its network's public
   address and hashes it (SHA-256 with a fixed salt, first 128 bits). IPv6 uses the /64 prefix, because each
   IPv6 device has its own address. Browsers behind the same router therefore share a *network key*.
2. The host creates a public Lobby named `<game> (Web LAN)` with the network key in indexed field `S1`
   (`WebLanNetwork`) and its room ID (`WebLanRoom`). Joining browsers query lobbies by their own key.
3. A joining browser creates a WebRTC offer (about 1 KB, with all ICE candidates) and joins the lobby with it as
   member-only player data. The host polls the lobby every 1.5 seconds, answers each offer in member-only
   lobby data (`Answer<player ID prefix>`), and heartbeats the lobby every 15 seconds.
4. The client applies the answer, leaves the lobby, and starts Netcode over the new data channel. The host
   removes answers of players who left. When hosting stops, the host deletes the lobby.

Large Netcode packets are split into ordered 16 KB chunks, and sending waits while a channel buffers more than
4 MB. Each data channel speaks only for the player that joined through it.

Limits and privacy: discovery needs an Internet connection (Lobby and STUN); play itself does not route through
the Internet. Networks that isolate Wi-Fi clients or block multicast DNS can prevent the direct connection; the
player then sees `network.browser.lan.failed` and can use Internet multiplayer. Lobby data is public, so only
the salted hash of the public address is published; the descriptions are visible only to lobby members. Lobby
rate limits allow roughly one query per second per player. Older native versions show `(Web LAN)` lobbies in
their Internet list but cannot join them; current versions hide them there. Starting local play in a browser
also hosts a LAN room, like native builds.

## Two-tab reproduction

1. Build the Web player and serve it over HTTP or HTTPS. Open the same URL in
   two tabs of one browser profile. Load the same card game in both tabs.
2. Open Multiplayer, select LAN, and Host in the first tab.
3. Select LAN in the second tab. Within the five-second refresh interval the
   hosted game should appear. Select it and Join. Alternatively, copy the room
   ID from the host's scoreboard and enter it in the second tab.
4. Verify both players appear. Create/move/flip a card and change a counter from
   each tab; verify the other tab sees the resulting state. Switch tabs during play.
5. Disconnect the client and rejoin. Close the client tab and verify the host
   removes that player. Close the host and verify the client disconnects.
6. Host again, and host another independent room in a third tab. Verify discovery
   lists both rooms and each client receives only its own room's game state.
7. Repeat native LAN hosting/joining and Internet hosting/joining as regression checks.

Browser tab suspension can pause game simulation. Keep the host tab open and
avoid putting it to sleep. The transport tolerates up to 60 seconds of silence;
missing hosts time out after 10 seconds. Closing a tab notifies its peers using
`pagehide`; heartbeat expiry handles crashes. Stale discovery entries expire.
LAN passwords have the same behavior as native LAN (the password field is for
Internet rooms).

## Two-device reproduction

1. Connect two devices (for example a computer and a phone or tablet) to the same Wi-Fi network. Open the Web
   player on both and load the same card game.
2. Host LAN on the first device. On the second, open Multiplayer and select LAN; the room appears within the
   five-second refresh. Join, then repeat the gameplay and disconnect checks below in both directions.
3. Repeat with one device on a different network (such as a phone hotspot): the room must not appear.

## Automated checks

- `node --test scripts/browser-lan.test.cjs` executes the actual JavaScript plugin
  with independent contexts and real BroadcastChannels. It checks discovery,
  binary/large packet ordering, client routing, room isolation, disconnects,
  reconnects, timeouts, and unsupported browsers. In a process-restricted shell,
  add `--test-isolation=none`.
  The WebRTC tests use fake peer connections; for real WebRTC, load the plugin in two browser tabs with an
  isolated `BroadcastChannel` and exchange the offer and answer by hand.
- Run the Unity PlayMode `BrowserNetworkLanTests` for Lobby signaling (with in-memory fakes), and
  `BrowserLanTests` for the room-ID/transport integration
  and selecting a deck that has already despawned after drawing its last card.
- Compile a Web player as well as Editor scripts: browser bridge imports are
  enabled only in the Web player and must survive IL2CPP stripping/linking.

Room and client identifiers are generated per connection, never read from
PlayerPrefs/localStorage, which are shared between tabs. The host assigns Netcode
client IDs. Browser packets never bypass existing Netcode RPC ownership checks.
BroadcastChannel is scoped by the browser's origin/storage partition; it is not
an authentication boundary against other scripts running on the same origin.

References: [Unity Web networking](https://docs.unity.cn/Packages/com.unity.transport@2.3/manual/websockets.html),
[Broadcast Channel API](https://developer.mozilla.org/en-US/docs/Web/API/Broadcast_Channel_API).
