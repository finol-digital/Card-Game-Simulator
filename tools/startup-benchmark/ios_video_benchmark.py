# This Source Code Form is subject to the terms of the Mozilla Public
# License, v. 2.0. If a copy of the MPL was not distributed with this
# file, You can obtain one at http://mozilla.org/MPL/2.0/.
"""iOS (TestFlight) video benchmark for CGS: turns frame numbers read from high-frame-rate video into budget results.

Procedure (see developer-docs/startup-performance.md):
1. Film the iPhone screen with an external camera at >= 240 fps. Launch CGS by tapping a benchmark link, e.g.
   cardgamesim://main?benchmark=1&run=7 , saved in Notes. Use a new run number for each launch.
2. In the video, note the frame numbers of: the tap (finger contact), the green "menu" marker (top-left),
   the blue "start" marker (top-right), and the yellow "action" marker (bottom-left) or red "failed" marker
   (bottom-right). Each marker shows the run number.
3. Put one row per run in a CSV with columns: run,tap,menu,start,action,failed (leave unused cells empty).
4. Share each run's JSON from the app's results panel into one folder (optional, for diagnostics and pass/fail).

  python3 ios_video_benchmark.py frames.csv --fps 240 --display-hz 60 --results-dir shared-json/ --csv out.csv

Each interval is recorded as its measured value plus one capture frame plus one display refresh, so a run passes
only if that conservative upper bound meets the budget.
"""

from __future__ import annotations

import argparse
import csv
import json
import os
import sys
from pathlib import Path

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import startup_stats as stats  # noqa: E402


def frame_interval(start, end, fps: float, margin_ms: float):
    if start in (None, "") or end in (None, ""):
        return None
    return (float(end) - float(start)) * 1000.0 / fps + margin_ms


def load_results(directory):
    results = {}
    if not directory:
        return results
    for path in Path(directory).glob("*.json"):
        try:
            result = json.loads(path.read_text(encoding="utf-8"))
            results[str(result.get("runId"))] = result
        except (ValueError, OSError):
            print(f"skipping unreadable result {path}", file=sys.stderr)
    return results


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("frames_csv")
    parser.add_argument("--fps", type=float, default=240.0, help="Capture frame rate of the video")
    parser.add_argument("--display-hz", type=float, default=60.0, help="Refresh rate of the phone display")
    parser.add_argument("--results-dir", help="Folder of JSON results shared from the app")
    parser.add_argument("--csv")
    args = parser.parse_args()
    if args.fps < 240:
        print("warning: capture below 240 fps widens the uncertainty margin", file=sys.stderr)

    margin_ms = 1000.0 / args.fps + 1000.0 / args.display_hz
    results = load_results(args.results_dir)
    records = []
    with open(args.frames_csv, newline="", encoding="utf-8") as handle:
        for row in csv.DictReader(handle):
            run_id = (row.get("run") or "").strip()
            failed_marker = (row.get("failed") or "").strip() != ""
            intervals = {
                "launchToMenuMs": frame_interval(row.get("tap"), row.get("menu"), args.fps, margin_ms),
                "launchToActionMs": frame_interval(row.get("tap"), row.get("action"), args.fps, margin_ms),
                "startToActionMs": frame_interval(row.get("start"), row.get("action"), args.fps, margin_ms),
            }
            ok = not failed_marker and intervals["launchToActionMs"] is not None
            flags = []
            result = results.get(run_id)
            if args.results_dir:
                if result is None:
                    flags.append("noJson")
                else:
                    flags.extend(stats.result_flags(result))
                    if (result.get("outcome") == "ok") != ok:
                        # Video and app disagree about pass/fail; discard and re-run
                        flags.append("videoJsonMismatch")
            failure = None if ok else ("failedMarker" if failed_marker else "missingFrames")
            records.append(stats.RunRecord(run_id, ok, failure, intervals, flags))

    report = stats.summarize(records, {"launchToMenuMs": stats.LAUNCH_TO_MENU_BUDGET_MS,
                                       "launchToActionMs": stats.LAUNCH_TO_ACTION_BUDGET_MS,
                                       "startToActionMs": stats.START_TO_ACTION_BUDGET_MS})
    report["condition"] = "ios-video"
    report["uncertaintyMarginMs"] = margin_ms
    stats.write_reports(records, report, args.csv)
    return 0 if report["failures"] == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
