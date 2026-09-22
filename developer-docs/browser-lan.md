# Browser LAN multiplayer

Web builds use a Netcode `BrowserLanTransport` backed by `BroadcastChannel` for
the LAN option. Browsers cannot listen for incoming UDP/WebSocket connections,
so Unity Transport plus UDP discovery cannot host a LAN game inside a tab.

This supports tabs/windows in the **same browser profile and same website
origin** (scheme, hostname, port, and storage partition). It requires no local
server, sign-in, or Internet connection after the game has loaded. It does not
connect different computers, browsers, profiles, or normal/private windows.
Use Internet multiplayer for those connections. Native builds retain UDP LAN
networking; Internet multiplayer retains its existing transport.

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

## Automated checks

- `node --test scripts/browser-lan.test.cjs` executes the actual JavaScript plugin
  with independent contexts and real BroadcastChannels. It checks discovery,
  binary/large packet ordering, client routing, room isolation, disconnects,
  reconnects, timeouts, and unsupported browsers. In a process-restricted shell,
  add `--test-isolation=none`.
- Run the Unity PlayMode `BrowserLanTests` for the room-ID/transport integration
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
