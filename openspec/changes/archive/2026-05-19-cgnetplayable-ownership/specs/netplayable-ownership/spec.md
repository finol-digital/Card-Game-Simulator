## ADDED Requirements

### Requirement: Creator owns newly spawned playables
When a `CgsNetPlayable` is created by a `CgsNetPlayer`, the system MUST assign ownership of that playable to the creating player at spawn time.

#### Scenario: Ownership assigned on creation
- **WHEN** a `CgsNetPlayer` spawns a new `CgsNetPlayable`
- **THEN** the new playable is owned by that player

### Requirement: Non-owners cannot interact with private playables
The system MUST prevent non-owners from moving, rotating, or performing other actions on `CgsNetPlayable` instances they do not own, unless the playable is a shared `CardStack`.

#### Scenario: Non-owner action denied
- **WHEN** a non-owner attempts to move, rotate, or act on a non-shared `CgsNetPlayable`
- **THEN** the action is rejected

### Requirement: Shared card stacks allow ownership requests
If a `CardStack` is created from a deck marked as shared, the stack MUST allow other players to request ownership and perform actions once ownership is granted.

#### Scenario: Ownership request allowed for shared stack
- **WHEN** a non-owner attempts to interact with a shared `CardStack`
- **THEN** the system allows an ownership request and permits actions after ownership is granted

### Requirement: Sharing is scoped to card stacks
Shared status MUST be associated with the `CardStack` itself, not globally with the player, so other stacks remain private.

#### Scenario: Shared status is per-stack
- **WHEN** a player has both shared and non-shared stacks
- **THEN** only the stacks marked shared allow ownership requests
