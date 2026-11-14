# This Source Code Form is subject to the terms of the Mozilla Public
# License, v. 2.0. If a copy of the MPL was not distributed with this
# file, You can obtain one at http://mozilla.org/MPL/2.0/.
"""Android launch-action benchmark for CGS over adb (for example, the Galaxy Z Flip 3 with the Play beta build).

Each run force-stops CGS, launches it with a benchmark deep link through `am start -W`, and reads two lines from
logcat: the system's "Fully drawn" time (the app calls reportFullyDrawn() when the Main Menu becomes interactive,
so this is launch intent -> interactive Main Menu on the system clock), and the [CgsBenchmark] result. Later
intervals use the app's elapsedRealtime-consistent milestones measured from that same interactive point.

  python3 android_benchmark.py --runs 30 --csv zflip3.csv
"""

from __future__ import annotations

import argparse
import os
import queue
import re
import subprocess
import sys
import threading
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import startup_stats as stats  # noqa: E402

PACKAGE = "com.finoldigital.cardgamesim"
REFERENCE_ACTIVITY = "com.unity3d.player.UnityPlayerActivity"


def fully_drawn_pattern(package: str):
    return re.compile(r"Fully drawn " + re.escape(package) + r"/\S+: \+(?:(\d+)s)?(\d+)ms")
RESULT_TAG = "[CgsBenchmark] "


def adb(args, *command, check=True, capture=True):
    full = ["adb", *(["-s", args.serial] if args.serial else []), *command]
    return subprocess.run(full, check=check, text=True, capture_output=capture)


def run_once(args, run_index: int) -> stats.RunRecord:
    run_id = str(run_index)
    package = args.package
    adb(args, "shell", "am", "force-stop", package)
    adb(args, "logcat", "-c")
    time.sleep(args.settle)
    link = f"cardgamesim://main?benchmark=1&run={run_id}&delay={args.start_delay_ms:g}"
    if package == PACKAGE:
        start = adb(args, "shell", "am", "start", "-W", "-a", "android.intent.action.VIEW", "-d", f"'{link}'",
                    package, check=False)
    else:
        # The minimal reference app always benchmarks and has no deep links
        start = adb(args, "shell", "am", "start", "-W", "-n", f"{package}/{REFERENCE_ACTIVITY}", check=False)
    fully_drawn = fully_drawn_pattern(package)
    total_time = re.search(r"TotalTime: (\d+)", start.stdout or "")

    fully_drawn_ms = None
    result = None
    deadline = time.monotonic() + args.timeout
    logcat = subprocess.Popen(["adb", *(["-s", args.serial] if args.serial else []), "logcat", "-v", "brief"],
                              stdout=subprocess.PIPE, stderr=subprocess.DEVNULL, text=True)
    # Read logcat on a thread so a quiet log cannot block the deadline check
    lines: queue.Queue = queue.Queue()
    threading.Thread(target=lambda: [lines.put(line) for line in logcat.stdout], daemon=True).start()
    try:
        while result is None:
            remaining = deadline - time.monotonic()
            if remaining <= 0:
                break
            try:
                line = lines.get(timeout=min(remaining, 0.5))
            except queue.Empty:
                continue
            match = fully_drawn.search(line)
            if match:
                fully_drawn_ms = int(match.group(1) or 0) * 1000 + int(match.group(2))
            index = line.find(RESULT_TAG)
            if index >= 0:
                result = stats.parse_result(line[index + len(RESULT_TAG):])
    finally:
        logcat.kill()
        adb(args, "shell", "am", "force-stop", package, check=False)

    if result is None:
        return stats.RunRecord(run_id, False, "noResult",
                               {"launchToFirstFrameMs": float(total_time.group(1)) if total_time else None})

    launch, play = result.get("launch"), result.get("play")
    menu = stats.milestone(launch, "mainMenuInteractive")
    start_game = stats.milestone(play, "startGameChosen")
    action = stats.milestone(play, "firstCardAction")
    flags = stats.result_flags(result)
    if fully_drawn_ms is None:
        flags.append("noFullyDrawn")
    intervals = {
        "launchToFirstFrameMs": float(total_time.group(1)) if total_time else None,
        "launchToMenuMs": fully_drawn_ms,
        "launchToActionMs": None if fully_drawn_ms is None or menu is None or action is None
        else fully_drawn_ms + (action - menu),
        "startToActionMs": None if start_game is None or action is None else action - start_game,
        "processStartToMenuMs": None if launch is None or launch.get("processStartOffsetMs") is None
        or menu is None else launch["processStartOffsetMs"] + menu,
    }
    ok = result.get("outcome") == "ok"
    return stats.RunRecord(run_id, ok, None if ok else result.get("failedMilestone") or "failed", intervals, flags)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--serial", help="adb device serial when more than one device is connected")
    parser.add_argument("--package", default=PACKAGE,
                        help="Package to measure; use com.finoldigital.cgsstartupreference for the reference build")
    parser.add_argument("--runs", type=int, default=stats.MIN_BUDGET_RUNS)
    parser.add_argument("--timeout", type=float, default=120.0)
    parser.add_argument("--settle", type=float, default=2.0, help="Seconds to idle after force-stop")
    parser.add_argument("--start-delay-ms", type=float, default=0)
    parser.add_argument("--csv")
    args = parser.parse_args()

    records = []
    for index in range(1, args.runs + 1):
        record = run_once(args, index)
        records.append(record)
        print(f"run {record.run_id}: {'ok' if record.ok else record.failure} {record.intervals_ms} {record.flags}",
              file=sys.stderr)

    report = stats.summarize(records, {"launchToMenuMs": stats.LAUNCH_TO_MENU_BUDGET_MS,
                                       "launchToActionMs": stats.LAUNCH_TO_ACTION_BUDGET_MS,
                                       "startToActionMs": stats.START_TO_ACTION_BUDGET_MS})
    report["condition"] = "android-terminated-process"
    stats.write_reports(records, report, args.csv)
    return 0 if report["failures"] == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
