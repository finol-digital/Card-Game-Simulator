# Startup Performance

How to measure CGS startup against its budgets. The plan and requirements are in
`openspec/changes/optimize-startup-performance/` (design D1 and D1a; the `startup-benchmarking` spec).

## Budgets

Judged at the 95th percentile over at least 30 runs, on launches with existing saved data, with Start Game chosen as
soon as the Main Menu is interactive (benchmark delay 0):

| Interval | Budget |
|---|---|
| Launch action → Main Menu interactive | 500 ms |
| Start Game → first successful card action | 500 ms |
| Launch action → first successful card action | 1000 ms |
| Single frame between Start Game and the first card action | 100 ms |

First install, resume from background, web first visit, and web repeat visit are reported separately against their
own budgets (to be set from the baselines below). A device whose minimal reference build alone misses a budget gets
an explicit maintainer-approved exception, stated relative to the reference build.

## Reference devices and builds

All measurements use the builds produced by the develop → main PR run of `main.yml`. PR builds run when that PR is
opened, and on later pushes to `develop` that touch `Assets`, `Packages`, or `ProjectSettings` while it is open.

| Device | Build | Where to get it | Harness |
|---|---|---|---|
| Lenovo laptop (about 2010) | `cgs-StandaloneWindows64` (or `cgs-StandaloneWindows` / `cgs-StandaloneLinux64`, depending on its OS) | Artifacts of the PR's "Test, Build, and Deploy with GameCI" run | `desktop_benchmark.py --player` |
| Samsung Galaxy Z Flip 3 | Google Play beta track | Play Store (beta tester) | `android_benchmark.py` |
| iPhone 11 | TestFlight | TestFlight | `ios_video_benchmark.py` (video) |
| Steam Deck | Steam `prerelease` branch | Steam > Properties > Betas | `desktop_benchmark.py --steam` and `--player` |
| Google Pixel Fold 10 | Google Play beta track | Play Store (beta tester) | `android_benchmark.py` (record the posture) |
| iPad (9th generation) | TestFlight | TestFlight | `ios_video_benchmark.py` (video) |
| Mac mini (M1) | `cgs-StandaloneOSX` (`Card Game Simulator.app`); optionally Steam `prerelease` | Artifacts of the PR's run; Steam > Properties > Betas | `desktop_benchmark.py --player` (optionally `--steam`) |
| Microsoft Surface Book | `cgs-StandaloneWindows64`; optionally Steam `prerelease` | Artifacts of the PR's run; Steam > Properties > Betas | `desktop_benchmark.py --player` (optionally `--steam`) |
| Web (laptop and Z Flip 3 browsers) | Firebase `preview` channel | URL in the PR's Firebase deploy step | `web_benchmark.mjs` |

Record each device's details here before using its results:

