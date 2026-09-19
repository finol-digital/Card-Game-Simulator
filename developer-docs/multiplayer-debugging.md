# Investigating Android 1.164 client interaction failures

## Candidates introduced after 1.163

These are investigation targets, not confirmed causes. First establish the user's last working app version.

1. **`c2f470e1` (August 4, 2026), `cc83d226`, `0154ef33`, `976537c1`: pointer cleanup.**
   `CgsNetPlayable` now prunes pointers after 0.5 seconds with no pressed mouse, pen, or
   `Touchscreen.current` touch. Cleanup explicitly calls `OnEndDrag` and clears drag state.
   `RotateZoomableScrollRect` also clears pointers when Enhanced Touch reports no active touches.
   If Android's pointer events and device state disagree, a legitimate drag could be ended early.
   A joining player additionally waits for ownership before moving the object, so trace that
   timing too. This does not yet explain failed non-drag actions or why the host is unaffected.
   The cleanup log was made Editor-only, leaving Android releases without that evidence.
2. **`06c18d3a` and `7ea5b842` (August 6): custom-game setup.**
   Setup queries changed from `playerCount=N` to `playerSeat=N`, and deck callbacks now use
   the player's seat instead of total connected players. Older custom definitions with
   `playerCount=` no longer match. This can explain missing starting cards/layout differences,
   but does not directly deny interaction with already-spawned objects.

The Netcode and Input System package versions did not change from 1.163 to 1.164.
Cards, stacks, dice, and counters permit non-owner requests in the 1.164 source.
Do not remove authorization checks as a speculative fix.

## Capture a session

Build the diagnostic revision for both participants. This is new instrumentation; it is
not present in the published 1.164 APK. No new RPCs or network messages are added.

1. On **both devices**, enable **Settings > Developer Mode** before hosting/joining.
   The **NET TRACE** toolbar appears only in the play scene, including the lobby.
   Recording is local; there is no automatic upload. Menus and card/deck editors do not record traces.
2. Join the same game. Press **Mark / snapshot** after loading on both devices.
3. Use one host-created card, stack, die, and counter. Have the joining player try a slow
   drag, quick drag, flip, roll, and counter change. Note the order and which action failed.
   Keep gestures outside the diagnostic toolbar, which consumes input within its bounds.
4. Press **Mark / snapshot** immediately after failure and **Export trace** on both devices.
   Android/iOS opens the share sheet. Desktop copies the trace to the clipboard. A copy is
   also written to `Application.temporaryCachePath/cgs-network-trace.txt`, replacing the
   previous export. Retain each export before the next attempt.
5. Swap host/client roles, then repeat with a built-in game. If relevant, compare a fresh
   lobby with reconnecting and direct LAN with the normal online lobby.

The buffer retains the latest 2,000 events, each capped at 2,000 characters. High-frequency
drag/position/ownership-request events are sampled once per second per stage/object (and
sender for incoming requests). They are **not** a packet count or exact latency measurement.
Up to 512 sampling keys can be active at once. When full, new sampled keys are skipped until
an existing one-second window expires; active windows are never cleared to make room.
Repeated warnings are sampled too. Enabling Developer Mode or **Clear trace** starts a new capture.
The recorder belongs to the play scene; leaving it removes the toolbar and discards the capture.
Export before returning to the main menu, clearing, disabling/re-enabling Developer Mode, or quitting.

Exports include UTC timestamps, frame numbers, app/build/Unity versions, device/OS, connection
and local-player state, network object/owner IDs, input hits, and warnings/exceptions.
Pointer events include raw pressed-touch count and Enhanced Touch count (`-1` when disabled).
UI hit names and existing exception text may include game-specific information; review the
file before sharing it outside the debugging team. No lobby join codes are deliberately recorded.

Snapshots include game ID, loading state, card count, and a SHA-256 hash of local `cgs.json`.
Different hashes prove the configuration bytes differ, even if the game IDs match. Matching
hashes do not prove all card/deck/image files match. Assembly versions in the header may be
less specific than package versions; compare builds and package manifests as well.

## Read the traces

Correlate peers by network object ID and sender/client ID; Unity instance IDs and frame
numbers are local to each device. Account for device clock differences when comparing UTC.

| Last observed event | Next investigation |
| --- | --- |
| No `input-press` | Input System device state, focus, touch hardware, recording enabled |
| `input-press`, wrong `top` hit or no playable | UI overlay, raycast blocker, input module |
| `pointer-down` / `drag-begin`, followed by `pointer-pruned` during contact | August 4 cleanup and raw versus Enhanced Touch state |
| `ownership-request` on client, no `ownership-received` on host | Connection or RPC delivery |
| `ownership-denied-*` | Connected client membership, object type and authorization |
| `ownership-granted` on host, no `ownership-gained` on client | Ownership synchronization |
| Ownership gained, no `position-send` | Drag ended while waiting, missing follow-up drag events, subclass drag handling |
| `position-received` / `position-applied`, no `position-observed` on peer | NetworkVariable synchronization (unchanged values need not emit a change event) |
| `*-send` for flip/roll/counter but no matching `*-received` | RPC delivery; if received, inspect `authorized` and subsequent exceptions |
| No `game-ready`, or mismatched game snapshots | Join initialization or custom content |

For the cleanup hypothesis, compare identical two-device scenarios on 1.163 and the
instrumented revision. If cleanup fires while a finger is held, test a diagnostic build
with only the new cleanup disabled; do not roll back unrelated input fixes wholesale.
Any resulting gameplay fix needs real host/client validation, including delayed ownership,
host/client role swapping, and Android touch input. Existing `OwnershipTests` use offline
objects/reflection and do not establish that a remote client's gestures work.

## Validation

Use `unity command recompile` and poll `unity command recompile_status`, then run
`unity command run_tests --mode playmode --filter OwnershipTests --async_tests` and poll
`unity command test_status`. Run `NetworkDiagnosticsTests` the same way to check opt-in
recording, the toolbar toggle, sampling, and bounded storage. On devices, also check that Developer Mode off produces no
trace toolbar, that export works, and that only the toolbar's own rectangle intercepts input.
