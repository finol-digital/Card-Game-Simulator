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
| Lenovo IdeaPad Y510P (Ubuntu) | `cgs-StandaloneLinux64` | Artifacts of the PR's "Test, Build, and Deploy with GameCI" run | `desktop_benchmark.py --player` |
| Samsung Galaxy Z Flip 3 | Google Play beta track | Play Store (beta tester) | `android_benchmark.py` |
| iPhone 11 | TestFlight | TestFlight | `ios_video_benchmark.py` (video) |
| Steam Deck | Steam `prerelease` branch | Steam > Properties > Betas | `desktop_benchmark.py --steam` and `--player` |
| Google Pixel 10 Pro Fold | Google Play beta track | Play Store (beta tester) | `android_benchmark.py` (record the posture) |
| iPad (6th generation) | TestFlight | TestFlight | `ios_video_benchmark.py` (video) |
| Mac mini (M1) | `cgs-StandaloneOSX` (`Card Game Simulator.app`); optionally Steam `prerelease` | Artifacts of the PR's run; Steam > Properties > Betas | `desktop_benchmark.py --player` (optionally `--steam`) |
| Microsoft Surface Laptop Studio | `cgs-StandaloneWindows64`; optionally Steam `prerelease` | Artifacts of the PR's run; Steam > Properties > Betas | `desktop_benchmark.py --player` (optionally `--steam`) |
| Web (laptop and Z Flip 3 browsers) | Firebase `preview` channel | URL in the PR's Firebase deploy step | `web_benchmark.mjs` |

Record each device's details here before using its results:

