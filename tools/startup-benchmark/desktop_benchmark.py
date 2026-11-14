# This Source Code Form is subject to the terms of the Mozilla Public
# License, v. 2.0. If a copy of the MPL was not distributed with this
# file, You can obtain one at http://mozilla.org/MPL/2.0/.
"""Desktop and Steam launch-action benchmark for CGS (Windows, Linux, macOS, Steam Deck).

Before each run this writes a run file with a fresh run ID, the expected build, and the OS monotonic time taken
immediately before the launch request, then launches the player directly or through Steam. The player logs its
milestones on the same clock, so launch-action intervals are exact on one clock.

Direct launch:
  python3 desktop_benchmark.py --player "/path/to/Card Game Simulator.x86_64" --build 1.2.3 --runs 30

Through Steam (set the game's launch options once, in Steam > Properties > Launch Options, to
  -cgsBenchmark -cgsRunFile=$HOME/cgs-bench/run.json -cgsResultFile=$HOME/cgs-bench/result.json ):
  python3 desktop_benchmark.py --steam --build 1.2.3 --runs 30
"""

from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys
import time
import uuid
from pathlib import Path

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import startup_stats as stats  # noqa: E402

STEAM_APP_ID = "1742850"


def run_once(args, run_index: int, work_dir: Path) -> stats.RunRecord:
    run_id = f"{run_index}-{uuid.uuid4().hex[:8]}"
    run_file = work_dir / "run.json"
    result_file = work_dir / "result.json"
    for stale in (result_file, run_file, Path(str(run_file) + ".consumed")):
        if stale.exists():
            stale.unlink()

    launch_ns = stats.monotonic_ns()
    run_file.write_text(json.dumps({"runId": run_id, "build": args.build or "", "monotonicNs": launch_ns}),
                        encoding="utf-8")

    process = None
    if args.steam:
        # Timestamp is taken before Steam handles the launch request
        subprocess.Popen(["steam", f"steam://rungameid/{STEAM_APP_ID}"], stdout=subprocess.DEVNULL,
                         stderr=subprocess.DEVNULL)
    else:
        process = subprocess.Popen([args.player, "-cgsBenchmark", f"-cgsRunFile={run_file}",
                                    f"-cgsResultFile={result_file}", f"-cgsRun={run_id}",
                                    f"-cgsStartDelayMs={args.start_delay_ms}", *args.player_args],
                                   stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)

    deadline = time.monotonic() + args.timeout
    while time.monotonic() < deadline and not result_file.exists():
        if process is not None and process.poll() is not None and not result_file.exists():
            time.sleep(0.5)
            break
        time.sleep(0.05)

    if process is not None:
        try:
            process.wait(timeout=15)
        except subprocess.TimeoutExpired:
            process.kill()
    elif args.steam:
        time.sleep(args.steam_settle)

    if not result_file.exists():
        if args.steam:
            # The player may still be running and could write a later run's result.json
            raise SteamRunAborted(f"Steam run {run_id} produced no result within {args.timeout:g} s. "
                                  "Stop Card Game Simulator before running again.")
        return stats.RunRecord(run_id, False, "noResult")

    try:
        result = stats.parse_result(result_file.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        return stats.RunRecord(run_id, False, "malformedResult")
    return to_record(run_id, result)


class SteamRunAborted(RuntimeError):
    pass


def to_record(run_id: str, result: dict) -> stats.RunRecord:
    launch, play = result.get("launch"), result.get("play")
    offset = (launch or {}).get("launchRequestOffsetMs")
    menu = stats.milestone(launch, "mainMenuInteractive")
    start = stats.milestone(play, "startGameChosen")
    action = stats.milestone(play, "firstCardAction")
    flags = stats.result_flags(result)
    if result.get("runId") != run_id:
        flags.append("runIdMismatch")
    if offset is None:
        flags.append("noLaunchClock")
    intervals = {
        "launchToMenuMs": None if offset is None or menu is None else offset + menu,
        "launchToActionMs": None if offset is None or action is None else offset + action,
        "startToActionMs": None if start is None or action is None else action - start,
        "managedToMenuMs": menu,  # elapsed zero (first managed callback) to menu; diagnostic only
    }
    ok = result.get("outcome") == "ok"
    return stats.RunRecord(run_id, ok, None if ok else result.get("failedMilestone") or "failed", intervals, flags)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    target = parser.add_mutually_exclusive_group(required=True)
    target.add_argument("--player", help="Path to the CGS player executable")
    target.add_argument("--steam", action="store_true", help="Launch through Steam (see module docs)")
    parser.add_argument("--build", default="", help="Expected Application.version; mismatched runs are flagged")
    parser.add_argument("--runs", type=int, default=stats.MIN_BUDGET_RUNS)
    parser.add_argument("--timeout", type=float, default=120.0, help="Seconds to wait for each run's result")
    parser.add_argument("--start-delay-ms", type=float, default=0)
    parser.add_argument("--work-dir", default=os.path.join(os.path.expanduser("~"), "cgs-bench"))
    parser.add_argument("--steam-settle", type=float, default=5.0, help="Seconds to let Steam settle between runs")
    parser.add_argument("--csv", help="Write per-run intervals to this CSV file")
    parser.add_argument("player_args", nargs="*", help="Extra player arguments after --")
    args = parser.parse_args()

    work_dir = Path(args.work_dir)
    work_dir.mkdir(parents=True, exist_ok=True)
    records = []
    aborted = None
    for index in range(1, args.runs + 1):
        try:
            record = run_once(args, index, work_dir)
        except SteamRunAborted as error:
            aborted = str(error)
            print(aborted, file=sys.stderr)
            break
        records.append(record)
        print(f"run {record.run_id}: {'ok' if record.ok else record.failure} {record.intervals_ms} {record.flags}",
              file=sys.stderr)
        time.sleep(1.0)

    report = stats.summarize(records, {"launchToMenuMs": stats.LAUNCH_TO_MENU_BUDGET_MS,
                                       "launchToActionMs": stats.LAUNCH_TO_ACTION_BUDGET_MS,
                                       "startToActionMs": stats.START_TO_ACTION_BUDGET_MS})
    report["condition"] = "steam" if args.steam else "direct"
    if aborted:
        report["aborted"] = aborted
    stats.write_reports(records, report, args.csv)
    return 0 if report["failures"] == 0 and not aborted else 1


if __name__ == "__main__":
    sys.exit(main())
