## 1. Create EditMode test assembly and migrate schema tests

- [x] 1.1 Create `Assets/Tests/EditMode/` and add `EditMode.asmdef` with required references for `SchemaTests`.
- [x] 1.2 Move `SchemaTests.cs` from `Assets/Tests/PlayMode/` to `Assets/Tests/EditMode/` and update namespace/assembly compatibility.
- [x] 1.3 Remove or adjust any stale PlayMode references so schema tests are discovered only in EditMode.

## 2. Update CI test execution

- [x] 2.1 Update `.github/workflows/main.yml` test configuration to execute EditMode tests in addition to existing PlayMode coverage.
- [x] 2.2 Ensure CI test output/failure behavior clearly fails the workflow when EditMode schema tests fail.

## 3. Validate migration behavior

- [x] 3.1 Verify EditMode test discovery includes `SchemaTests` and PlayMode test discovery excludes migrated schema tests.
- [x] 3.2 Run CI-equivalent test commands (or Unity Test Runner) to confirm both test modes execute successfully with the migrated test.
