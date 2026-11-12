# Spec Delta

## Purpose

Provide repeatable startup measurements, reference baselines, and documented budgets so that startup optimizations are verified with data and regressions are detected.

## ADDED Requirements

### Requirement: Builds record startup milestones
Every player build SHALL record the elapsed time from process start (on the web, from page navigation start) to each startup milestone: engine ready, first scene loaded, first frame drawn, Main Menu interactive, play screen loaded, and table playable. Release builds SHALL record them without visible output or measurable startup cost, and SHALL write one summary log line.

#### Scenario: Normal release launch
- **WHEN** a release build is launched and reaches the Main Menu
- **THEN** the player log contains one startup summary line with the elapsed time of each milestone reached
- **AND** no timing information is shown to the player

#### Scenario: Milestone not reached
- **WHEN** a session ends before reaching a milestone
- **THEN** the summary marks that milestone as not reached instead of reporting a misleading time

### Requirement: Benchmark mode automates the startup path
Player builds SHALL support an opt-in benchmark mode, started by a command-line argument or deep link, that goes from launch to the Main Menu, starts the selected bundled game with its start decks, performs a first card action, writes the milestone timings in a machine-readable form, and quits. Benchmark mode SHALL NOT change saved player preferences.

#### Scenario: Run the benchmark
- **WHEN** a build is launched in benchmark mode
- **THEN** it reaches a playable table without input, writes machine-readable timings, and exits
- **AND** the player's saved game selection and settings are unchanged afterwards

#### Scenario: Benchmark failure
- **WHEN** a milestone is not reached within a timeout in benchmark mode
- **THEN** the build records which milestone failed, exits with a failure status, and does not hang

### Requirement: Budgets are judged on documented devices and conditions
The project SHALL document, per platform, the reference devices, startup conditions (first install, repeat launch with saved data, resume from background, web first visit, web repeat visit), run counts, and budgets. Results SHALL be reported as median, 95th percentile, and failure count, never as a single best run.

#### Scenario: Report a measurement
- **WHEN** a contributor reports startup results for a change
- **THEN** the report names the device, build, condition, run count, median, 95th percentile, and failures

### Requirement: A minimal reference build separates engine and CGS cost
The project SHALL maintain a minimal Unity reference build using the same Unity version and relevant player settings as CGS, containing one scene with one working button, and SHALL measure it under the same conditions as CGS.

#### Scenario: Compare against the reference build
- **WHEN** CGS and the reference build are measured on the same device and condition
- **THEN** the report shows how much of CGS's time to an interactive Main Menu exceeds the reference build's time to an interactive button

### Requirement: Startup regressions are detected before release
An automated check SHALL run benchmark mode on a standalone build and fail when a milestone exceeds its documented threshold, and each release SHALL be measured on the reference devices before publishing.

#### Scenario: A change slows startup
- **WHEN** a change makes the automated benchmark exceed its Main Menu or playable-table threshold
- **THEN** the check fails and reports the milestone and measured times
