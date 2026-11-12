# Design

## Context

See proposal.md for motivation and specs for the required behavior. This section records only the current startup path (Unity 6.6.8f1) that shapes the approach.

- **The Unity splash screen is already off** (`m_ShowUnitySplashScreen: 0`). What players see is the platform's static startup image or background while the engine boots and the first scene initializes.
- **The first scene is `TitleScreen`.** Every scene contains the `CardGameManager` prefab, which also carries `CgsNetManager`, `UnityTransport`, `CgsNetDiscovery`, `Dialog`, and `ProgressBar`. As a result, every first scene pays for multiplayer components.
- **`CardGameManager.Awake` blocks the first frame:**
  - On first launch, it synchronously copies or extracts about 13 MB across 380 files of bundled games from StreamingAssets (`CreateDefaultCardGames`).
  - It reads every game's `cgs.json` (`LookupCardGames`).
  - Its `sceneLoaded` handler calls `ResetGameScene`, which runs the full synchronous `UnityCardGame.Load`. That load starts card-page coroutines and synchronously loads sets and decodes the banner, card-back, back-face, and play-mat PNGs. It can also start an automatic network update.
- **Optional services initialize at launch:**
  - `CgsNetManager.Start` calls `UnityServices.InitializeAsync()`.
  - `SteamDeckSupport` calls `SteamAPI.Init()` before scene load in every standalone build.
  - Localization initializes asynchronously before scene load, with preload of the selected locale's tables.
- **Scene changes block.** Every scene change uses synchronous `SceneManager.LoadScene`.
- **Main Menu ignores early input.** It drops input for `StartBufferTime` (100 ms) after load.
- **The Play flow waits on several things:**
  - `PlayController.Start` waits for `IsCurrentReady`.
  - On non-web platforms, it calls `Lobby.Host()` even for solo play.
  - It then prompts before loading start decks.
- **The web build blocks on downloads.** On WebGL, when the current game is missing, errored, or not loaded (typical of a first visit), `CardGameManager.Start` downloads the three default games sequentially before `IsCurrentReady`. `CGS_SINGLEGAME` web builds download their single site `cgs.json` instead. `webGLCompressionFormat` is 2, which appears to be Disabled.

## Goals / Non-Goals

**Goals:**
- Reach the specs' budgets while recording why each change was made, backed by before/after measurements.
- Order the work so that the cheapest, highest-confidence changes land first and later structural work is justified by data.

**Non-Goals:**
- Rewriting the UI framework (uGUI → UI Toolkit) or replacing Netcode for GameObjects.
- Guaranteeing web first-visit times under arbitrary network conditions.
- A separate HTML/JS pre-Unity web menu. This is deferred; see Open Questions.
- Changing the card-game JSON format that game authors publish. Any cache is a local derivative.

## Decisions

