# Proposal

## Why

Launching CGS feels slow: players see a static startup image, then a "Press any key / Tap anywhere" title screen, before they can use the Main Menu, and starting a solo game adds more synchronous loading and a prompt. The Unity splash screen is already disabled, so the remaining delay comes mostly from CGS's own startup work, which runs on the main thread before the first frame. Now that the project is on Unity 6.6, we want launch to feel instant: an interactive Main Menu within 500 ms and a playable single-player table within 1 second, measured on named reference devices.

## What Changes

- **BREAKING (UX)**: Remove the title screen. Launch opens directly on the Main Menu; nothing from the title screen (prompt, footer, logo, version/company text) is retained. Scene indices shift by one.
- Add external launch-action measurement harnesses, in-app startup timing milestones, a scripted benchmark mode, a minimal Unity 6.6 reference build, and documented per-platform budgets and reference devices, so every optimization is measured and regressions are caught.
- Show the Main Menu from lightweight game properties only; load card data, sets, and game images after the menu is interactive instead of before the first frame.
- Defer optional services (Unity Services, LAN discovery, Steam) and first-launch bundled-game installation work so they do not block the first interactive frame.
- Accept Main Menu input as soon as it is shown instead of ignoring presses for an initial 100 ms buffer.
- Make the platform startup image/background visually continuous with the Main Menu.
- Reduce engine startup cost through build configuration (unused engine modules, Android LTO/IL2CPP/graphics API choices), validated by measurement.
- Make starting a single-player game fast: make the Play scene cheap to open and load it without freezing the menu (a gated background preload only if measurements require it), ensure single-player networking setup never delays the playable table (whether solo play keeps auto-hosting is decided from measurements), and let players choose to always load a game's start decks without a prompt.
- Speed up card-data loading for large games (cache and/or off-main-thread parsing, asynchronous image decoding) and avoid first-use rendering hitches.
- Web: stop blocking play on sequential downloads of default games, and serve compressed, cacheable build files. Web first-visit time is reported but held to separate budgets.

## Capabilities

### New Capabilities

- `app-launch`: What the player experiences from launching CGS until the Main Menu is usable, including startup budgets, what may and may not block the first interactive frame, first-launch content availability, deep links, and startup language.
- `single-player-start`: What the player experiences from choosing to play until a single-player table is playable, including budgets, start-deck loading choices, and networking not delaying the table.
- `startup-benchmarking`: Startup timing milestones, the automated benchmark mode, reference builds and devices, and the regression check that keeps the budgets enforced.

### Modified Capabilities

- `localized-interface`: The localization coverage requirement lists TitleScreen among the shipped scenes and refers to "six shipped scenes"; with the title screen removed, coverage applies to the five remaining scenes.

## Impact

- Scenes and build settings: `TitleScreen.unity` and `TitleScreen.cs` removed; `EditorBuildSettings` scene list and `Tags` scene indices updated; deep-link scene loading re-verified.
- `CardGameManager` (startup lookup, first-launch default-game installation, scene-load reset, web default-game downloads), `UnityCardGame` loading paths, `MainMenu`, `PlayController` startup/lobby flow, `CgsNetManager` service initialization, `SteamDeckSupport` bootstrap, and localization startup.
- `ProjectSettings` (startup background/splash images, engine module set, Android/Web player settings) and `Packages/manifest.json` (built-in modules).
- New benchmark-mode entry point, timing instrumentation, and a developer guide at `developer-docs/startup-performance.md`; PlayMode tests for startup ordering and the Play flow.
- No `.github` changes. Any CI startup-timing job requiring workflow changes is handed to the maintainer.
