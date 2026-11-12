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
- **The web build blocks on downloads.** On WebGL, `CardGameManager.Start` downloads the three default games sequentially before `IsCurrentReady`. `webGLCompressionFormat` is 2, which appears to be Disabled.

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

### D1. Measure before optimizing, using one timeline type
Add a static `StartupTimeline` with these behaviors:
- It records `Stopwatch`-based marks from the earliest `RuntimeInitializeOnLoadMethod` (`SubsystemRegistration`).
- It anchors those marks to process start using platform APIs:
  - Android: `Process.getStartElapsedRealtime` via JNI.
  - iOS/macOS: the process start time from `sysctl`.
  - Windows/Linux: `Process.StartTime`.
  - WebGL: `performance.now()`, which counts from navigation start.
- It writes a single summary line, plus JSON in benchmark mode.

Benchmark mode is triggered by `-cgsBenchmark` or a `benchmark` deep-link query. It drives the real UI path, as opposed to calling internal methods, so the measurements reflect the player's path.

*Alternatives:* using only the Unity Profiler or Android Macrobenchmark/XCTest. These remain useful for diagnosis and as external cross-checks, but neither identifies "table playable", and neither runs on every platform.

### D2. Delete the title screen; Main Menu becomes build index 0
Remove `TitleScreen.unity` and `TitleScreen.cs`, along with title-only sprites, strings, and localization inventory entries that nothing else uses. Then shift the `Tags` scene indices. `MainMenu` already sets the version text and already contains the `CardGameManager` prefab, so no bootstrap scene is needed.

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
Make hosting non-blocking first: the table becomes playable before `StartHost` is called, and the host starts on a later frame. Then use D1 data to compare three options:
- (a) keep auto-hosting, deferred;
- (b) offline solo with host-on-demand;
- (c) lazy network-prefab instantiation.

Record the result in this design before archive. The spec requires only that networking never delays the table and that joinability is preserved unless that recorded decision changes it.

*Alternatives:* deciding now. The user explicitly deferred this to measurements.

### D6. Input readiness instead of a time buffer
Replace `StartBufferTime` with a release-then-press gate. Any button, key, or touch held when the menu becomes interactive is ignored until it is released. This keeps the original protection against carry-over presses from the previous scene without a fixed delay.

### D7. Background preload of PlayGame
While the Main Menu is idle, start `LoadSceneAsync(PlayGame)` with `allowSceneActivation = false`. Start Game then activates the preloaded scene.

Guardrails:
- Navigating to any other scene cancels the preload, by activating it into an unloaded state or by not starting it until idle. This matters because Unity serializes async loads and a held activation would block Settings, DeckEditor, and CardsExplorer.
- Async loading thread priority stays at Normal to protect menu responsiveness.

Other scene transitions move to `LoadSceneAsync` with a single-frame fade only where measurement shows that a blocking load exceeds one frame.

*Alternatives:* additive persistent scenes (Main Menu and the table always loaded). This is more memory, more complexity, and invasive for multiplayer object lifetimes.

### D8. Card data cache and off-thread parse
After a successful load, write a versioned binary snapshot of the parsed card list, keyed by game ID plus a hash or modification time of the source files and the CGS data version. Later loads read the snapshot, and fall back to JSON when it is missing, stale, or corrupt.

On platforms with threads (not WebGL), JSON parsing and snapshot reading run on a worker thread. Unity objects are created on the main thread in frame-budgeted batches.

Implement this only if D1 shows card loading on the critical path for bundled games, or above budget for a reference "large game" fixture.

*Alternatives:* lazy per-set loading. This conflicts with search and deck import, which assume all cards are present.

### D9. First-launch install without blocking
Make first-launch installation non-blocking:
- Install the default selected game synchronously only if it cannot be read in place. On desktop and iOS, StreamingAssets can be read directly.
- Install the remaining bundled games on a background thread on Android and desktop, using a per-game completion marker so that an interrupted install is detected and repaired.
- Notify the game list when each game becomes available.

*Alternatives:* shipping the bundled games as one zip. Fewer files, but extraction is still serial and the whole archive is redone if it fails.

### D10. Engine and build configuration, each change measured
Measure each of these as a separate experiment against the D1 benchmark:
- Remove unused built-in modules: terrain, terrain physics, cloth, vehicles, wind, Umbra, AI, video, and 3D physics if no dependency needs them.
- On Android, try engine ThinLTO, IL2CPP code generation set to optimize for runtime speed, and the Vulkan-vs-GLES ordering.
- Keep managed stripping at its current level unless the reflection/AOT audit (`AotTypeEnforcer`, Newtonsoft) supports raising it.

### D11. Rendering warmup
Use a Unity 6 `GraphicsStateCollection` to record the menu and table graphics states in a trace build, and warm them up during idle Main Menu time. Shader variant stripping keeps the build small. This applies only if D1 frame-time data shows first-use hitches above 100 ms.

### D12. Web delivery
Make these web delivery changes:
- Download only the selected default game before play, and fetch the others in the background and in parallel. Alternatively, bundle the three default games' data into the build so the first visit needs no game downloads.
- Enable Brotli, or confirm that the host serves pre-compressed files with correct `Content-Encoding` (Cloudflare/GitHub Pages), and keep data caching enabled.
- Use the D1 web milestones for both first-visit and repeat-visit reporting.

## Risks / Trade-offs

- **A deferred load leaves code that assumes `Current.HasLoaded` after scene load working with incomplete data.** → Mitigation: audit callers of `ResetGameScene` / `OnSceneActions`, gate on the content-ready state, and add PlayMode tests that open each scene before content is ready.
- **The startup language must show on the first Main Menu frame, which can conflict with the 500 ms budget if locale tables or CJK fonts load slowly.** → Mitigation: keep the selected-locale preload, measure the localization-ready milestone separately, and if it is over budget, render the menu only after the startup locale is ready, then optimize table and font size rather than flashing English.
- **Removing the title screen shifts scene indices and could break deep links, tests, and saved references.** → Mitigation: use named constants, add a PlayMode test for every deep-link target, and search the code for literal indices.
- **A background PlayGame preload can block other async loads and use memory on low-end devices.** → Mitigation: start the preload only when the menu is idle, cancel it on navigation, and skip it below a memory threshold.
- **The card cache could serve stale data after a game update or a CGS schema change.** → Mitigation: key the cache on the source hash and data version, delete it on update or delete, and treat any read error as a cache miss.
- **Background bundled-game installation can race with game selection or deletion.** → Mitigation: install per game with completion markers, and have the game list ignore incomplete directories.
- **Benchmark-mode code could ship and be triggered by untrusted deep links.** → Mitigation: benchmark mode performs only local navigation, does not change preferences, and exits; it never downloads or imports content.
- **CI timing is noisy.** → Mitigation: use loose CI thresholds aimed at large regressions; the release gate relies on reference-device runs.

## Migration Plan

1. Land the D1 instrumentation and benchmark first, and record baselines per platform in `developer-docs/startup-performance.md`.
2. Land changes in the task-group order, re-measuring after each group and recording results.
3. Players upgrading lose the title screen with no data migration. Saved preferences, bundled games, and user games are unaffected. The card cache is created lazily and can be deleted safely.
4. Rollback: each group is an independent commit set on `develop`. Reverting the title-screen removal restores the scene and its indices together.

## Open Questions

- Whether an HTML/JS pre-Unity menu for the web is worth building, once D12 results for web repeat visits are known.
- The final reference device list per platform, chosen from hardware the maintainer owns or can rent. It must include an older supported Android phone and an ordinary laptop.