| Device | OS | CPU | GPU | RAM | Disk | Meets Unity 6.6 player requirements? |
|---|---|---|---|---|---|---|
| Lenovo laptop | _to fill_ | | | | HDD / SSD | Check the [Unity 6.6 system requirements](https://docs.unity.com/en-us/engine/6000.6/manual/get-started/install-and-upgrade/getting-started-installing-unity/system-requirements) |
| Galaxy Z Flip 3 | | Snapdragon 888 | Adreno 660 | 8 GB | UFS | |
| iPhone 11 | | A13 | | 4 GB | | |
| Steam Deck | SteamOS | | | 16 GB | | |
| Pixel Fold 10 | | | | | | |
| iPad (9th generation) | | A13 | | 3 GB | | |
| Mac mini (M1) | | Apple M1 | | | SSD | |
| Surface Book | | | integrated / discrete (base attached?) | | SSD | |

## What the app records

Every build records these in-app milestones, in milliseconds from the first managed startup callback ("elapsed
zero"): `engineReady`, `firstSceneLoaded`, `firstFrameDrawn`, `mainMenuInteractive`, `startGameChosen`,
`playSceneLoaded`, `tablePlayable`, `firstCardAction`.

The app writes two log lines prefixed `[CgsStartup]`:

- **Launch summary**: when the Main Menu becomes interactive, or after 30 s.
- **Play summary**: after the session's first card action succeeds or fails, or 60 s after Start Game.
  `tablePlayable` counts only when that action succeeded; otherwise it appears as `diagnostics.tableReadyMs`.

Each summary carries the clock anchors that are exact on that platform:

- `zeroMonotonicNs`: elapsed zero on the OS monotonic clock, used by the desktop harness.
- `processStartOffsetMs`: process start (Android) or navigation start (web) to elapsed zero.
- `launchRequestOffsetMs`: harness launch request to elapsed zero, from a valid run file.

Anything that cannot be converted exactly is `null`. Android also calls `reportFullyDrawn()` when the Main Menu
becomes interactive. Resuming from the background logs a `resume` line with its monotonic time.

## Benchmark mode

Benchmark mode presses through the title screen (until it is removed), waits for the Main Menu, and declines a
first-launch welcome prompt. It then chooses Start Game for Standard Playing Cards, accepts its start decks, and
confirms the starting-hand dealer, which is the first card action. Finally it delivers a JSON result.

**Saved data:**
- It never saves the game selection or the welcome flag.
- It ignores deep-link game URLs.
- If the saved game is not Standard Playing Cards, the run selects it without saving and is flagged `gameSwitched`; such runs are not budget results.

**Starting it:**
- Desktop or Steam: `-cgsBenchmark`, plus `-cgsRunFile=`, `-cgsResultFile=`, `-cgsRun=`, `-cgsStartDelayMs=`, and `-cgsBenchmarkGame=` as needed.
- Mobile or web: a launch link or URL query such as `cardgamesim://main?benchmark=1&run=7&delay=0`, or `?benchmark=1` on the web page.
- A benchmark link that arrives while CGS is already running is ignored.
- Single-game web builds (`CGS_SINGLEGAME`) do not support benchmark mode.

**Markers:** benchmark mode draws markers for video, each labelled with the run number:

| Marker | Corner | Colour |
|---|---|---|
| Menu interactive | Top-left | Green |
| Start Game dispatched | Top-right | Blue |
| First card action succeeded | Bottom-left | Yellow |
| Failed | Bottom-right | Red |

Start Game is dispatched only after the frame with the menu marker has been presented.

**Results:**

| Platform | Where the result goes | Who ends the run |
|---|---|---|
| Desktop | Result file and log | The player exits with status 0 (ok) or 1 (failed) |
| Android | Logcat line `[CgsBenchmark] {...}` and `files/benchmark/run-<id>.json` | The harness |
| iOS | `os_log` line (Console or `log stream` on a Mac) and an on-screen panel: tap it to share the JSON | The tester closes the app |
| Web | `window.cgsBenchmarkResult` and a console line | The harness |

## Harnesses

The scripts are in `tools/startup-benchmark/` and need Python 3.9 or later. Each prints the median, p95, failure
count, and pass/fail per interval. Runs flagged `gameSwitched`, with a start delay, or with a run-file error are
excluded from budget statistics.

### Desktop (Lenovo laptop, Linux, Steam Deck direct)

```bash
python3 tools/startup-benchmark/desktop_benchmark.py --player "/path/to/Card Game Simulator.x86_64" \
  --build <Application.version> --runs 30 --csv lenovo-direct.csv
```

On Windows, use `py` and the `.exe`. The harness writes `~/cgs-bench/run.json` with the monotonic time taken just
before launching. `launchToMenuMs` is `launchRequestOffsetMs + mainMenuInteractive` on one clock.

### Mac mini (M1)

Unzip the `cgs-StandaloneOSX` artifact, clear its quarantine so Gatekeeper checks are not timed, then run the
desktop harness against the executable inside the app bundle:

```bash
xattr -dr com.apple.quarantine "Card Game Simulator.app"
python3 tools/startup-benchmark/desktop_benchmark.py \
  --player "Card Game Simulator.app/Contents/MacOS/Card Game Simulator" \
  --build <Application.version> --runs 30 --csv macmini-direct.csv
```

### Surface Book

Same as the Lenovo laptop, with the Windows artifact. Record whether the keyboard base is attached.

### Steam Deck through Steam

Set the launch options once in Steam > Card Game Simulator > Properties > Launch Options:

```
-cgsBenchmark -cgsRunFile=/home/deck/cgs-bench/run.json -cgsResultFile=/home/deck/cgs-bench/result.json
```

Then, from desktop mode:

```bash
python3 tools/startup-benchmark/desktop_benchmark.py --steam --build <Application.version> --runs 30 \
  --work-dir /home/deck/cgs-bench --csv deck-steam.csv
```

Clear the launch options afterwards. Report Steam-launched and direct (`--player`) results separately.

### Android (Galaxy Z Flip 3, Pixel Fold 10)

Enable USB debugging, connect over USB, install the Play beta build, and set the saved game to Standard Playing
Cards. Then run:

```bash
python3 tools/startup-benchmark/android_benchmark.py --runs 30 --csv zflip3.csv
```

With both phones connected, pick one with `--serial <id>` (from `adb devices`). For the Pixel Fold 10, keep it in
one posture for a whole run set and record it (for example `--csv pixelfold10-unfolded.csv`).

`launchToMenuMs` is the system's "Fully drawn" time, from the launch intent to `reportFullyDrawn()` at the
interactive Main Menu. Later intervals add the app's own milestones, which use the same `elapsedRealtime` clock.

### iOS and iPadOS (iPhone 11, iPad 9th generation, TestFlight)

1. Save `cardgamesim://main?benchmark=1&run=1` (one run number per launch) as links in Notes.
2. Film the screen with an external camera at 240 fps or faster. Tap a link, wait for the results panel, close CGS
   from the app switcher, and wait about 2 s. Repeat for 30 runs.
3. In the video, record the frame numbers of the tap and of each marker in `frames.csv`, with columns
   `run,tap,menu,start,action,failed`.
4. Share each run's JSON from the results panel into one folder.
5. Run:

   ```bash
   python3 tools/startup-benchmark/ios_video_benchmark.py frames.csv --fps 240 --display-hz 60 \
     --results-dir shared-json --csv iphone11.csv
   ```

Each interval adds one capture frame and one display refresh (about 20.8 ms at 240 fps and 60 Hz), so pass/fail is
conservative. Runs whose video and JSON disagree are discarded and re-run. On a local development build, an
`XCTOSSignpostMetric` test is an optional cross-check.

### Web

```bash
npx -y -p playwright playwright install chromium
npx -y -p playwright node tools/startup-benchmark/web_benchmark.mjs --url <preview-url> --runs 30 --mode first
npx -y -p playwright node tools/startup-benchmark/web_benchmark.mjs --url <preview-url> --runs 30 --mode repeat
```

Record the network type and browser version with the results.

## Minimal reference build

`tools/startup-reference/` is a one-scene, one-button Unity 6.6.8f1 project with CGS's startup-relevant player
settings:
- URP
- IL2CPP
- low managed stripping
- engine code stripping
- the splash screen off
- the same built-in modules

It records the time until the button handles a submit, using the same clocks, run file, marker, and result format, so
the same harnesses measure it. It is not built by `main.yml`; adding it to CI requires a maintainer change to
`.github`. To build it locally:

```bash
~/Unity/Hub/Editor/6000.6.8f1/Editor/Unity -batchmode -quit -projectPath tools/startup-reference \
  -executeMethod ReferenceBuild.Build -referenceTarget StandaloneLinux64 \
  -referenceOutput "$PWD/tools/startup-reference/Builds/StandaloneLinux64/Reference.x86_64"
```

On Ubuntu 26.04 and later, Unity 6.6's IL2CPP linker looks for `libxml2.so.2`, which those releases no longer ship.
Link it to the installed version in a private folder and put that folder on `LD_LIBRARY_PATH` for the Unity process:

```bash
mkdir -p /tmp/cgs-libxml2-shim
ln -sf /usr/lib/x86_64-linux-gnu/libxml2.so.16 /tmp/cgs-libxml2-shim/libxml2.so.2
LD_LIBRARY_PATH=/tmp/cgs-libxml2-shim ~/Unity/Hub/Editor/6000.6.8f1/Editor/Unity ...
```

Linking the full CGS IL2CPP player needs about 3.5 GB on top of the editor. On a machine with 8 GB of RAM,
`systemd-oomd` can stop the build and everything in the same session; run it in its own scope (for example
`systemd-run --user --scope -p MemoryHigh=5G Unity ...`) or use the PR build artifacts instead.

Other targets are `StandaloneWindows64`, `Android` (`.apk` output), `iOS` (an Xcode project), and `WebGL`. For
Android, install the APK and run `android_benchmark.py --package com.finoldigital.cgsstartupreference`.

## Reporting

For every result, report:
- device, OS, build version, and channel;
- condition: first install, repeat launch with saved data, resume, web first visit, or web repeat visit;
- run count, median, p95, and failures for each interval;
- the reference build's result for the same device and condition.

Never report a single best run.

## Local development check

A local Mono Linux player is enough to check that benchmark mode works end to end, but its times are not baselines
(shipped builds use IL2CPP). Results from the development laptop (Lenovo IdeaPad Y510P, Ubuntu 26.04, 8 GB, 2026-11-12;
30 runs each, no failures):

| Build | Launch → menu (median / p95) | Start → action | Launch → action |
|---|---|---|---|
| CGS, Mono, before any optimization | 2272 / 2352 ms | 525 / 560 ms | 2867 / 2936 ms |
| Reference build, IL2CPP | 500 / 533 ms | – | – |

## Baselines

| Device | Condition | Build | Launch → menu (median / p95) | Start → action | Launch → action | Reference launch → button | Failures |
|---|---|---|---|---|---|---|---|
| _To be recorded (task 1.7)_ | | | | | | | |
