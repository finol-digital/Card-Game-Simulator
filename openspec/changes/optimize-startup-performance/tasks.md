# Tasks

## 1. Startup measurement and baselines

- [ ] 1.1 Add `StartupTimeline` with the milestones, launch and play summary emission points, and timeouts from the `startup-benchmarking` spec; verify with PlayMode tests for menu-only, play, and timeout sessions (exactly one launch summary, at most one play summary, unreached milestones marked)
- [ ] 1.2 Add the exact process-relative anchors (Android `getStartElapsedRealtime`, harness-supplied monotonic timestamp on desktop, `performance.now()` on web), reporting others as unavailable, plus `reportFullyDrawn()` at Main Menu interactive on Android and a resume anchor; verify on IL2CPP device builds for Android and iOS and on each desktop OS that offsets match harness timings within a documented tolerance
- [ ] 1.3 Add benchmark mode (`-cgsBenchmark` argument and `benchmark` deep-link query) with a configurable Start Game delay defaulting to zero, a designated benchmark game, suppressed preference writes, ignored deep-link game URLs, and per-platform result delivery from design D1; verify on desktop (file plus exit status), Android (logcat/file via `adb`), iOS (XCTest harness), and web (browser harness) for success and timeout, with a saved Mahjong selection and remembered start-deck choice unchanged afterwards
- [ ] 1.4 Write the external launch-action harness scripts per platform (Android `am start -W`/Macrobenchmark, iOS `XCTApplicationLaunchMetric`, desktop monotonic-clock spawner, web Playwright) under `tools/startup-benchmark/`; verify that each produces median, p95, and failures for 30 runs on one device
- [ ] 1.5 Create the minimal Unity 6.6 reference project (one scene, one working button, CGS player settings) under `tools/startup-reference/` with its own timing mark; verify it builds and reports time to an interactive button on each platform
- [ ] 1.6 Write `developer-docs/startup-performance.md` covering the reference devices and their PR build channels from design D1a (with the Lenovo laptop's hardware details), startup conditions (including resume), run counts, budgets, the harness, benchmark, and reference-build procedures, result channels, and the reporting format; verify that a contributor can follow it to produce one baseline report
- [ ] 1.7 Record baseline CGS and reference-build results (median, p95, failures) for every platform and condition in the guide; verify each required platform/condition cell is filled or explicitly marked unavailable

## 2. Open directly on the Main Menu

- [ ] 2.1 Move the version-text provider (`TitleScreen.LocalizedVersion` and its `title.version` key) used by `MainMenu` into `MainMenu` or a shared helper, then remove `TitleScreen.unity`, `TitleScreen.cs`, and title-only assets/strings; make `MainMenu` build index 0 and update `Tags` scene indices; verify the build compiles and launches to the Main Menu with its version text, and that `rg -n "TitleScreen|title\\.start" Assets ProjectSettings translations developer-docs` finds no leftover references
- [ ] 2.2 Update the localization coverage inventory and translation sources for the removed title strings; verify that the localization coverage tests pass and that the inventory lists five scenes
- [ ] 2.3 Add PlayMode tests that each deep-link target (`main`, `play`, `decks`, `cards`, `settings`, and the game `url` query) opens the right scene on cold start and when CGS is already running; verify the tests pass
- [ ] 2.4 Replace `StartBufferTime` with a release-then-press gate in `MainMenu`; verify with PlayMode tests that a fresh first-frame press acts and a press held over from before is ignored until released
- [ ] 2.5 Make sure that a Main Menu control is selected for gamepad/keyboard users on the first interactive frame; verify with a PlayMode test that simulates a gamepad press on that frame
- [ ] 2.6 Re-measure with the benchmark and record the change in the guide

## 3. Show the Main Menu before card data loads

- [ ] 3.1 Split readiness into properties-ready and content-ready states on `CardGameManager`/`UnityCardGame`, and gate the Main Menu only on properties; verify with a PlayMode test that the Main Menu is interactive while a large fixture game's content is still loading
- [ ] 3.2 Replace the synchronous `Load` in the scene-load reset with `LoadAsync` started after the first interactive frame, decode carousel banners asynchronously, and audit the remaining synchronous `Load` callers; verify the existing `CardGameManagerTests` and new tests pass, and that only the documented fallback uses synchronous `Load`
- [ ] 3.3 Move the automatic game-update check after the Main Menu is interactive, keeping progress UI; verify with a test that a game due for update starts updating only after the interactive milestone
- [ ] 3.4 Verify that a background load failure still shows the existing load-error prompt without blocking the menu (PlayMode test with a corrupt fixture)
- [ ] 3.5 Make Settings, Deck Editor, Cards Explorer, and warm deep links behave correctly while content is loading (progress where card data is needed, fill-in on completion, no double load), and make rapid game changes keep only the latest selection; verify with PlayMode tests that open each screen and switch games several times during a slow fixture load
- [ ] 3.6 Measure the localization-ready milestone against the first Main Menu frame; wait for the startup locale up to a documented bound, falling back to readable English with in-place refresh and a diagnostic on delay or failure; verify with PlayMode tests for saved Korean and Japanese, command-line override, device locale, English fallback, Simplified Chinese fonts, and simulated delayed and failed initialization

## 4. Defer optional services

- [ ] 4.1 Replace eager `UnityServices.InitializeAsync` with a lazy `EnsureServicesAsync` awaited by the lobby, relay, and authentication entry points; verify by a test that services are uninitialized after launch and that the online lobby flow still works in a manual multiplayer check per `developer-docs/multiplayer-debugging.md`
- [ ] 4.2 Start LAN discovery only when the lobby or hosting needs it; verify that LAN join still works using the LAN guide
- [ ] 4.3 Initialize Steam after the first interactive frame and only for Steam builds or when Steam is detected; verify on Steam Deck per `developer-docs/steam-deck-validation.md`, and that a non-Steam desktop build logs no Steam initialization
- [ ] 4.4 Evaluate moving the network components out of the `CardGameManager` prefab into a lazily created prefab, and record the measured benefit in design.md; verify that the decision and data are written down
- [ ] 4.5 Re-measure and record results in the guide

## 5. Continuous startup visuals

- [ ] 5.1 Set the platform startup images and background colors (Android, iOS, desktop, web loader) to match the Main Menu background; verify with a frame capture from launch to the first interactive frame on Android, iOS, and desktop that no color flash appears

## 6. Non-blocking first-launch installation

- [ ] 6.1 Install the default selected game first, showing its properties and banner from the bundle if its copy exceeds the launch budget and making Start Game wait for the copy with progress; verify with a first-launch test that the Main Menu shows the default game within the first-install budget
- [ ] 6.2 Install the remaining bundled games in the background via temporary directories, atomic rename, and completion markers, refreshing the game list as each becomes available; verify with tests for the list update, repair after a simulated interrupted install, and updating, editing, and deleting a bundled game followed by a relaunch (a deleted game stays deleted)
- [ ] 6.3 Measure the first-install condition on Android, iOS, and desktop, and set first-install budgets in the guide; verify that the measurements and budgets are recorded

## 7. Engine and build configuration

- [ ] 7.1 Remove unused built-in modules after checking package dependencies; verify that all platform builds succeed, the PlayMode tests pass, and the benchmark delta is recorded
- [ ] 7.2 Benchmark Android engine ThinLTO (documenting its build-setting prerequisites), then IL2CPP runtime-speed code generation, then Vulkan/GLES ordering, one at a time, keeping only measured improvements; verify that the results table is in the guide and the chosen settings are committed
- [ ] 7.3 Review managed stripping against the AOT/reflection needs (`AotTypeEnforcer`, Newtonsoft) and change it only if it is measured as safe and beneficial; verify with a full IL2CPP build smoke test across all scenes

## 8. Fast single-player start

- [ ] 8.1 Make solo hosting non-blocking so the table is playable before host startup begins, converting existing table objects into networked objects when hosting starts; verify with a PlayMode test that a card action succeeds before `StartHost` is called, and with a LAN check that a player joining after decks were loaded and cards moved sees the same table and can act on it
- [ ] 8.2 Benchmark the D5 options (deferred auto-host, offline with host-on-demand, lazy network prefab), record the decision in design.md, and implement it; verify that the decision and data are recorded and, if joinability changes, that the player documentation is updated
- [ ] 8.3 Switch Start Game to an unheld `LoadSceneAsync` and cut PlayGame's first-frame cost (lazy menus and viewers per design D7); verify with PlayMode tests that the Main Menu keeps rendering during the load and that the Start Game → play-screen-loaded milestone improves
- [ ] 8.4 Only if 8.3 data misses the budget, add the gated additive preload from design D7; verify with PlayMode tests that navigating to Settings, Deck Editor, and Cards Explorer during and after the preload adds no delay and creates no host, decks, scene-reset callbacks, or blocked localization loads
- [ ] 8.5 Open the play screen immediately when content is still loading, showing progress and loading start decks once content is ready without reloading cards; verify with a PlayMode test using a slow-loading fixture
- [ ] 8.6 Add the "always load start decks" choice to the start-deck prompt and Settings, localized in all shipped languages; verify with PlayMode tests for remembering, auto-loading, and resetting the choice, and with localization coverage tests
- [ ] 8.7 Measure Start Game → playable (benchmark delay zero) and launch → playable (external harness, saved data) over at least 30 runs per reference device, plus first install separately, and record them; verify against the `single-player-start` budgets

## 9. Data-driven deep optimizations

- [ ] 9.1 Using the group 3 and group 8 data plus a large-game fixture, decide whether the card-data cache and off-thread parse (D8) are needed, and record the decision in design.md; verify that the decision and data are recorded
- [ ] 9.2 If needed, implement the versioned card snapshot with off-thread parse (non-WebGL) and frame-budgeted object creation; verify with tests for cache hit, stale invalidation, corrupt-cache fallback, and identical card results versus a JSON load
- [ ] 9.3 Record frame times from an immediate Start Game to the first card action with cold caches on each graphics backend the reference devices use; if any frame exceeds 100 ms from first-use rendering, add the per-backend incremental warmup from design D11; verify by re-measuring with cold caches and zero menu idle time that no frame exceeds 100 ms

## 10. Web startup

- [ ] 10.1 Stop blocking web play on sequential default-game downloads: download the selected game first and the rest in the background, or bundle the default game data, and document the choice; verify that a first-visit web session can start the selected game before other downloads finish
- [ ] 10.2 Enable Brotli compression, or confirm correct pre-compressed serving with `Content-Encoding` on the hosting path, keeping data caching enabled; verify with response headers from the deployed or preview host and a repeat-visit run that does not re-download game data
- [ ] 10.3 Record web first-visit and repeat-visit milestones under the documented network and cache settings, and set the web budgets in the guide; verify that the results are recorded

## 11. Regression guard and final verification

- [ ] 11.1 Add a script that builds or uses a standalone Linux player, runs benchmark mode N times, and fails above the documented loose thresholds; verify that it fails on an artificially delayed build and passes on the current build. If wiring it into CI requires `.github` changes, hand that to the maintainer instead of editing workflows
- [ ] 11.2 Run the full benchmark on every reference device and condition, and confirm the `app-launch` and `single-player-start` budgets at p95; verify that the final results table in the guide shows every budget met, or an explicit exception approved by the maintainer
- [ ] 11.3 Run the full PlayMode suite and the multiplayer, LAN, and Steam Deck manual checks; verify that all pass

## Workflow follow-up

- Archive the change after the maintainer approves the final measurements, and verify that `openspec/specs/` contains `app-launch`, `single-player-start`, and `startup-benchmarking` and that the updated `localized-interface` coverage text no longer lists TitleScreen.
