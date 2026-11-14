# This Source Code Form is subject to the terms of the Mozilla Public
# License, v. 2.0. If a copy of the MPL was not distributed with this
# file, You can obtain one at http://mozilla.org/MPL/2.0/.
"""Shared helpers for CGS startup benchmark harnesses: statistics, result parsing, and reports."""

from __future__ import annotations

import csv
import json
import math
import statistics
import sys
import time
from dataclasses import dataclass, field
from typing import Iterable, Optional

LAUNCH_TO_MENU_BUDGET_MS = 500.0
LAUNCH_TO_ACTION_BUDGET_MS = 1000.0
START_TO_ACTION_BUDGET_MS = 500.0
MIN_BUDGET_RUNS = 30


def monotonic_ns() -> int:
    """The OS monotonic clock the player reads through System.Diagnostics.Stopwatch.

    Linux/SteamOS/Android: CLOCK_MONOTONIC. Windows: QueryPerformanceCounter (perf_counter_ns).
    macOS: mach_absolute_time (monotonic_ns). Each platform adapter must be validated on device.
    """
    if sys.platform.startswith("linux"):
        return time.clock_gettime_ns(time.CLOCK_MONOTONIC)
    if sys.platform == "win32":
        return time.perf_counter_ns()
    return time.monotonic_ns()


def percentile(values: list[float], pct: float) -> Optional[float]:
    """Nearest-rank percentile; conservative for small run counts."""
    if not values:
        return None
    ordered = sorted(values)
    rank = max(1, math.ceil(pct / 100.0 * len(ordered)))
    return ordered[rank - 1]


@dataclass
class RunRecord:
    run_id: str
    ok: bool
    failure: Optional[str] = None
    intervals_ms: dict[str, Optional[float]] = field(default_factory=dict)
    flags: list[str] = field(default_factory=list)


def milestone(summary: Optional[dict], name: str) -> Optional[float]:
    if not summary:
        return None
    return (summary.get("milestonesMs") or {}).get(name)


def parse_result(text: str) -> dict:
    """Parses a benchmark result from a file body or a '[CgsBenchmark] {...}' log line."""
    start = text.find("{")
    if start < 0:
        raise ValueError("no JSON object in benchmark result")
    return json.loads(text[start:])


def result_flags(result: dict) -> list[str]:
    flags = []
    if result.get("gameSwitched"):
        flags.append("gameSwitched")
    if result.get("runFileError"):
        flags.append(str(result["runFileError"]))
    if (result.get("startDelayMs") or 0) > 0:
        flags.append("startDelay")
    return flags


def summarize(records: Iterable[RunRecord], budgets: dict[str, float]) -> dict:
    records = list(records)
    usable = [r for r in records if r.ok and not r.flags]
    report = {"runs": len(records), "usableRuns": len(usable),
              "failures": sum(1 for r in records if not r.ok), "intervals": {}}
    names = sorted({name for r in records for name in r.intervals_ms})
    for name in names:
        values = [r.intervals_ms[name] for r in usable if r.intervals_ms.get(name) is not None]
        p95 = percentile(values, 95)
        budget = budgets.get(name)
        report["intervals"][name] = {
            "count": len(values),
            "medianMs": statistics.median(values) if values else None,
            "p95Ms": p95,
            "budgetMs": budget,
            "meetsBudget": None if budget is None or p95 is None or len(values) < MIN_BUDGET_RUNS
            else p95 <= budget,
        }
    return report


def write_reports(records: list[RunRecord], report: dict, csv_path: Optional[str]) -> None:
    if csv_path:
        names = sorted({name for r in records for name in r.intervals_ms})
        with open(csv_path, "w", newline="", encoding="utf-8") as handle:
            writer = csv.writer(handle)
            writer.writerow(["run", "ok", "failure", "flags", *names])
            for r in records:
                writer.writerow([r.run_id, r.ok, r.failure or "", ";".join(r.flags),
                                 *[("" if r.intervals_ms.get(n) is None else f"{r.intervals_ms[n]:.1f}")
                                   for n in names]])
    print(json.dumps(report, indent=2))
    for name, stats in report["intervals"].items():
        verdict = stats["meetsBudget"]
        note = "n/a" if verdict is None else ("PASS" if verdict else "FAIL")
        median = "-" if stats["medianMs"] is None else f"{stats['medianMs']:.1f}"
        p95 = "-" if stats["p95Ms"] is None else f"{stats['p95Ms']:.1f}"
        print(f"{name}: median {median} ms, p95 {p95} ms, n={stats['count']}, budget {stats['budgetMs']} -> {note}",
              file=sys.stderr)
    print(f"failures: {report['failures']} of {report['runs']} runs; usable: {report['usableRuns']}", file=sys.stderr)
