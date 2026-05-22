## Why

Schema file parity checks currently run as PlayMode tests even though they only validate filesystem content and do not require scene/runtime behavior. This makes the test suite slower and couples static validation to runtime-oriented execution paths.

## What Changes

- Move `SchemaTests` from the PlayMode test assembly into a new EditMode test assembly.
- Add an EditMode test folder and assembly definition under `Assets/Tests/EditMode/`.
- Update CI to run EditMode tests so schema parity checks continue to gate pull requests.
- Keep the schema validation behavior unchanged (same directories and comparison expectations).

## Capabilities

### New Capabilities
- `schema-validation-test-mode`: Defines that schema parity validation tests execute in EditMode and are included in CI test runs.

### Modified Capabilities
- None.

## Impact

- Affected code: `Assets/Tests/PlayMode/SchemaTests.cs` moved to `Assets/Tests/EditMode/SchemaTests.cs`, plus new `Assets/Tests/EditMode/EditMode.asmdef`.
- Affected CI: `.github/workflows/main.yml` test job must include EditMode execution.
- Dependencies: no new external dependencies; Unity Test Runner configuration is updated.
