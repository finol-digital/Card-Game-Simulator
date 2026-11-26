# This Source Code Form is subject to the terms of the Mozilla Public
# License, v. 2.0. If a copy of the MPL was not distributed with this
# file, You can obtain one at http://mozilla.org/MPL/2.0/.
"""Startup regression check for a standalone Linux player.

Runs the player in benchmark mode N times, in -batchmode and with its own data folder, after warm-up runs that
install the bundled games. It exits with status 1 when any run fails, or when the median of an interval is above its
loose threshold in regression_thresholds.json, and 0 otherwise.

  python3 regression_check.py --player Builds/Linux/CardGameSimulator.x86_64 --runs 10

The player can be a local build or the `cgs-StandaloneLinux64` artifact from CI (unzip it and make the executable
runnable). The thresholds are deliberately loose; they catch large regressions, not budget misses.
"""

from __future__ import annotations

import argparse
import json
import os
import statistics
import sys
import tempfile
import time
from pathlib import Path

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import desktop_benchmark  # noqa: E402
import startup_stats as stats  # noqa: E402

DEFAULT_THRESHOLDS = Path(__file__).with_name("regression_thresholds.json")


def check(records: list[stats.RunRecord], thresholds: dict[str, float]) -> tuple[bool, list[str]]:
    """Returns whether the runs pass, with a line per interval and any failed runs."""
    lines = []
    passed = True
    failed = [r for r in records if not r.ok]
    for record in failed:
        lines.append(f"run {record.run_id} failed: {record.failure}")
        passed = False
    usable = [r for r in records if r.ok]
    for name, threshold in thresholds.items():
        values = [r.intervals_ms[name] for r in usable if r.intervals_ms.get(name) is not None]
        if not values:
            lines.append(f"{name}: no values -> FAIL")
            passed = False
            continue
        median = statistics.median(values)
        ok = median <= threshold
        passed &= ok
        lines.append(f"{name}: median {median:.0f} ms (n={len(values)}, max {max(values):.0f} ms), "
                     f"threshold {threshold:.0f} ms -> {'PASS' if ok else 'FAIL'}")
    return passed, lines


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--player", required=True, help="Path to the standalone Linux player executable")
    parser.add_argument("--runs", type=int, default=10)
    parser.add_argument("--warmup-runs", type=int, default=2,
                        help="Uncounted first runs, which install the bundled games into the new data folder")
    parser.add_argument("--thresholds", default=str(DEFAULT_THRESHOLDS))
    parser.add_argument("--timeout", type=float, default=120.0, help="Seconds to wait for each run's result")
    parser.add_argument("--keep-data", action="store_true", help="Keep the data folder and player logs")
    parser.add_argument("--nographics", action="store_true",
                        help="Start the player without a graphics device, for machines with no GPU or display")
    args = parser.parse_args()

    thresholds = json.loads(Path(args.thresholds).read_text(encoding="utf-8"))["medianMs"]
    data_dir = Path(tempfile.mkdtemp(prefix="cgs-regression-"))
    # The player keeps its saved data under XDG_CONFIG_HOME on Linux, so the runs never touch a player's own data
    os.environ["XDG_CONFIG_HOME"] = str(data_dir / "config")
    work_dir = data_dir / "work"
    work_dir.mkdir(parents=True)

    def log_path(index: int) -> Path:
        return work_dir / f"player-{index}.log"

    def run(index: int) -> stats.RunRecord:
        options = argparse.Namespace(player=args.player, build="", timeout=args.timeout, start_delay_ms=0,
                                     steam=False,
                                     player_args=["-batchmode", *(["-nographics"] if args.nographics else []),
                                                  "-logFile", str(log_path(index))])
        return desktop_benchmark.run_once(options, index, work_dir)

    for index in range(1, args.warmup_runs + 1):
        record = run(-index)
        print(f"warm-up {index}: {'ok' if record.ok else record.failure}", file=sys.stderr)
    records = []
    for index in range(1, args.runs + 1):
        record = run(index)
        records.append(record)
        print(f"run {index}: {'ok' if record.ok else record.failure} "
              f"{ {k: round(v) for k, v in record.intervals_ms.items() if v is not None} }", file=sys.stderr)
        time.sleep(0.5)

    passed, lines = check(records, thresholds)
    for line in lines:
        print(line)
    print("PASS" if passed else "FAIL")
    failed = next((index for index, record in enumerate(records, 1) if not record.ok), None)
    if failed is not None:
        # The player's own log usually says why a run failed
        log = log_path(failed)
        tail = log.read_text(encoding="utf-8", errors="replace").splitlines()[-60:] if log.exists() else []
        print(f"--- end of the player log for run {failed} ({log}) ---")
        print("\n".join(tail) if tail else "(no log was written)")
    if args.keep_data:
        print(f"data and logs kept in {data_dir}", file=sys.stderr)
    else:
        import shutil
        shutil.rmtree(data_dir, ignore_errors=True)
    return 0 if passed else 1


if __name__ == "__main__":
    sys.exit(main())
