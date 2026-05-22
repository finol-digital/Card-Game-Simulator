## ADDED Requirements

### Requirement: Per-card back assignment SHALL persist across reload
When a card has an individual back-face selection saved from editor or import workflows, the system SHALL restore that same selection after loading cards from `AllCards.json`.

#### Scenario: Reload restores selected back from canonical backs field
- **WHEN** a card entry in `AllCards.json` contains a non-empty back id in `backs`
- **THEN** the loaded card SHALL expose that back id as its effective per-card back selection

#### Scenario: Reload restores selected back from legacy backFaceId field
- **WHEN** a card entry in `AllCards.json` does not provide a usable `backs` value but provides `backFaceId`
- **THEN** the loaded card SHALL use `backFaceId` as a compatibility fallback for effective per-card back selection

### Requirement: Card serialization SHALL write schema-aligned back data
When cards are serialized to `AllCards.json`, per-card back selections SHALL be represented using the schema-aligned `backs` field.

#### Scenario: Save writes selected custom back
- **WHEN** a card has a selected custom back-face id
- **THEN** serialized card JSON SHALL include that back selection in `backs`

#### Scenario: Save writes no custom back selection
- **WHEN** a card has no selected custom back-face id
- **THEN** serialized card JSON SHALL not imply a custom back and runtime behavior SHALL resolve to default global card back

### Requirement: Back-field precedence SHALL be deterministic
If multiple back representations are present in the same card payload, the loader SHALL apply deterministic precedence to avoid ambiguity.

#### Scenario: Both fields present
- **WHEN** a card payload contains both `backs` and `backFaceId`
- **THEN** loader behavior SHALL prioritize `backs` and use `backFaceId` only when `backs` is absent or unusable
