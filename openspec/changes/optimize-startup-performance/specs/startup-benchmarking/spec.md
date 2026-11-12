# Spec Delta

## Purpose

Provide repeatable startup measurements, reference baselines, and documented budgets so that startup optimizations are verified with data and regressions are detected.

## ADDED Requirements

### Requirement: Budgets are measured from the launch action
Budget results SHALL come from an external harness that timestamps the launch or resume action and the milestone being judged on one monotonic clock per platform. In-app timings SHALL be labelled process-relative diagnostics and SHALL NOT be reported as launch-action budget results. When a platform clock cannot be converted reliably, the result SHALL be reported as unavailable instead of estimated.

#### Scenario: Android budget run
- **WHEN** the harness measures an Android launch
- **THEN** the reported time runs from the launch intent to the Main Menu being reported as fully drawn and interactive, and any in-app process-relative timings are labelled separately

#### Scenario: Clock conversion unavailable
- **WHEN** a build cannot relate its in-app clock to process start on a platform
- **THEN** process-relative milestones are reported as unavailable while harness-measured budget results are unaffected

### Requirement: Builds record startup milestones
Every player build SHALL record in-app milestones: engine ready, first scene loaded, first frame drawn, Main Menu interactive, play screen loaded, and table playable. Release builds SHALL record them without visible output or measurable startup cost.

#### Scenario: Normal release launch
- **WHEN** a release build is launched and reaches the Main Menu
- **THEN** the milestones up to Main Menu interactive are recorded and no timing information is shown to the player

### Requirement: Milestone summaries have defined emission points
Builds SHALL write one launch summary line when the Main Menu becomes interactive or a documented launch timeout expires, and one play summary line when the first table of the session becomes playable or a documented play timeout expires. Milestones not reached SHALL be marked not reached. A session ended abruptly before a summary is written MAY have no summary, and the benchmark SHALL treat a missing summary as a failure.

#### Scenario: Menu-only session
- **WHEN** a player launches CGS and never starts a game
- **THEN** the log contains exactly one launch summary and no play summary

#### Scenario: Play session
- **WHEN** a player launches CGS and starts a game
- **THEN** the log contains one launch summary and one play summary with the play milestones

#### Scenario: Milestone not reached
- **WHEN** a launch or play timeout expires before its final milestone
- **THEN** the summary is written with that milestone marked as not reached instead of a misleading time

#### Scenario: Interrupted session
- **WHEN** a session is terminated before a summary is written
- **THEN** a benchmark run that expected the summary reports a failure

### Requirement: Benchmark mode automates the startup path
Player builds SHALL support an opt-in benchmark mode that reaches the Main Menu, chooses Start Game for a designated bundled game after a configurable delay (default zero), loads start decks, performs a first card action, and delivers machine-readable results through a documented per-platform channel. It SHALL NOT change saved preferences or game selection, on success or failure.

#### Scenario: Run the benchmark on desktop
- **WHEN** a desktop build is launched in benchmark mode
- **THEN** it reaches a playable table without input, writes machine-readable results, and exits with a success status

#### Scenario: Run the benchmark on mobile or web
- **WHEN** a mobile or web build runs in benchmark mode under its harness
- **THEN** results are delivered through the documented channel for that platform and the harness, not the app, ends the run

#### Scenario: Benchmark failure
- **WHEN** a milestone is not reached within its timeout in benchmark mode
- **THEN** the results record which milestone failed, a desktop build exits with a failure status, and no platform hangs

#### Scenario: Preferences preserved
- **WHEN** a player whose saved game is Mahjong and who has a remembered start-deck choice runs a benchmark that succeeds or times out
- **THEN** afterwards the saved game is still Mahjong and the start-deck choice is unchanged

### Requirement: Benchmark mode limits network access
Benchmark mode SHALL NOT download or import content named by a deep link or argument. It SHALL download only the designated default games from the official CGS host, and only where the measured condition requires it, such as a web first visit.

#### Scenario: Benchmark link with a game URL
- **WHEN** benchmark mode is started by a deep link that also contains a game URL
- **THEN** the game URL is ignored and nothing is downloaded from it

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
