# This Source Code Form is subject to the terms of the Mozilla Public
# License, v. 2.0. If a copy of the MPL was not distributed with this
# file, You can obtain one at http://mozilla.org/MPL/2.0/.
"""Scan a Unity Editor log for warnings and errors that are not on the known-issue allowlist.

Unity's console is not saved anywhere, but every console message is also written to the Editor log
(Logs/Editor.log for an interactive Editor, or the -logFile path for a batchmode run). This script finds the
messages that indicate project debt, such as compiler warnings, shaders that cannot compile, GUID conflicts,
outdated settings files, and Debug.LogWarning/LogError calls. It skips messages that match allowlist.txt and
exits with status 1 if anything else is found.

  python3 tools/unity-log-scan/scan_unity_log.py Logs/Editor.log
  python3 tools/unity-log-scan/scan_unity_log.py Logs/Editor.log --github   # also emit GitHub annotations
"""

from __future__ import annotations

import argparse
import re
import sys
from collections import Counter
from dataclasses import dataclass
from pathlib import Path

DEFAULT_ALLOWLIST = Path(__file__).with_name("allowlist.txt")

WORKER_PREFIX = re.compile(r"^\[Worker\d+\]\s*")
COMPILER = re.compile(r"^(?P<msg>\S.*\(\d+,\d+\): (?P<level>warning|error) [A-Z]+\d+: .*)$")
SHADER = re.compile(r"^(?:WARNING: Shader Unsupported: .*|Shader (?:warning|error) in '.*)$")
GUID_CONFLICT = re.compile(r"^GUID \[[0-9a-f]{32}\] for asset '.*' conflicts with:$")
OUTDATED_SETTINGS = re.compile(r"below the supported minimum .*re-save", re.IGNORECASE)
STACK_START = re.compile(r"^UnityEngine\.Debug:ExtractStackTraceNoAlloc ")
LOG_CALL = re.compile(r"^UnityEngine\.(?:Debug|Logger):(?P<call>Log(?:Format)?|LogWarning(?:Format)?|LogError(?:Format)?"
                      r"|LogException|LogAssertion(?:Format)?) ")
# Native Editor messages that carry a managed stack trace but no Debug.Log frame (for example, Player Settings
# validation). Treat them as warnings only when the text reads like one.
NATIVE_WARNING_WORDS = re.compile(
    r"\b(?:warn(?:ing)?|deprecated|obsolete|no longer|not supported|unsupported|failed|missing|re-?save)\b",
    re.IGNORECASE)


@dataclass(frozen=True)
class Finding:
    category: str
    level: str
    message: str


def _clean(line: str) -> str:
    return " ".join(WORKER_PREFIX.sub("", line).split())


def _stack_level(lines: list[str], start: int) -> str | None:
    """Return 'warning'/'error' for a Debug.Log* stack trace, '' for a plain Debug.Log, None if no Log frame."""
    for line in lines[start:start + 20]:
        if not line.strip():
            break
        match = LOG_CALL.match(line)
        if match:
            call = match.group("call")
            if call.startswith("LogWarning"):
                return "warning"
            if call.startswith(("LogError", "LogException", "LogAssertion")):
                return "error"
            return ""
    return None


def scan(text: str) -> list[Finding]:
    """Return every warning or error message found in the log, in order, including repeats."""
    lines = text.splitlines()
    findings: list[Finding] = []
    for index, raw in enumerate(lines):
        line = WORKER_PREFIX.sub("", raw).rstrip()
        if match := COMPILER.match(line):
            findings.append(Finding("compiler", match.group("level"), _clean(match.group("msg"))))
        elif SHADER.match(line):
            level = "error" if line.startswith("Shader error") else "warning"
            findings.append(Finding("shader", level, _clean(line)))
        elif GUID_CONFLICT.match(line):
            other = lines[index + 1].strip() if index + 1 < len(lines) else ""
            findings.append(Finding("guid-conflict", "warning", _clean(f"{line} {other}")))
        elif OUTDATED_SETTINGS.search(line):
            findings.append(Finding("outdated-asset", "warning", _clean(line)))
        elif STACK_START.match(line) and index > 0:
            message = _clean(lines[index - 1])
            level = _stack_level(lines, index + 1)
            if level is None and NATIVE_WARNING_WORDS.search(message):
                findings.append(Finding("editor", "warning", message))
            elif level:
                findings.append(Finding("script-log", level, message))
    return findings


def load_allowlist(path: Path) -> list[re.Pattern[str]]:
    patterns = []
    for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
        line = line.strip()
        if not line or line.startswith("#"):
            continue
        try:
            patterns.append(re.compile(line))
        except re.error as error:
            raise SystemExit(f"{path}:{number}: invalid regular expression: {error}") from error
    return patterns


def _escape_annotation(message: str) -> str:
    return message.replace("%", "%25").replace("\r", "%0D").replace("\n", "%0A")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("log", type=Path, help="Path to a Unity Editor log file")
    parser.add_argument("--allowlist", type=Path, default=DEFAULT_ALLOWLIST,
                        help="File of regular expressions for known messages to ignore (default: %(default)s)")
    parser.add_argument("--github", action="store_true", help="Also print GitHub Actions annotations")
    args = parser.parse_args(argv)

    allowlist = load_allowlist(args.allowlist) if args.allowlist.exists() else []
    findings = Counter(scan(args.log.read_text(encoding="utf-8", errors="replace")))
    unexpected: Counter[Finding] = Counter()
    allowed: Counter[str] = Counter()
    for finding, count in findings.items():
        pattern = next((p for p in allowlist if p.search(finding.message)), None)
        if pattern:
            allowed[pattern.pattern] += count
        else:
            unexpected[finding] = count

    if allowed:
        print(f"Ignored {sum(allowed.values())} known message(s) from {args.allowlist}:")
        for pattern, count in allowed.most_common():
            print(f"  {count:>4}x  {pattern}")
    if not unexpected:
        print("No unexpected warnings or errors found.")
        return 0

    print(f"Found {len(unexpected)} unexpected warning/error message(s):")
    for finding, count in sorted(unexpected.items(), key=lambda item: (item[0].level != "error", item[0].category)):
        print(f"  [{finding.level}] [{finding.category}] {count}x  {finding.message}")
        if args.github:
            kind = "error" if finding.level == "error" else "warning"
            print(f"::{kind} title=Unity {finding.category}::{_escape_annotation(finding.message)}")
    return 1


if __name__ == "__main__":
    sys.exit(main())
