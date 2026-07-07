# Design: fix-multiplayer-deck-load-order

## Context

CGS multiplayer uses Unity Netcode for GameObjects (server-authoritative, Relay/Lobby). Deck placement today is driven by two independent orderings:

- **Position**: `PlayController.NewPlayablePosition` (Assets/Scripts/Cgs/Play/PlayController.cs:107) selects `GamePlayDeckPositions[AllCardStacks.Count]` — one slot per already-spawned stack, i.e. **deck-load order**. Extra card groups in `LoadDeck` (lines 399–404 online, 424–429 solo) consume subsequent slots the same way. The client computes positions and passes them through `CgsNetPlayer.RequestNewDeck`/`RequestNewCardStack` → `CreateCardStackServerRpc`, which the server uses verbatim.
- **Rotation**: `CgsNetPlayer.ApplyPlayerRotationServerRpc` (Assets/Scripts/Cgs/Play/Multiplayer/CgsNetPlayer.cs:205) derives `DefaultZRotation` from `ConnectedClients.Count` — i.e. **join order**.

Game configs author `gamePlayDeckPositions` as **per-player blocks**, one slot per stack a full deck load produces. Yugioh (dragogodev/cgs): 4 positions = player 1's Main Deck + Extra Deck (y=−12), then player 2's (y=+12). Grand Archive (docs/games/grand_archive/cgs.json): 4 positions = 2 players × (Main Deck + Material Deck). This exposes two bugs:

1. If the second player loads first, they consume the first player's slots (and rotation, keyed to join order, disagrees with position, keyed to load order).
2. A deck that produces fewer stacks than the block intends (e.g., a Yugioh deck with no Extra Deck cards spawns one stack, not two) shifts every subsequent slot, so the next player's deck lands in the wrong place even when loading in the "correct" order.

A schema change to FinolDigital.Cgs.Json was considered and **explicitly rejected** for this change (decision below); the fix is entirely in this repo.

## Goals / Non-Goals

**Goals:**
- Multiplayer deck/extra-group placement independent of deck-load order.
- Placement independent of how many stacks each player's deck actually produces (reserved slots).
- Position and rotation for a player derive from a single, stable per-player seat index.
- Existing game configs (Yugioh, Grand Archive) work correctly with zero config edits.
- Solo multi-deck loading gains the same gap-tolerant block layout.

**Non-Goals:**
- Changes to FinolDigital.Cgs.Json, the cgs.json schema, or any game configs (an explicit `gamePlayDeckPositionsPerPlayer` override field may be a future follow-up).
- Seat picking UI or seat reassignment/compaction on disconnect.
- Server-side validation of client-supplied spawn positions (trust model unchanged).
- Games with no `gamePlayDeckPositions` (free-slot placement is unchanged).

## Decisions

### 1. Server-assigned seat index on `CgsNetPlayer`

Add `NetworkVariable<int> SeatIndex` to `CgsNetPlayer`, assigned server-side in `OnNetworkSpawn` from a monotonic server-side counter (host = seat 0, next join = seat 1, ...). Assigning at spawn means it replicates with initial spawn data, well before any deck load.

*Alternative considered*: keying off `OwnerClientId` — rejected; client IDs are not guaranteed dense/sequential (especially across Relay reconnects), so they cannot index position blocks.

### 2. Slots-per-player inferred from the game's extras definitions (no schema change)

`slotsPerPlayer = 1 + (number of distinct group names in CardGameManager.Current.Extras)`. Yugioh's seven extras entries all share the group "Extra Deck" → 2; Grand Archive's map to "Material Deck" → 2. Both match their authored 4-position configs (2 players × 2 slots).

*Alternatives considered*:
- Explicit `gamePlayDeckPositionsPerPlayer` schema field (with or without inference fallback) — rejected per proposal review: requires a FinolDigital.Cgs.Json NuGet release, a DLL update in Assets/Plugins, docs/schema changes, and edits to existing (including third-party) game configs before anyone benefits. Inference fixes shipped games immediately. The field remains a compatible future addition if a game surfaces where inference guesses wrong.

### 3. Per-seat block mapping with a divisibility guard

Seat `i` owns slots `[i*S, (i+1)*S)` of `GamePlayDeckPositions`. The per-seat mapping applies only when `GamePlayDeckPositions.Count > 0 && GamePlayDeckPositions.Count % S == 0`; otherwise (config not authored in block multiples) fall back to the current load-order behavior rather than misinterpret the author's intent. If a seat's block start is beyond the configured positions (more players than blocks), use the existing non-overlapping free-slot scan.

### 4. Fixed offsets within a block; missing groups reserve their slot

Within a seat's block: the deck stack goes at offset 0; extra group `g` goes at offset `1 + indexOf(g)` in the game's canonical extra-group order (distinct group names by first appearance in the `extras` list). Offsets are computed from the game definition, not from the deck's contents, so a deck lacking cards for some extra group simply leaves that slot empty. This is what fixes bug 2 — placement never depends on how many stacks the previous player spawned.

### 5. Rotation derived from the same seat index

Refactor `ApplyPlayerRotationServerRpc`/`ApplyPlayerRotationOwnerClientRpc` to compute `DefaultZRotation` from `SeatIndex` (seat 0 → 0°, seat 1 → 180°, seat 2 → 90°, seat 3 → 270°, preserving the current join-order mapping where seat = playerCount − 1). Position and rotation can then never disagree.

### 6. Solo play uses block-per-deck-load

In the solo branch of `LoadDeck`, the block index is the number of decks loaded so far this session (load order), with the same fixed offsets within the block — not the raw stack count. Loading a single deck into an empty play area behaves exactly as today (block 0, offsets from slot 0); the change only corrects the gap-shifting when a deck is missing extra-group cards.

### 7. Computation stays client-side

The requesting client computes positions from its replicated `SeatIndex` and the (identically replicated) game config, then sends them through the existing `RequestNewDeck`/`RequestNewCardStack` RPCs. No RPC signature changes. Server-side derivation/validation is a possible follow-up but unnecessary to fix the ordering bug.

## Risks / Trade-offs

- [Inference guesses wrong for some game (extras groups ≠ intended slots per player)] → Divisibility guard falls back to legacy behavior when the config isn't authored in block multiples; a future `gamePlayDeckPositionsPerPlayer` schema field is the escape hatch if a real case appears.
- [A game defines extras but authored positions assuming pure load-order filling] → No known example (Yugioh and Grand Archive both follow the block pattern; other bundled games define no positions). Guard above limits blast radius.
- [Player reloads a deck mid-session] → New stacks target the same seat block (on top of any prior stacks there), instead of spilling into the next free slots. This is arguably more correct (a reload replaces your seat's deck) but is a behavior change to note in testing.
- [Seat not reclaimed on disconnect; a rejoining player gets a new seat] → Matches current rotation behavior; documented non-goal. Mitigation later: server maps returning players to their previous seat.
- [`SeatIndex` not yet replicated when the owner loads a deck] → Assigned in `OnNetworkSpawn` server-side, replicated with spawn data; deck loading reads it lazily at request time.

## Migration Plan

No data or config migration; no package updates. Behavior ships in the CGS client; both players in a session should run compatible versions (already required by NGO). Rollback = revert the commit.

## Open Questions

- None blocking. If a published game is found where `1 + distinct extras groups` mismatches its authored position blocks, revisit the explicit schema field.
