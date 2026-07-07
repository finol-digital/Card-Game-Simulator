# Proposal: fix-multiplayer-deck-load-order

## Why

In multiplayer play sessions, deck placement depends on the order players load their decks: `PlayController` fills `GamePlayDeckPositions` by the count of card stacks already in the play area, while seat rotation is assigned by join order. Two failure modes result:

1. **Out-of-order loading**: If the second player loads their deck before the first player, their stacks take the first player's configured positions (e.g., Yugioh's `gamePlayDeckPositions[0..1]`), and decks land in the wrong seats with mismatched rotations.
2. **Variable stack counts**: `GamePlayDeckPositions` entries are consumed one per spawned stack, so a deck that produces fewer stacks than the game expects shifts everyone after it. In Yugioh (4 positions: player 1 main/extra, player 2 main/extra), a first player whose deck has no Extra Deck cards consumes only one slot, so the second player's main deck lands in player 1's extra-deck position.

## What Changes

- Assign each player a stable seat index at connection time (join order), managed server-side and replicated to all clients.
- Interpret `GamePlayDeckPositions` as consecutive per-seat blocks: seat `i` owns slots `[i*S, (i+1)*S)`, where the block size `S` is inferred from the game config as `1 + (number of distinct extra group names in extras)`. No change to FinolDigital.Cgs.Json or the cgs.json schema.
- Place stacks at fixed offsets within the owner's block: the deck at offset 0, each extra group at a fixed offset determined by its position in the game's extras definitions. Slots for extra groups absent from a player's deck stay empty (reserved), so later loads are never shifted.
- Derive seat rotation from the same seat index used for placement, so a player's deck position and orientation always agree.
- In solo play, apply the same block/offset layout per deck load (blocks assigned in load order), so a deck missing extra-group cards no longer shifts the next deck's positions.
- Games that define no `GamePlayDeckPositions` (e.g., Dominoes, Mahjong, Standard Playing Cards) keep the existing free-slot placement, unchanged.

## Capabilities

### New Capabilities

- `seat-based-deck-placement`: Stable per-player seat assignment in networked play, per-seat slot blocks inferred from the game's extras definitions, and fixed-offset placement of deck and extra-group stacks within a seat's block.

### Modified Capabilities

<!-- None: no existing spec covers deck loading or positioning. -->

## Impact

- **Affected code**:
  - `Assets/Scripts/Cgs/Play/PlayController.cs` — `NewPlayablePosition`, `LoadDeck` (both online and solo branches), slot-block/offset lookup.
  - `Assets/Scripts/Cgs/Play/Multiplayer/CgsNetPlayer.cs` — seat index `NetworkVariable`, `RequestNewDeck`, rotation RPCs (`ApplyPlayerRotationServerRpc`/`ApplyPlayerRotationOwnerClientRpc`).
  - `Assets/Scripts/Cgs/Play/Multiplayer/CgsNetManager.cs` — server-side seat counter (if not housed in `CgsNetPlayer`).
- **Not affected**: FinolDigital.Cgs.Json (NuGet package/DLL), `docs/schema/cgs.json`, and existing game configs — Yugioh and Grand Archive get correct behavior with no config edits because their configured positions already follow the per-seat block pattern (`4 positions = 2 players × (main + extra/material deck)`).
- **Behavior**: Multiplayer deck placement becomes independent of deck-load order and of whether decks contain extra-group cards; solo multi-deck loading gets the same gap-tolerant layout; games without configured deck positions are unchanged.
- **Networking**: Adds a per-player seat index (`NetworkVariable`) assigned by the server at spawn; deck spawn requests otherwise unchanged.
