## Why

When users set a per-card back in the Card Editor, the value is saved, but subsequent loads from `AllCards.json` do not consistently restore that selection. This breaks editor expectations and causes card backs to silently revert at runtime.

## What Changes

- Add load-time compatibility so cards with a serialized `backFaceId` still restore per-card back behavior.
- Align card serialization output with the documented cards schema by writing per-card back data using `backs`.
- Keep compatibility with existing user-created card data while transitioning to schema-aligned output.
- Ensure card and stack rendering continue to resolve back-face sprite selection from restored per-card metadata.

## Capabilities

### New Capabilities
- `card-back-persistence`: Persist and reload per-card card-back assignments across editor save and app restart using schema-aligned card data.

### Modified Capabilities
- None.

## Impact

- Affected code in card serialization/deserialization flow under `Assets/Scripts/FinolDigital.Cgs.Json.Unity/`.
- Affected editor-originated card save behavior in `Assets/Scripts/Cgs/Cards/`.
- No external API changes, but `AllCards.json` output shape for editor-saved cards will be normalized toward schema usage.