### D1. Measure from the launch action externally; keep in-app timings as diagnostics
Budgets are judged by an external harness that records the launch (or resume) action and the judged milestone on one monotonic clock:
- **Android:** `am start -W` / Macrobenchmark startup timing. The app calls `Activity.reportFullyDrawn()` when the Main Menu is interactive, so the system's time-to-full-display is the menu milestone.
- **iOS:** high-frame-rate video of the TestFlight build (see D1a). In benchmark mode the app shows a small visual marker on the exact frame the Main Menu becomes interactive (and again when a card action succeeds), so the video measures from the launch tap to that interactive marker on one clock (the video's frame timeline), not merely to the menu appearing. On a local development build, an XCTest interval such as `XCTOSSignpostMetric`, ending at the app's Main Menu interactive signpost, is an optional cross-check; `XCTApplicationLaunchMetric` responsiveness is diagnostic only.
- **Desktop:** a harness script reads the OS monotonic clock (`CLOCK_MONOTONIC`, `QueryPerformanceCounter`, `mach_continuous_time`) just before spawning the player and passes it as an argument; the app logs milestones on the same clock.
- **Web:** Playwright/Puppeteer records navigation start and reads milestones from the page via `performance.now()`, which already shares that origin.
- **Resume:** the harness timestamps the foreground action; the app records an `OnApplicationPause(false)` / focus mark as the in-app anchor.

In the app, a static `StartupTimeline` records `Stopwatch` marks from the earliest `RuntimeInitializeOnLoadMethod` (`SubsystemRegistration`). Process-relative offsets are added only where the conversion is exact: Android `Process.getStartElapsedRealtime()` (API 24+, same `elapsedRealtime` origin), Linux/Windows/macOS via the harness-provided timestamp, and the web via `performance.now()`. Anything else is reported as unavailable, never estimated. Native adapters are validated on real IL2CPP device builds, not only in PlayMode.

Summaries follow the emission points in the `startup-benchmarking` spec: a launch summary at Main Menu interactive or the launch timeout, and a play summary at the first playable table or the play timeout.

Benchmark mode is triggered by `-cgsBenchmark`, or by a `benchmark` deep-link query only when that link launches CGS. A deep link received while CGS is already running never enters benchmark mode, so it cannot take over or quit an active session. Benchmark mode drives the real UI path (not internal shortcuts) and chooses Start Game after a configurable delay that defaults to zero, so idle-time preparation cannot silently improve results. Result delivery per platform:
- **Desktop:** a JSON file at an argument-supplied path, then exit with a success or failure status.
- **Android:** a tagged logcat line plus a JSON file in app storage that the harness pulls with `adb`; the harness stops the app.
- **iOS:** an `os_log` line plus a JSON file in the app container read by the XCTest harness.
- **Web:** a `window` result object and a console line read by the browser harness.

Preference isolation: in benchmark mode, writes to PlayerPrefs (including `DefaultGame`), remembered start-deck choices, and settings are suppressed in memory instead of being saved, and the designated benchmark game is selected without persisting. Benchmark mode ignores deep-link game URLs and imports, and may download only the designated default games from the official host when the condition requires it (web first visit).

*Alternatives:* using only in-app process-relative timings. These miss the launch-action-to-process delay, have no new process on resume, and mix clock origins across platforms.

### D1a. Reference devices and test builds
Reference devices, chosen by the maintainer:

| Device | Platform | Build under test | How it is obtained |
|---|---|---|---|
| 16-year-old Lenovo laptop | Windows (or Linux) desktop | `cgs-StandaloneWindows64` (or `cgs-StandaloneWindows` / `cgs-StandaloneLinux64`) | Artifact of the develop → main PR run of `main.yml` |
| Samsung Galaxy Z Flip 3 | Android | Google Play beta track build | `deployToGooglePlayTest` on the PR run |
| iPhone 11 | iOS | TestFlight build | `deployToAppleTest` on the PR run |
| Steam Deck | Linux (SteamOS) | Steam `prerelease` branch | `deployToSteam` on the PR run |

The web version is measured on the Firebase `preview` channel from `deployToFirebaseTest`, using the Lenovo laptop and the Z Flip 3 browsers.

Single-game web builds (`CGS_SINGLEGAME`, profile `Web_SingleGame`) are not built by `main.yml` and are excluded from benchmarking, so the benchmark's download rule needs no exception for their site `cgs.json`.

Consequences for the harnesses and benchmark mode:
- **All of these are release builds**, so benchmark mode and milestone recording must work in release builds, and benchmark mode must be triggerable without a debugger: by command-line argument on desktop and Steam (Steam launch options or running the game binary directly), and by deep link (`adb shell am start -W -d <benchmark link>` on Android, or opening the link on iOS).
- **iOS:** `XCTApplicationLaunchMetric` needs a development-signed UI-test target and cannot drive a TestFlight build, so the iPhone 11 is measured with high-frame-rate video of the launch tap up to the benchmark's visual interactive marker (D1), plus the `os_log` results. An XCTest run from a local development build is an optional cross-check.
- **Android:** `am start -W` and `reportFullyDrawn()` work with the Play-distributed release build over `adb`. Macrobenchmark, which needs a profileable build, is an optional local cross-check.
- **Steam Deck:** measure both launching through Steam (as players do) and running the binary directly from game mode's desktop, to separate Steam client overhead.
- **Old laptop:** this is the lowest-end device. Record its OS, CPU, GPU, RAM, and disk type (HDD or SSD) in the guide, and confirm it meets Unity 6's minimum player requirements before treating its results as budget evidence.
- **Minimal reference build:** this is a separate project, so it is not built by `main.yml`. It is built locally, and the maintainer decides whether to add it to CI, because agents do not edit `.github`.
- **When builds exist:** PR builds run when a develop → main PR is opened, and on later pushes to `develop` that touch `Assets`, `Packages`, or `ProjectSettings` while that PR is open. A plan-only change produces no new builds, so measurement uses the build of the commit under test.

### D2. Delete the title screen; Main Menu becomes build index 0
Remove `TitleScreen.unity` and `TitleScreen.cs`, along with title-only sprites, strings, and localization inventory entries that nothing else uses. Then shift the `Tags` scene indices. `MainMenu` sets its version text through `TitleScreen.LocalizedVersion`, so that provider (`VersionMessage`, `LocalizedVersion`, and the `title.version` key) moves into `MainMenu` or a small shared helper before `TitleScreen.cs` is deleted. `MainMenu` already contains the `CardGameManager` prefab, so no bootstrap scene is needed.

*Alternatives:*
- An empty bootstrap scene that loads Main Menu asynchronously. This adds a scene load for no benefit, because every scene self-bootstraps through the prefab.
- Auto-advancing the title screen. The user chose to remove it.

### D3. Two-tier game loading: properties now, content later
The Main Menu needs only `ReadProperties` data and the banner. Changes:
- `OnSceneLoaded` no longer triggers a synchronous `Load`. Instead, after the first interactive frame, `CardGameManager` starts the existing `LoadAsync` path for `Current`.
- Banner decoding for the visible carousel uses `CreateSpriteFromFileAsync`.
- `IsCurrentReady` splits into "properties ready", which gates the menu, and "content ready", which gates the table.
- Synchronous `Load` remains only as a fallback for callers that truly need it. Those callers are audited and migrated.

*Alternatives:* keeping synchronous `Load` and making it faster. Even with a fast load, its cost scales with game size, so a large custom game would still miss the menu budget.

### D4. Defer optional services behind first use
These services are moved behind first use:
- **Unity Services:** `UnityServices.InitializeAsync` moves into a lazy `EnsureServicesAsync()`, awaited by online lobby, relay, and authentication entry points.
- **LAN discovery:** it starts when the lobby or hosting needs it.
- **Steam:** Steam initializes after the first interactive frame, and only when the build is a Steam build or Steam is detected.
- **Automatic game updates:** the update check moves after the Main Menu is interactive.

Splitting the network components out of the `CardGameManager` prefab, into a lazily instantiated prefab, is evaluated with D1 data; only the timing gate is required.

*Alternatives:* starting services in parallel at launch, unawaited. This still costs main-thread time and, on some platforms, competes for I/O during the critical window.

### D5. Single-player hosting is a measured decision
Make hosting non-blocking first: the table becomes playable before `StartHost` is called, and the host starts on a later frame. `PlayController` spawns stacks, cards, dice, counters, and zones as network objects only when online, so when hosting starts after play began, the host converts the existing table objects into networked objects, preserving positions, order, faces, and values, so a player who joins later sees the same table. Then use D1 data to compare three options:
- (a) keep auto-hosting, deferred;
- (b) offline solo with host-on-demand;
- (c) lazy network-prefab instantiation.

Record the result in this design before archive. The spec requires only that networking never delays the table and that joinability is preserved unless that recorded decision changes it.

*Alternatives:* deciding now. The user explicitly deferred this to measurements.

### D6. Input readiness instead of a time buffer
Replace `StartBufferTime` with a release-then-press gate. Any button, key, or touch held when the menu becomes interactive is ignored until it is released. This keeps the original protection against carry-over presses from the previous scene without a fixed delay.

### D7. Make the Play scene cheap to open; preload only with gated initialization
`LoadSceneAsync` with `allowSceneActivation = false` cannot be cancelled, stalls every later async operation (including other scene and localization loads) until it activates, and activating a Single-mode load replaces the Main Menu and runs PlayGame lifecycle code. A held preload is therefore not used.

Default approach, in order:
1. Start Game uses an unheld `LoadSceneAsync(PlayGame)` so the Main Menu keeps rendering feedback during the load.
2. Reduce PlayGame's own load cost: lazily instantiate the menus it does not need on the first frame (several already are), and move `CardViewer`/`PlayableViewer` creation off the critical path where measurement shows a cost.
3. Ensure the selected game's content is loaded or loading before Start Game (D3), which removes most of the remaining wait.

Only if D1 data shows that PlayGame activation alone still misses the budget, evaluate an additive preload with these rules:
- The PlayGame root is saved inactive, and `PlayController` and network components do nothing until an explicit activation call. No hosting, deck creation, scene-reset callbacks (`OnSceneActions`), or input bindings happen before Start Game.
- Activation: the additive scene becomes active, the Main Menu scene unloads, and the gated components initialize.
- Leaving for any other screen: wait for the additive load to finish (it is never held), then `UnloadSceneAsync` it before or alongside the navigation, and measure that this adds no delay.
- Tests navigate away both while the preload is in progress and once it has completed, and assert no hosting, decks, callbacks, or blocked localization loads.

*Alternatives:* a held preload (rejected for the reasons above); persistent additive scenes (more memory, more complexity, and invasive for multiplayer object lifetimes).

### D8. Card data cache and off-thread parse
After a successful load, write a versioned binary snapshot of the parsed card list, keyed by game ID plus a hash or modification time of the source files and the CGS data version. Later loads read the snapshot, and fall back to JSON when it is missing, stale, or corrupt.

On platforms with threads (not WebGL), JSON parsing and snapshot reading run on a worker thread. Unity objects are created on the main thread in frame-budgeted batches.

Implement this only if D1 shows card loading on the critical path for bundled games, or above budget for a reference "large game" fixture.

*Alternatives:* lazy per-set loading. This conflicts with search and deck import, which assume all cards are present.

### D9. First-launch install without blocking, keeping one writable games root
All game paths derive from `persistentDataPath/games`, and updates, edits, deletion, set-card downloads, and `ReadProperties` (which can move a game directory) assume writable content. Reading bundled games in place from StreamingAssets would need a second read-only root with copy-on-write, override precedence, and tombstones for deleted bundled games. That is out of scope, so the single writable root stays.

Instead:
- On first launch, install the default selected game first. If its copy exceeds the launch budget, the Main Menu shows its properties and banner read directly from the bundle (read-only, display only) while the copy finishes, and Start Game waits for the copy with visible progress.
- Install the remaining bundled games on a background thread, each into a temporary directory that is atomically renamed into place, with a completion marker, so an interrupted install is detected and repaired on the next launch.
- Notify the game list when each game becomes available, and keep today's rule that a bundled game the player deleted is not reinstalled.

*Alternatives:* read-in-place with copy-on-write (deferred, for the reasons above); shipping the bundled games as one zip (fewer files, but extraction stays serial and the whole archive is redone on failure).

### D10. Engine and build configuration, each change measured
Measure each of these as a separate experiment against the D1 benchmark:
- Remove unused built-in modules: terrain, terrain physics, cloth, vehicles, wind, Umbra, AI, video, and 3D physics if no dependency needs them.
- On Android, try engine ThinLTO, IL2CPP code generation set to optimize for runtime speed, and the Vulkan-vs-GLES ordering.
- Keep managed stripping at its current level unless the reflection/AOT audit (`AotTypeEnforcer`, Newtonsoft) supports raising it.

### D11. Rendering warmup per graphics backend
Apply this only if D1 frame-time data shows first-use hitches over 100 ms:
- **DX12, Vulkan, Metal:** record a `GraphicsStateCollection` per backend and platform in a trace build, and warm it up incrementally.
- **GLES, WebGL, DX11:** warm a traced `ShaderVariantCollection` incrementally instead, since PSO tracing does not apply there.
- **Timing:** warmup starts right after the Main Menu is interactive, in small per-frame slices, and does not depend on menu idle time. If Start Game arrives first, the remaining warmup for the table's states runs during the Play scene load before the board is shown.
- Unsupported or missing collections fall back to no warmup without errors.
- Verification uses an immediate Start Game with cold caches (fresh install or cleared driver and pipeline caches) on each backend the reference devices use.

### D12. Web delivery
Make these web delivery changes:
- Download only the selected default game before play, and fetch the others in the background and in parallel. Alternatively, bundle the three default games' data into the build so the first visit needs no game downloads.
- Enable Brotli, or confirm that the host serves pre-compressed files with correct `Content-Encoding` (Cloudflare/GitHub Pages), and keep data caching enabled.
- Use the D1 web milestones for both first-visit and repeat-visit reporting.

## Risks / Trade-offs

- **A deferred load leaves code that assumes `Current.HasLoaded` after scene load working with incomplete data.** → Mitigation: audit callers of `ResetGameScene` / `OnSceneActions`, gate on the content-ready state, and add PlayMode tests that open each scene before content is ready.
- **The startup language must show on the first Main Menu frame, which can conflict with the 500 ms budget if locale tables or CJK fonts load slowly.** → Mitigation: keep the selected-locale preload and measure the localization-ready milestone separately. Wait for the startup locale only up to a documented bound. On failure or timeout, follow the existing language-selection fallback (readable English, usable menu, diagnostic) and refresh in place when the locale arrives. If the successful path is over budget, optimize table and font size.
- **Removing the title screen shifts scene indices and could break deep links, tests, and saved references.** → Mitigation: use named constants, add a PlayMode test for every deep-link target, and search the code for literal indices.
- **An additive PlayGame preload (only if D7's measurement requires it) could run gameplay setup early, or delay other screens.** → Mitigation: keep the root inactive with gated initialization, never hold activation, unload it on navigation, skip it below a memory threshold, and test navigation during and after the load.
- **The card cache could serve stale data after a game update or a CGS schema change.** → Mitigation: key the cache on the source hash and data version, delete it on update or delete, and treat any read error as a cache miss.
- **Background bundled-game installation can race with game selection or deletion.** → Mitigation: install per game with completion markers, and have the game list ignore incomplete directories.
- **Benchmark-mode code could ship and be triggered by untrusted deep links.** → Mitigation: benchmark mode suppresses preference writes, ignores deep-link game URLs and imports, and may download only the designated default games from the official host.
- **The minimal reference build may itself exceed 500 ms on the 16-year-old laptop.** → Mitigation: measure it first (task 1.7). If the engine floor alone misses the budget, the maintainer records an explicit exception for that device, as task 11.2 allows, with a budget for CGS's overhead above the reference build.
- **CI timing is noisy.** → Mitigation: use loose CI thresholds aimed at large regressions; the release gate relies on reference-device runs.

## Migration Plan

1. Land the D1 instrumentation and benchmark first, and record baselines per platform in `developer-docs/startup-performance.md`.
2. Land changes in the task-group order, re-measuring after each group and recording results.
3. Players upgrading lose the title screen with no data migration. Saved preferences, bundled games, and user games are unaffected. The card cache is created lazily and can be deleted safely.
4. Rollback: each group is an independent commit set on `develop`. Reverting the title-screen removal restores the scene and its indices together.

## Open Questions

- Whether an HTML/JS pre-Unity menu for the web is worth building, once D12 results for web repeat visits are known.
