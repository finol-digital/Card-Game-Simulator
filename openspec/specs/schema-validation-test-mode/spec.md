# schema-validation-test-mode Specification

## Purpose
TBD - created by syncing change move-schema-tests-to-editmode. Update Purpose after archive.

## Requirements
### Requirement: Schema parity validation runs in EditMode
The test suite SHALL execute schema parity validation tests as EditMode tests rather than PlayMode tests.

#### Scenario: Running EditMode tests includes schema parity validation
- **WHEN** the Unity Test Runner executes EditMode tests
- **THEN** the schema parity validation test suite is discovered and run

#### Scenario: Running PlayMode tests excludes migrated schema parity validation
- **WHEN** the Unity Test Runner executes PlayMode tests
- **THEN** the migrated schema parity validation test suite is not run from the PlayMode assembly

### Requirement: CI executes EditMode schema parity validation
Continuous integration SHALL run EditMode tests so schema parity validation remains part of automated validation.

#### Scenario: CI test workflow runs EditMode tests
- **WHEN** the main CI workflow runs unit tests for a pull request or merge
- **THEN** EditMode tests are executed by the Unity test runner

#### Scenario: Schema parity regression fails CI
- **WHEN** schema parity validation in EditMode fails
- **THEN** the CI workflow reports failure for the test stage
