# Investigating Internet room-code failures

Connection diagnostics are written to the normal Unity player log with the prefix
`[CgsNet Connection]`. They are also included in the existing NET TRACE export when
Settings > Developer Mode is enabled before hosting or joining. No automatic upload
is performed. Use a build containing this instrumentation on both devices.

## Collect a reproduction

1. Record the CGS versions on the host and client, including the host platform/store.
2. Enable Developer Mode on both devices before hosting/joining. Host an Internet
   game and use Share to copy its Room Id.
3. Paste the code on the client and join. Keep the reference shown in any error.
4. Export NET TRACE on both devices before leaving the play scene or clearing the
   trace. See [multiplayer debugging](multiplayer-debugging.md#capture-a-session)
   for export instructions. If Developer Mode was off, collect the player logs.
5. Also try selecting the host from the Internet game list. This uses the lobby ID
   rather than the code. Record whether it succeeds; do not assume it will.

These logs include live room/relay codes and lobby identifiers. Share them privately
with support, preferably after closing the room. The added events do not record
passwords, authentication tokens, or relay allocation keys.

## Compare the events

| Event | Evidence |
| --- | --- |
| `lobby-created` | Lobby ID and the distinct lobby and relay codes returned during hosting |
| `room-share` | The exact value passed to sharing, alongside the current lobby/relay codes and transport; this does not prove clipboard or OS-share completion |
| `join-code-start` / `join-code-request` | Client input before sign-in and immediately before calling the lobby API |
| `join-code-failed` | Same attempt reference, SDK exception type, service reason/error code, messages and stack traces |
| `join-code-succeeded` | Lobby ID and lobby code returned by the service |
| `join-id-start` / `join-id-request` / `join-id-failed` / `join-id-succeeded` | The same reference-aware stages when selecting the host from the Internet list, with the lobby ID labeled separately |
| `relay-join-request` / `relay-join-failed` | The separate relay stage after a successful lobby join |
| `relay-client-start` | Whether Netcode accepted the start request; this does not prove the client connected |
| `relay-client-connected` / `relay-client-disconnected` | Connection confirmation, or a disconnect before connection with the same attempt reference and Netcode disconnect reason; intentionally leaving cancels the pending error notification |

Each event includes UTC time, app/build/Unity version, platform, cloud project ID,
and lobby SDK assembly version (which may not equal the package version). Match
host and client by lobby code/ID; the attempt reference correlates client events
and the on-screen error within that client only.

Code objects contain the original length, a value sample, and indexed UTF-16 units.
For example `O` is `U+004F`, `0` is `U+0030`, `I` is `U+0049`, and `1` is `U+0031`.
Whitespace, zero-width characters, and surrogate pairs remain visible in these
units. Values are JSON-escaped and capped at 64 UTF-16 units with a truncation flag;
the submitted value is never changed by diagnostics. Normal player logs contain
the complete event; NET TRACE retains its existing 2,000-character per-event cap.

If the hosted, shared, and submitted lobby codes match but Unity reports
`InvalidJoinCode`, retain both logs for a service investigation. If they differ,
the stages locate the first discrepancy. Do not replace ambiguous characters or
retry a lobby code as a relay code: that hides evidence and changes the join path.
