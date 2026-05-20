## 1. Loader Compatibility

- [x] 1.1 Update card deserialization to read `backs` as primary per-card back source.
- [x] 1.2 Add fallback parsing from `backFaceId` when `backs` is absent or unusable.
- [x] 1.3 Enforce deterministic precedence (`backs` before `backFaceId`) when both are present.

## 2. Canonical Writer Output

- [x] 2.1 Update `AllCards.json` serialization path to emit per-card back selection in schema-aligned `backs` form.
- [x] 2.2 Ensure cards without custom back selection serialize without implying a custom back.
- [x] 2.3 Validate that editor/import-created cards round-trip through save/load with preserved effective back selection.

## 3. Verification and Regression Coverage

- [x] 3.1 Add or update tests for save->reload restoration using `backs` input.
- [x] 3.2 Add or update tests for legacy `backFaceId` fallback behavior.
- [x] 3.3 Add or update tests for mixed-field payload precedence and default-back behavior.
