## Context

`SchemaTests` currently lives in the PlayMode test assembly and validates parity between files under `Assets/StreamingAssets` and `docs/games`. The test behavior is deterministic and file-based, with no scene loading, coroutine scheduling, GameObject lifecycle interaction, or network runtime behavior.

The repository currently defines only one Unity test assembly under `Assets/Tests/PlayMode/PlayMode.asmdef`. CI runs `game-ci/unity-test-runner` in `playmode` only, which means relocating schema validation to EditMode requires explicit CI updates to preserve coverage.

## Goals / Non-Goals

**Goals:**
- Reclassify schema parity validation as an EditMode concern.
- Introduce an EditMode test assembly dedicated to editor-time tests.
- Ensure CI executes EditMode tests so schema checks continue to gate changes.
- Preserve existing schema validation semantics and test assertions.

**Non-Goals:**
- Changing schema comparison logic or file filtering behavior.
- Refactoring unrelated PlayMode tests.
- Introducing new schema tooling, data formats, or generation workflows.

## Decisions

1. Create `Assets/Tests/EditMode/` and add `EditMode.asmdef`.
   - Rationale: Keep test mode intent explicit by directory and assembly naming, align with Unity test runner conventions, and enable independent dependency management.
   - Alternative considered: keep the test in PlayMode and optimize runtime test selection. Rejected because classification remains semantically incorrect and CI remains slower than necessary.

2. Move `SchemaTests.cs` into the EditMode assembly with namespace updated to `Tests.EditMode`.
   - Rationale: Prevent accidental PlayMode discovery and make mode ownership obvious to contributors.
   - Alternative considered: duplicate test in both modes temporarily. Rejected to avoid duplicate execution and divergence risk.

3. Update CI to run both EditMode and PlayMode tests.
   - Rationale: Retain current PlayMode coverage while adding explicit enforcement for EditMode-only tests.
   - Alternative considered: switch CI entirely to EditMode for this suite. Rejected because other existing tests may depend on PlayMode semantics.

4. Keep path resolution behavior unchanged (`Application.dataPath` based lookup).
   - Rationale: Minimize behavioral risk during mode migration and preserve compatibility with current repo layout.
   - Alternative considered: refactor to project-root-relative path helper. Rejected for this change to avoid expanding scope.

## Risks / Trade-offs

- [CI runtime increases by adding EditMode execution] -> Mitigation: keep test jobs parallelizable and monitor workflow duration after rollout.
- [Assembly reference mismatch in new EditMode asmdef] -> Mitigation: mirror only required references from current test and validate test discovery locally/in CI.
- [Path assumptions differ between local editor and CI container] -> Mitigation: preserve existing lookup strategy and verify with CI run after migration.
- [Contributors may add future filesystem tests back to PlayMode by habit] -> Mitigation: make folder/assembly naming explicit and reflect expectation in tasks/documentation.