| Device | OS | CPU | GPU | RAM | Disk | Meets Unity 6.6 player requirements? |
|---|---|---|---|---|---|---|
| Lenovo IdeaPad Y510P | Ubuntu 26.04.1 LTS, GNOME on Wayland | Intel Core i7-4700MQ (4 cores, 8 threads, 2.4–3.4 GHz) | NVIDIA GeForce GT 750M with the open-source nouveau driver (OpenGL 4.3, Mesa 26.0.8) | 8 GB (7.2 GB usable) | SSD (Kingston A400, 240 GB) | Partly. The CPU and OpenGL 4.3 meet the [Unity 6.6 system requirements](https://docs.unity.com/en-us/engine/6000.6/manual/get-started/install-and-upgrade/getting-started-installing-unity/system-requirements); Ubuntu 26.04 (supported: 22.04 and 24.04) and the nouveau driver (supported: NVIDIA's proprietary driver) are outside them |
| Galaxy Z Flip 3 (SM-F711U1) | Android 15, One UI 7.0 (AP3A.240905.015.A2.F711U1UESIKZH2) | Snapdragon 888 | Adreno 660 (OpenGL ES 3.2, Vulkan); 120 Hz display | 8 GB | UFS, 256 GB | Yes. Android 15 on ARM64 with OpenGL ES 3.2 and Vulkan meets the [Unity 6.6 system requirements](https://docs.unity.com/en-us/engine/6000.6/manual/get-started/install-and-upgrade/getting-started-installing-unity/system-requirements) |
| iPhone 11 (iPhone12,1) | iOS 27.2.1 (24B156) | Apple A13 Bionic (2 performance and 4 efficiency cores) | Apple 4-core GPU (Metal); 828 × 1792 display at 60 Hz | 4 GB | NAND flash, 64 GB | Yes. iOS on ARM64 with Metal meets the [Unity 6.6 system requirements](https://docs.unity.com/en-us/engine/6000.6/manual/get-started/install-and-upgrade/getting-started-installing-unity/system-requirements) |
| Steam Deck (LCD, "Jupiter") | SteamOS 3.8.28; Desktop Mode (KDE Plasma on Wayland) for these measurements | AMD Custom APU 0405 (Zen 2, 4 cores, 8 threads) | AMD Custom GPU 0405 (RDNA 2) with Mesa radeonsi; the player used OpenGL 4.6 | 16 GB (14 GiB usable) | SSD (NVMe, Transcend TS256GMTE352T, 256 GB) | Partly. The CPU, GPU, and AMD Mesa driver meet the [Unity 6.6 system requirements](https://docs.unity.com/en-us/engine/6000.6/manual/get-started/install-and-upgrade/getting-started-installing-unity/system-requirements); SteamOS and KDE Plasma are not listed (supported: Ubuntu 22.04 and 24.04 with GNOME) |
| Pixel 10 Pro Fold ("rango") | Android 17 (CP3A.261005.005, security patch 2026-10-05) | Google Tensor G5 | PowerVR D-Series DXT-48-1536 (OpenGL ES 3.2, Vulkan); inner display 2076 × 2152 at 120 Hz | 16 GB | UFS, 256 GB | Yes. Android on ARM64 with OpenGL ES 3.2 and Vulkan meets the [Unity 6.6 system requirements](https://docs.unity.com/en-us/engine/6000.6/manual/get-started/install-and-upgrade/getting-started-installing-unity/system-requirements) |
| iPad (6th generation) (iPad7,5) | iPadOS 17.7.11 (21H461) | Apple A10 Fusion (2 performance and 2 efficiency cores) | Apple A10 GPU (Metal); 1536 × 2048 display at 60 Hz | 2 GB | NAND flash, 32 GB | Yes. iPadOS on ARM64 with Metal meets the [Unity 6.6 system requirements](https://docs.unity.com/en-us/engine/6000.6/manual/get-started/install-and-upgrade/getting-started-installing-unity/system-requirements) |
| Mac mini (M1, Macmini9,1) | macOS 27.0 (26A428) | Apple M1 (4 performance and 4 efficiency cores) | Apple M1, 8 GPU cores (Metal 4); 1920 × 1080 display at 100 Hz | 8 GB | SSD (Apple AP0256Q, 256 GB) | Yes. Apple silicon with Metal meets the [Unity 6.6 system requirements](https://docs.unity.com/en-us/engine/6000.6/manual/get-started/install-and-upgrade/getting-started-installing-unity/system-requirements) |
| Surface Laptop Studio | Windows 11 Home (10.0.26300) | Intel Core i7-11370H (4 cores, 8 threads, 3.3–4.8 GHz) | Intel Iris Xe and NVIDIA GeForce RTX 3050 Ti Laptop GPU (switchable; the player used the NVIDIA GPU, per the `Renderer` line in its `Player.log`) | 16 GB | SSD (Samsung MZ9LQ512HALU, 512 GB) | Yes. Windows 11 x64 and DirectX 12-capable GPUs meet the [Unity 6.6 system requirements](https://docs.unity.com/en-us/engine/6000.6/manual/get-started/install-and-upgrade/getting-started-installing-unity/system-requirements) |

## What the app records

Every build records these in-app milestones, in milliseconds from the first managed startup callback ("elapsed
zero"): `engineReady`, `firstSceneLoaded`, `firstFrameDrawn`, `localizationReady`, `mainMenuInteractive`,
`startGameChosen`, `playSceneLoaded`, `tablePlayable`, `firstCardAction`.

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

The launch summary's `localization` field says whether the startup language was `ready`, had `failed`, or was still
`pending` when the summary was written. Because the language can arrive after that, a separate `localization`
line records its outcome and `elapsedMs` when it does. `localizationReady` is `null` when the language failed or was
still loading.

## Startup language

The first Main Menu after launch shows the startup language (from `-language=`, the saved choice, or the device
language) without showing English first:

- If the startup language is English, the menu shows at once, because it is authored in English.
- Otherwise the menu stays hidden, and ignores input, until the language's text and fonts are ready, then shows on
  the next frame.
- The wait is at most 500 ms from the menu's first frame (`MainMenu.DefaultStartupLanguageWaitSeconds`). After
  that, the menu shows in readable English and is usable, logs the warning `MainMenu: the startup language was not
  ready after 500 ms`, and switches to the language in place when it arrives.
- If localization fails, the menu shows in English straight away, and CGS logs `CGS localization initialization
  failed`.

Measured on the Lenovo IdeaPad Y510P (2026-11-23, local Mono build, `-batchmode` because the screen was locked, 5
launches per language), the language was ready this long after the menu's first frame:

| Language | Median | Range |
|---|---|---|
| English | 272 ms | 269–357 ms |
| Japanese | 243 ms | 242–260 ms |
| Korean | 308 ms | 308–323 ms |
| Simplified Chinese | 310 ms | 298–323 ms |

The bound is about 1.5 times the slowest of these, so a normal non-English launch on the slowest reference desktop
waits for its language rather than flashing English. With the wait in place (same laptop and method), the menu
became interactive a median of 281 ms (Japanese) to 374 ms (Korean, Simplified Chinese) after its first frame,
shown in the startup language, and no launch reached the bound. English launches do not wait. Most of the wait is
localization starting up rather than the CJK fonts, since English takes nearly as long to load. Re-check the
bound against the reference devices when their baselines are next measured.

## Benchmark mode

Benchmark mode waits for the Main Menu, which CGS opens directly, and declines a first-launch welcome
prompt. It then chooses Start Game for Standard Playing Cards, accepts its start decks, and
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
| iOS | `os_log` line (Console or `log stream` on a Mac), `Documents/benchmark/run-<id>.json` in the app's container, and an on-screen panel: tap it to share the JSON | The tester or the harness closes the app |
| Web | `window.cgsBenchmarkResult` and a console line | The harness |

## Harnesses

The scripts are in `tools/startup-benchmark/` and need Python 3.9 or later (3.10 or later on Windows). Each prints
the median, p95, failure count, and pass/fail per interval. Runs flagged `gameSwitched`, with a start delay, or with
a run-file error are excluded from budget statistics.

### Desktop (Lenovo IdeaPad Y510P, Linux, Steam Deck direct)

```bash
python3 tools/startup-benchmark/desktop_benchmark.py --player "/path/to/Card Game Simulator.x86_64" \
  --build <Application.version> --runs 30 --csv lenovo-direct.csv
```

On Windows, use `py` and the `.exe`. The harness writes `~/cgs-bench/run.json` with the monotonic time taken just
before launching. `launchToMenuMs` is `launchRequestOffsetMs + mainMenuInteractive` on one clock.

For the first-install condition, run `first_install.py` with the same arguments (Windows and Linux):

```bash
python3 tools/startup-benchmark/first_install.py --player "/path/to/Card Game Simulator.x86_64" \
  --build <Application.version> --runs 30 --csv lenovo-first-install.csv
```

Every run starts with no CGS data or settings. On Linux each run gets an empty `XDG_CONFIG_HOME`, so your own data
is not touched. On Windows your CGS data folder (`AppData\LocalLow`), cache folder (`%TEMP%`), and registry settings
are set aside before the first run and put back when the runs end, even after a failure or Ctrl+C; the settings are
also exported to `~/cgs-bench/cgs-settings-backup.reg`. If the script is stopped before it can put them back (for
example, by a power loss), run it again with `--restore`.

### Mac mini (M1)

Unzip the `cgs-StandaloneOSX` artifact, clear its quarantine so Gatekeeper checks are not timed, then run the
desktop harness against the executable inside the app bundle:

```bash
xattr -dr com.apple.quarantine "Card Game Simulator.app"
C=~/Library/Containers/com.finoldigital.CardGameSimulator/Data
python3 tools/startup-benchmark/desktop_benchmark.py \
  --player "Card Game Simulator.app/Contents/MacOS/Card Game Simulator" \
  --build <Application.version> --runs 30 --work-dir "$C/cgs-bench" --csv macmini-direct.csv
```

This build is sandboxed and signed for the Mac App Store, so it can only read and write inside its container: pass
a `--work-dir` there, as above. It also shares that container, including the saved game and settings, with an
installed App Store copy of CGS. Before changing the saved game, back up the settings with
`defaults export "$C/Library/Preferences/com.finoldigital.CardGameSimulator.plist" <file>`, and `defaults import`
them afterwards. `first_install.py` takes the same `--work-dir` and sets the container's data and settings aside
for its runs.

### Surface Laptop Studio

Same as the Lenovo IdeaPad, with the Windows artifact. Run it plugged in, in the Best performance power mode, after
the laptop has cooled, because it throttles otherwise. Record which GPU the player used (the `Renderer` line in
`%USERPROFILE%\AppData\LocalLow\Finol Digital LLC\Card Game Simulator\Player.log`).

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

### Android (Galaxy Z Flip 3, Pixel 10 Pro Fold)

Enable USB debugging, connect over USB, install the Play beta build, and set the saved game to Standard Playing
Cards. Then run:

```bash
python3 tools/startup-benchmark/android_benchmark.py --runs 30 --csv zflip3.csv
```

For the first-install condition, add `--clear-data`: it clears CGS's app data (`pm clear`) before every run. Play
builds cannot be backed up first, so this permanently deletes CGS's games, decks, and settings on that device; run it
last, and only on a device whose CGS data can be lost.

With both phones connected, pick one with `--serial <id>` (from `adb devices`). For the Pixel 10 Pro Fold, keep it in
one posture for a whole run set and record it (for example `--csv pixel10fold-unfolded.csv`).

`launchToMenuMs` is the system's "Fully drawn" time, from the launch intent to `reportFullyDrawn()` at the
interactive Main Menu. Later intervals add the app's own milestones, which use the same `elapsedRealtime` clock.

### iOS and iPadOS (iPhone 11, iPad 6th generation, TestFlight)

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

The iPhone 11 and iPad baselines were recorded without touching the device, from a Mac with Xcode:

- **Launch:** with the device trusted and in Developer Mode, close CGS and wait 3 s on the Home Screen. Then run
  `xcrun devicectl device process launch --device <udid> --payload-url "cardgamesim://main?benchmark=1&run=<id>"
  com.finoldigital.CardGameSim`.
  - Find the pid to close with `xcrun devicectl device info processes`, then close it with
    `xcrun devicectl device process terminate --pid <pid>`.
  - After the set, copy the results with `xcrun devicectl device copy from --domain-type appDataContainer
    --domain-identifier com.finoldigital.CardGameSim --source Documents/benchmark`.
- **Filming:** a Pixel 10 Pro Fold filmed the screen in Slow Motion at 1/8× (240 frames per second), started and
  stopped over `adb` with the volume key.
  - Fix the exposure at about 30% with the iPhone at 30% brightness, or yellow overexposes to white.
  - Turn on Auto-Lock Never and Do Not Disturb.
  - Use a Home Screen page without animated widgets.
  - Power the camera phone from a fast charger: 240 fps drains it faster than a 500 mA port charges it.
- **Reading the video:** the marker frames come from colour tracking.
  - Launch is the first frame where the screen starts to change, which is the start of the app-opening animation.
    This stands in for the player's tap, which iOS answers with that animation within a display refresh or two. The
    time of the `devicectl` request is not used, because it adds USB and CoreDevice delays that a tap does not have.
    The measured times can therefore be shorter than tap-to-menu times by those one or two refreshes (about
    17–33 ms at 60 Hz).
  - Compare each frame with the one two frames earlier. This cancels the 120 Hz flicker of mains lighting.
  - The launch threshold comes from a quiet stretch of the Home Screen after the previous run closes. On the iPad the
    app-closing animation lasts longer, so start that stretch 2 s after the previous run's markers disappear rather
    than 1 s.
  - Locate the blue and yellow markers from the green one's position. Tracked on their own, they can slide onto other
    yellow or blue content on screen.
- **The reference build:** launch it by bundle ID (`com.finoldigital.CgsStartupReference`). Unity on iOS does not pass
  launch arguments to the app, so it always writes `run-0.json`: copy that file after each run.
- **Xcode's debugger:** while Xcode's debugger is attached after a Run, `devicectl` cannot close the app. Press Stop
  in Xcode first.
- **First install:** `devicectl` cannot clear an app's data, so first install needs deleting and reinstalling from
  TestFlight by hand.

### Web

```bash
npm install --no-save --prefix tools/startup-benchmark playwright
tools/startup-benchmark/node_modules/.bin/playwright install chromium
node tools/startup-benchmark/web_benchmark.mjs --url <preview-url> --runs 30 --mode first --headless false
node tools/startup-benchmark/web_benchmark.mjs --url <preview-url> --runs 30 --mode repeat --headless false
```

The preview URL is in the log of the PR run's "Deploy to the Web via Firebase Test" job, and the channel expires 7
days after that deploy. Record the network type and browser version with the results.

- `--headless false` opens a visible browser window, which uses the GPU; headless Chromium renders WebGL in software.
- `--mode repeat` keeps its warm-up page open for `--warmup-ms` (default 60000) so that the first visit's game
  downloads finish and are saved before the measured reloads.
- `--angle vulkan` switches Chromium to its Vulkan backend, for GPUs whose OpenGL driver loses the WebGL context.
- `--console-log <file>` appends each run's browser console to a file, for diagnosing failed runs.
- `--pause-ms <ms>` leaves a blank page between runs so that a phone can cool down.
- `--cdp <url>` drives a phone's Chrome instead of a local browser. Turn on wireless or USB debugging, then run
  `adb forward tcp:9333 localabstract:chrome_devtools_remote` and pass `--cdp http://127.0.0.1:9333`. Each first
  visit uses a new incognito context; a repeat-visit set uses one incognito context for all its runs and closes it at
  the end, so the phone's normal profile is never changed. The Z Flip 3 web baselines below predate this: their repeat
  visits used a normal tab, and the harness then cleared the site's data from the phone's profile.

On a first visit the Main Menu accepts input while the default games are still downloading, so build 1.171.24
reports the play phase as failed (`benchmarkGameUnavailable`); use only `launchToMenuMs` from those runs.

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

Windows reference builds must be built on Windows. Install Unity 6000.6.8f1 with the "Windows Build Support
(IL2CPP)" module (open `unityhub://6000.6.8f1/237b6593792b` with Unity Hub, signed in with an activated license)
and the Visual Studio C++ build tools (`winget install Microsoft.VisualStudio.2022.BuildTools --override "--quiet
--wait --add Microsoft.VisualStudio.Workload.VCTools --includeRecommended"`), then build from PowerShell:

```powershell
$reference = "$PWD\tools\startup-reference"
$unity = Start-Process -PassThru "C:\Program Files\Unity\Hub\Editor\6000.6.8f1\Editor\Unity.exe" `
  -ArgumentList "-batchmode -quit -projectPath `"$reference`" -executeMethod ReferenceBuild.Build",
  "-referenceTarget StandaloneWindows64 -referenceOutput `"$reference\Builds\StandaloneWindows64\Reference.exe`"",
  "-logFile `"$reference\Logs\build.log`""
$null = $unity.Handle  # keeps the exit code readable
$unity.WaitForExit()
$unity.ExitCode  # 0 when the build worked; otherwise see Logs\build.log
```

Run it from the repository root. The first build takes several minutes while Unity imports the project.

Other targets are `StandaloneWindows64`, `Android` (`.apk` output), `iOS` (an Xcode project), and `WebGL`. For
Android, install the APK and run `android_benchmark.py --package com.finoldigital.cgsstartupreference`.
On desktop, pass `--build 6000.6.8` (the reference build's version). On iOS, launch it from its icon, record only
the tap and menu frames, and run `ios_video_benchmark.py --reference`.

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
| CGS, Mono, opens directly on the Main Menu (2026-11-14) | 1757 / 1824 ms | 569 / 592 ms | 2720 / 2807 ms |
| Reference build, IL2CPP | 500 / 533 ms | – | – |

Opening directly on the Main Menu removed the title screen's scene load and the Main Menu's 100 ms input delay:
launch → menu fell by about 515 ms at the median, and the first managed callback → menu interval fell from about
1608 ms to 1104 ms. Start → action is unaffected by this change.

## Baselines

| Device | Condition | Build | Launch → menu (median / p95) | Start → action | Launch → action | Reference launch → button | Failures |
|---|---|---|---|---|---|---|---|
| Lenovo IdeaPad Y510P | Repeat launch, saved data | 1.171.24 (`cgs-StandaloneLinux64`, commit `37fc9fe6`) | 1119 / 1176 ms | 188 / 202 ms | 1371 / 1444 ms | 498 / 560 ms | 0 of 30 |
| Lenovo IdeaPad Y510P | First install | 1.171.24 (`cgs-StandaloneLinux64`, commit `37fc9fe6`) | 5905 / 5955 ms | 3014 / 3017 ms | 10919 / 10967 ms | 498 / 560 ms | 0 of 30 |
| Surface Laptop Studio | Repeat launch, saved data | 1.171.24 (`cgs-StandaloneWindows64`, commit `37fc9fe6`) | 1428 / 3395 ms | 123 / 211 ms | 1626 / 5342 ms | 785 / 826 ms | 0 of 30 |
| Surface Laptop Studio | First install | 1.171.24 (`cgs-StandaloneWindows64`, commit `37fc9fe6`) | 3932 / 4313 ms | 261 / 320 ms | 4396 / 4725 ms | 785 / 826 ms | 0 of 30 |
| Steam Deck (LCD) | Repeat launch, saved data, direct | 1.171.24 (Steam `prerelease`, Steam build 26661549) | 1782 / 1903 ms | 212 / 235 ms | 2103 / 2273 ms | 1146 / 1199 ms | 0 of 30 |
| Steam Deck (LCD) | First install, direct | 1.171.24 (Steam `prerelease`, Steam build 26661549) | 1841 / 1924 ms | 214 / 232 ms | 2160 / 2237 ms | 1146 / 1199 ms | 0 of 30 |
| Steam Deck (LCD) | Repeat launch, saved data, through Steam | 1.171.24 (Steam `prerelease`, Steam build 26661549) | 6515 / 8071 ms | 224 / 259 ms | 6794 / 8351 ms | 1146 / 1199 ms (direct) | 0 of 30 |
| Galaxy Z Flip 3 | Repeat launch, saved data (process stopped) | 1.171.24 (Google Play beta, version code 1171024) | 1655 / 2315 ms | 229 / 249 ms | 1951 / 2625 ms | 619 / 678 ms | 0 of 30 |
| Galaxy Z Flip 3 | First install (app data cleared) | 1.171.24 (Google Play beta, version code 1171024) | 1832 / 2045 ms | 225 / 239 ms | 2129 / 2403 ms | 619 / 678 ms | 0 of 30 |
| Pixel 10 Pro Fold | Repeat launch, saved data (process stopped), unfolded | 1.171.24 (Google Play beta, version code 1171024) | 1327 / 1462 ms | 194 / 218 ms | 1590 / 1759 ms | 836 / 950 ms | 0 of 30 |
| Pixel 10 Pro Fold | First install (app data cleared), unfolded | 1.171.24 (Google Play beta, version code 1171024) | 1380 / 1415 ms | 202 / 219 ms | 1651 / 1686 ms | 836 / 950 ms | 0 of 30 |
| iPhone 11 | Repeat launch, saved data (app closed) | 1.171.24 (TestFlight) | 1296 / 1379 ms | 200 / 204 ms | 1506 / 1604 ms | 421 / 646 ms | 0 of 30 |
| iPhone 11 | First install | 1.171.24 (TestFlight) | Not measured | Not measured | Not measured | 421 / 646 ms | – |
| iPad (6th generation) | Repeat launch, saved data (app closed) | 1.171.24 (TestFlight) | 2075 / 3125 ms | 298 / 321 ms | 2396 / 3546 ms | 623 / 1333 ms | 0 of 30 |
| iPad (6th generation) | First install | 1.171.24 (TestFlight) | Not measured | Not measured | Not measured | 623 / 1333 ms | – |
| Mac mini (M1) | Repeat launch, saved data | 1.171.24 (`cgs-StandaloneOSX`, commit `37fc9fe6`) | 1007 / 1275 ms | 93 / 122 ms | 1542 / 1857 ms | 543 / 625 ms | 0 of 30 |
| Mac mini (M1) | First install | 1.171.24 (`cgs-StandaloneOSX`, commit `37fc9fe6`) | 1213 / 1379 ms | 103 / 120 ms | 1778 / 1953 ms | 543 / 625 ms | 0 of 30 |
| Web, Lenovo IdeaPad Y510P (Chromium 159) | Repeat visit, cached | 1.171.24 (Firebase `preview` channel, commit `37fc9fe6`) | 2180 / 2308 ms | 207 / 229 ms | 2428 / 2530 ms | Not built | 0 of 30 |
| Web, Lenovo IdeaPad Y510P (Chromium 159) | First visit, empty caches | 1.171.24 (Firebase `preview` channel, commit `37fc9fe6`) | 6953 / 7249 ms (20 runs) | Not measurable in this build | Not measurable in this build | Not built | 10 of 30 (WebGL context lost) |
| Web, Galaxy Z Flip 3 (Chrome 157) | Repeat visit, cached | 1.171.24 (Firebase `preview` channel, commit `37fc9fe6`) | 2990 / 3692 ms | 308 / 381 ms | 3355 / 4166 ms | Not built | 0 of 30 |
| Web, Galaxy Z Flip 3 (Chrome 157) | First visit, empty caches (incognito) | 1.171.24 (Firebase `preview` channel, commit `37fc9fe6`) | 4682 / 5000 ms | Not measurable in this build | Not measurable in this build | Not built | 0 of 30 (launch) |

Notes:
- **Lenovo IdeaPad Y510P** (2026-11-14, direct launch, 30 runs per row). The reference build alone misses the 500 ms
  launch budget at p95 on this laptop (560 ms), so its launch budget needs an exception stated relative to the
  reference build. First-install runs used `first_install.py`'s method (an empty `XDG_CONFIG_HOME` per run), so CGS
  had no saved data. On first install, the Main Menu becomes interactive about 5.5 s after the first managed
  callback, because the bundled games are copied before it accepts input; after Start Game, the Play screen takes
  about 2.1 s to load and the first card action follows about 0.9 s after the table is ready.
- **Surface Laptop Studio** (2026-11-15, direct launch, 30 runs, plugged in, Best performance power mode, NVIDIA
  GPU). 28 of the 30 runs reached the Main Menu in 1327–1510 ms; the p95 values come from two slow runs: the first
  launch (3395 ms to the menu) and run 29 (8804 ms, almost all of it before the Main Menu's first frame), which looks
  like a background Windows task. An earlier 30-run attempt on the default power mode slowed by about 1 s per phase
  after its sixth run (launch → menu median 2738 ms), so run this laptop plugged in, in Best performance, after it
  has cooled. The reference build alone misses the 500 ms launch budget here too (826 ms at p95; most of it before
  the first managed callback), so this laptop also needs a launch-budget exception relative to the reference build.
  First-install runs used `first_install.py`; the Main Menu becomes interactive about 2.5 s after the first managed
  callback while the bundled games are copied. Unlike the Lenovo IdeaPad, start → action on first install matched
  repeat launches (about 260 ms). The first run of each set was a cold outlier (first install 10431 ms to the menu,
  reference 4964 ms, right after the build); the nearest-rank p95 of 30 runs excludes one such run.
- **Steam Deck (LCD)** (2026-11-15, Desktop Mode, 30 runs per row, on battery at 98%). Launched directly from the
  Steam installation, over SSH with `DISPLAY=:0 WAYLAND_DISPLAY=wayland-0 XDG_RUNTIME_DIR=/run/user/1000`. The
  reference build alone misses the 500 ms launch budget (1199 ms at p95), so the Deck also needs a launch-budget
  exception relative to the reference build. First install added only about 60 ms here: the fresh data folder was
  confirmed to receive the bundled games (13 MB), which this device copies quickly, unlike the Lenovo IdeaPad (about
  5.5 s). Direct launches change Unity's `Screenmanager Resolution Use Native` preference, so back up
  `~/.config/unity3d/Finol Digital LLC/Card Game Simulator/prefs` before them and restore it afterwards.
  Through Steam (plugged in), launch → menu was 5367–8560 ms, about 4.7 s more than direct at the median, while the
  first managed callback → menu interval matched direct launches (933 ms median). Steam's `console-linux.txt` shows
  where the extra time goes, to 1 s resolution: about 3–4 s in the Steam client before it starts the game (session
  start, the Steam Cloud check, and Steam Input setup), then about 2 s for the Steam Linux Runtime container
  (`_v2-entry-point`, `capsule-capture-libs`) before Unity starts. Steam Cloud's launch scan itself took under 1 s,
  although it watches 1183 files (110 MB) in the legacy `~/.config/unity3d/FinolDigital/CardGameSimulator` folder
  rather than the current `Finol Digital LLC/Card Game Simulator` folder.
- **Galaxy Z Flip 3** (2026-11-16, unfolded, 120 Hz, on battery, over wireless debugging, 30 runs per row). Launch →
  menu is Android's "Fully drawn" time. An earlier repeat set on a wireless charger reached thermal status 3
  (severe; skin 44 °C) and slowed by about 170 ms halfway through; it was discarded. Even off the charger, 30
  back-to-back launches kept the phone at thermal status 1–3, but launch times stayed flat, so let it cool to status
  0 or 1 (`adb shell dumpsys thermalservice`) before each set and run it off the charger. In the repeat row, the p95
  comes from the first two launches (3335 and 2315 ms); the other 28 took 1601–1738 ms. First install (`--clear-data`)
  added about 180 ms at the median. The reference build (installed from the Surface, built with Unity 6000.6.8f1)
  took 582–687 ms to draw, so this phone also needs a launch-budget exception relative to the reference build. CGS
  adds about 1040 ms to it at the median, more than the 620–640 ms it adds on the desktops.
- **Pixel 10 Pro Fold** (2026-11-16, unfolded on the inner display at 120 Hz, on battery at 22–19%, over wireless
  debugging, 30 runs per row). The phone stayed at thermal status 0 throughout. Battery Saver turns on by itself at 20%
  and slowed launches by about 300 ms from that run on, so an earlier first-install set was discarded and re-run with
  Battery Saver turned off (`adb shell cmd power set-mode 0`, and `1` afterwards to put it back). Check
  `adb shell settings get global low_power` before each set, or charge the phone first and let it cool. In the
  repeat row the p95 comes from the first two launches (1533 and 1462 ms); the other 28 took 1295–1410 ms. First
  install added only about 50 ms at the median. The reference build (installed from the Surface) took 718–965 ms to
  draw, slower than on the Z Flip 3 even though CGS is faster here, so CGS adds only about 490 ms to it at the median.
  The reference set also stepped down after its 19th run (824–965 ms before, 718–789 ms after), which may be
  background optimization of the newly installed APK; wait a few minutes after installing before measuring.
- **iPhone 11** (2026-11-18, plugged into the Mac, 30% brightness, 30 runs per row, filmed at 240 fps; see
  [iOS and iPadOS](#ios-and-ipados-iphone-11-ipad-6th-generation-testflight)).
  - **Video times:** the times come from the video and include the 20.8 ms uncertainty margin. Every run's JSON
    reported `ok`, and none switched games.
  - **CGS on iOS draws at 30 fps,** so the screen lags the app's own timestamps by up to a few frames:
    - Start → action is about 180 ms on video, against about 112 ms in the JSON.
    - The Start button's marker appears 30–50 ms after the menu's on video, against 100–110 ms in the JSON.
    - Launch → menu agrees with the JSON: the menu appears 850–920 ms after the engine starts, and the rest is the
      system's app launch.
  - **Reference build:** it took 400–571 ms in 28 runs. The two slow runs were the first launches after a pause:
    646 ms on the first launch after installing, and 1229 ms after an hour without launching it, almost all of it
    before Unity started.

    Like every other native device, the iPhone 11 needs a launch-budget exception relative to the reference build.
    CGS adds about 875 ms to it at the median.
  - **First install was not measured.** It needs the app deleted and reinstalled from TestFlight before each run,
    because `devicectl` cannot clear app data. It is deferred to task 6.3, which sets the first-install budgets.
- **iPad (6th generation)** (2026-11-19, plugged into the Mac, 30% brightness, landscape, 30 runs per row, filmed at
  240 fps like the iPhone 11). The iPad (9th generation) originally listed was not available, so this older iPad,
  with an A10 Fusion and 2 GB of RAM, is the iPad reference device.
  - **CGS:** every run's JSON reported `ok`, and none switched games. 28 runs reached the menu in 1967–2504 ms. The
    two slow ones were the first launch of the set (3575 ms) and the first launch after a 6-minute pause (3125 ms).
    Each run was given 8 s before it was closed, rather than 6 s, because the first launch reached the card action
    about 4.2 s after launch.
  - **Reference build:** 24 runs took 592–688 ms. The first launch after installing took 2317 ms, and launches
    after a pause of a few minutes took 850–1333 ms.

    The iPad needs a launch-budget exception relative to the reference build. CGS adds about 1450 ms to it at the
    median, the most of any native device.
  - **First install was not measured,** for the same reason as on the iPhone 11.
- **Mac mini (M1)** (2026-11-16, on mains power, 30 runs per row, driven over SSH while the user stayed logged
  in). The `cgs-StandaloneOSX` build is sandboxed and shares its container with the installed App Store CGS, so the
  saved game was switched to Standard Playing Cards for these runs and the container's settings were restored
  afterwards. First install added about 200 ms. The reference build took 516–625 ms after its first launch (2404 ms),
  so this Mac also needs a launch-budget exception relative to it, by a small margin. CGS adds about 460 ms to it
  at the median, the least of any device. Wait for Spotlight and other background indexing to finish after copying
  or building an app: right after the reference build, the load average reached 24.
- **Web on the Lenovo IdeaPad Y510P** (2026-11-16, Playwright's Chromium 159.0.8154.6 in a visible window,
  Wi-Fi at 2.4 GHz, 30 runs per row). Chromium drew WebGL on the GeForce GT 750M through ANGLE on OpenGL (Mesa
  nouveau). Times run from navigation start, so they include downloading the build. Launch → menu is about 1 s
  longer than the native Linux player for repeat visits; a first visit spends about 6 s downloading and starting the
  build before the first managed callback. On a first visit the menu accepts input while the three default games
  are still downloading (about 6 s more), so build 1.171.24 cannot start the benchmark game and reports
  `benchmarkGameUnavailable`; start → action needs the benchmark to wait for that download. The nouveau driver
  rejects some of CGS's GPU commands (kernel `ZETA_STORAGE_TYPE_MISMATCH` traps), and in about a third of first
  visits Chromium lost the WebGL context before the menu. In one earlier first-visit attempt the driver shut down
  Chromium's GPU channel and every later run hung, so first-visit mode now starts a new browser for each run.
  Earlier repeat-visit attempts also lost the context or hung, including on `--angle vulkan`, before this clean set.
  The reference build has no WebGL build yet (this laptop's Unity 6.6.8 has no WebGL module and too little free disk
  space for it).
- **Web on the Galaxy Z Flip 3** (2026-11-16, Chrome 157.0.8103.78, unfolded, on battery at 63–54%, Wi-Fi, 30 runs
  per row, driven from the laptop with `--cdp` over wireless debugging). Each first visit used a new incognito
  context, so Chrome's GPU shader cache was not emptied between first visits. A CGS page keeps rendering, so
  back-to-back runs heated the phone to thermal status 3 within 2 minutes and slowed launches from about 2.1 s to
  7 s; those sets were discarded and re-run with `--pause-ms 15000`, which kept it at status 0 throughout. Repeat
  visits reach the menu about 1.3 s later than the installed Android app. The build ships DXT-compressed textures,
  which this phone's GPU cannot sample, so every load logs `decompressing texture` and pays to decompress them; an
  ASTC or ETC2 variant for mobile browsers would avoid that. In the discarded repeat set, the saved games vanished
  from browser storage after the 24th reload (`Could not find file …/Dominoes.json`), and CGS downloaded them again.
