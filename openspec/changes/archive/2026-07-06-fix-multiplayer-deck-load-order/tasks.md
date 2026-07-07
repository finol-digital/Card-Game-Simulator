# Tasks: fix-multiplayer-deck-load-order

## 1. Seat index assignment

- [x] 1.1 Add `NetworkVariable<int> SeatIndex` to `CgsNetPlayer` (Assets/Scripts/Cgs/Play/Multiplayer/CgsNetPlayer.cs), assigned server-side in `OnNetworkSpawn` from a monotonic server-side seat counter (host = seat 0, next join = seat 1, ...)
- [x] 1.2 Add the server-side seat counter (in `CgsNetManager` or a static server-only field on `CgsNetPlayer`), reset when a session starts/stops

## 2. Slot-block layout logic

- [x] 2.1 Add a helper for the game's canonical extra-group order: distinct group names from `CardGameManager.Current.Extras` in order of first appearance; slots per player S = 1 + that count
- [x] 2.2 Add a slot lookup in `PlayController` (e.g., `TryGetSeatSlotPosition(int blockIndex, int slotOffset, out Vector2 position)`) that maps to `GamePlayDeckPositions[blockIndex*S + slotOffset]`, applying the per-seat mapping only when `GamePlayDeckPositions.Count > 0 && Count % S == 0`, and reporting failure (out of range or guard not met) so callers can fall back
- [x] 2.3 Preserve the existing fallbacks: legacy load-order filling when the divisibility guard fails, and the non-overlapping free-slot scan when a block is beyond the configured positions or no positions are configured

## 3. Multiplayer placement from seat index

- [x] 3.1 In the online branch of `PlayController.LoadDeck`, place the deck at `blockIndex = LocalPlayer.SeatIndex`, offset 0, replacing the `NewPlayablePosition`/`AllCardStacks.Count` logic
- [x] 3.2 Place each extra group at its fixed offset (1 + index of its group name in the canonical extra-group order), replacing the `startingDeckCount + i` indexing — slots for groups missing from the deck stay empty
- [x] 3.3 Update `CgsNetPlayer.RequestNewDeck` so the deck position comes from the seat block (passed in or computed from `SeatIndex`) instead of `PlayController.Instance.NewPlayablePosition`

## 4. Rotation from seat index

- [x] 4.1 Refactor `ApplyPlayerRotationServerRpc`/`ApplyPlayerRotationOwnerClientRpc` to compute `DefaultZRotation` from `SeatIndex` (seat 0 → 0°, seat 1 → 180°, seat 2 → 90°, seat 3 → 270°) instead of `ConnectedClients.Count`
- [x] 4.2 Verify the host path (`ApplyPlayerTranslationServerRpc` in `OnNetworkSpawn`) still yields 0° for seat 0

## 5. Solo placement

- [x] 5.1 In the solo branch of `LoadDeck`, use `blockIndex = number of decks loaded this session` (track deck loads, not raw stack count) with the same fixed offsets, keeping current behavior for the first deck in an empty play area
- [x] 5.2 Keep the free-slot fallback for solo games without block-mapped positions

## 6. Verification

- [x] 6.1 Solo regression: load one deck with an empty play area (Yugioh config) → Main Deck at positions[0], Extra Deck at positions[1]
- [x] 6.2 Solo gap case: load a deck with no Extra Deck cards, then a second deck with Extra Deck cards → second deck's stacks at positions[2] and [3]
- [x] 6.3 Multiplayer in-order regression: host loads first, client second → seat 0 block then seat 1 block, rotations 0° and 180°
- [x] 6.4 Multiplayer out-of-order: client (seat 1) loads before host → client's Main/Extra at positions[2]/[3] with 180° rotation; host's later load at positions[0]/[1] with 0°
- [x] 6.5 Multiplayer gap case: host's deck has no Extra Deck cards, client loads after → positions[1] left empty, client at positions[2]/[3]
- [x] 6.6 Guard cases: game with no `gamePlayDeckPositions` (Standard Playing Cards) unchanged; third player beyond configured blocks gets free-slot placement
- [x] 6.7 Run existing PlayMode/EditMode tests to confirm no regressions (full suite run by user in Unity Editor — all passed; scenarios 6.1–6.6 verified by code trace)
