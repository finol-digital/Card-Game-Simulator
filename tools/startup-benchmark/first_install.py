# This Source Code Form is subject to the terms of the Mozilla Public
# License, v. 2.0. If a copy of the MPL was not distributed with this
# file, You can obtain one at http://mozilla.org/MPL/2.0/.
"""First-install condition for desktop players: every run starts with no CGS data or settings.

Linux: each run gets an empty XDG_CONFIG_HOME, so your own CGS data is never touched.
Windows and macOS: your CGS data, cache, and settings are renamed aside before the first run and put back afterwards,
even if the runs fail or you press Ctrl+C. Your settings are also exported to <work-dir> first. If the script is ever
stopped before it can put them back (for example, by a power loss), run it with --restore (and, on macOS, --player).
On macOS a sandboxed build (such as the Mac App Store build) shares its data with any installed copy, and can only
read the run file inside its container, so pass a --work-dir inside it:
  --work-dir ~/Library/Containers/com.finoldigital.CardGameSimulator/Data/cgs-bench

  py first_install.py --player "C:/path/to/Card Game Simulator.exe" --build 1.2.3 --runs 30 --csv first.csv
  python3 first_install.py --player "/path/to/Card Game Simulator.x86_64" --build 1.2.3 --runs 30
  python3 first_install.py --player "Card Game Simulator.app/Contents/MacOS/Card Game Simulator" --build 1.2.3
"""

from __future__ import annotations

import argparse
import os
import plistlib
import shutil
import subprocess
import sys
import time
from pathlib import Path

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import desktop_benchmark as bench  # noqa: E402
import startup_stats as stats  # noqa: E402

COMPANY = "Finol Digital LLC"
PRODUCT = "Card Game Simulator"
SAVED_SUFFIX = ".cgs-first-install-saved"


def remove_tree(path: Path) -> None:
    # Windows can hold files briefly after the player exits
    for _ in range(20):
        if not path.exists():
            return
        shutil.rmtree(path, ignore_errors=True)
        time.sleep(0.25)
    if path.exists():
        raise SystemExit(f"Could not delete {path}; close anything using it and try again")


class WindowsData:
    """Moves the player's data folder, cache folder, and registry settings aside, and puts them back."""

    def __init__(self, work_dir: Path):
        profile = Path(os.environ["USERPROFILE"])
        self.folders = [profile / "AppData" / "LocalLow" / COMPANY / PRODUCT,
                        Path(os.environ["TEMP"]) / COMPANY / PRODUCT]
        self.key = rf"HKCU\Software\{COMPANY}\{PRODUCT}"
        self.reg_backup = work_dir / "cgs-settings-backup.reg"
        # Present while the settings exist only in reg_backup
        self.settings_saved = work_dir / "cgs-settings-saved"
        # Present once everything is set aside; from then on, what is in the original places is run data
        self.runs_started = work_dir / "cgs-first-install-running"

    @staticmethod
    def saved(folder: Path) -> Path:
        return folder.with_name(folder.name + SAVED_SUFFIX)

    def reg(self, *args: str, check: bool = True) -> int:
        code = subprocess.run(["reg", *args], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL).returncode
        if check and code != 0:
            raise SystemExit(f"reg {args[0]} failed for {self.key}; your settings are in {self.reg_backup}")
        return code

    def save(self) -> None:
        if self.settings_saved.exists() or self.runs_started.exists() or \
                any(self.saved(f).exists() for f in self.folders):
            raise SystemExit("CGS data from an earlier first-install run is still set aside. Run with --restore.")
        try:
            if self.reg("query", self.key, check=False) == 0:
                self.reg("export", self.key, str(self.reg_backup), "/y")
                self.settings_saved.write_text("1", encoding="utf-8")
                self.reg("delete", self.key, "/f")
            for folder in self.folders:
                if folder.exists():
                    folder.rename(self.saved(folder))
        except BaseException:
            self.restore()
            raise
        self.runs_started.write_text("1", encoding="utf-8")
        print("Your CGS data and settings are set aside; they are put back when the runs end.", file=sys.stderr)

    def clear(self) -> None:
        for folder in self.folders:
            remove_tree(folder)
        self.reg("delete", self.key, "/f", check=False)

    def restore(self) -> None:
        running = self.runs_started.exists()
        for folder in self.folders:
            if self.saved(folder).exists():
                remove_tree(folder)
                self.saved(folder).rename(folder)
            elif running:
                remove_tree(folder)
        if self.settings_saved.exists():
            self.reg("delete", self.key, "/f", check=False)
            self.reg("import", str(self.reg_backup))
            self.settings_saved.unlink()
        elif running:
            self.reg("delete", self.key, "/f", check=False)
        if running:
            self.runs_started.unlink()
        print("Your CGS data and settings are restored.", file=sys.stderr)


class MacData:
    """Moves the player's data folder, cache folder, and settings aside, and puts them back."""

    def __init__(self, work_dir: Path, player: str):
        app = next((parent for parent in Path(player).resolve().parents if parent.suffix == ".app"), None)
        if app is None:
            raise SystemExit("--player must be the executable inside the .app bundle")
        with open(app / "Contents" / "Info.plist", "rb") as info:
            bundle_id = plistlib.load(info)["CFBundleIdentifier"]
        entitlements = subprocess.run(["codesign", "-d", "--entitlements", "-", str(app)], capture_output=True,
                                      text=True).stdout
        library = Path.home() / "Library"
        if "com.apple.security.app-sandbox" in entitlements:
            library = library / "Containers" / bundle_id / "Data" / "Library"
        self.folders = [library / "Application Support" / bundle_id, library / "Caches" / bundle_id]
        # Settings go through cfprefsd, which caches them, so they are changed with defaults instead of as a file
        self.prefs = str(library / "Preferences" / f"{bundle_id}.plist")
        self.prefs_backup = work_dir / "cgs-settings-backup.plist"
        self.settings_saved = work_dir / "cgs-settings-saved"
        self.runs_started = work_dir / "cgs-first-install-running"

    def defaults(self, *args: str, check: bool = True) -> int:
        code = subprocess.run(["defaults", *args], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL).returncode
        if check and code != 0:
            raise SystemExit(f"defaults {args[0]} failed for {self.prefs}; your settings are in {self.prefs_backup}")
        return code

    def save(self) -> None:
        if self.settings_saved.exists() or self.runs_started.exists() or \
                any(WindowsData.saved(f).exists() for f in self.folders):
            raise SystemExit("CGS data from an earlier first-install run is still set aside. Run with --restore.")
        try:
            if Path(self.prefs).exists():
                self.defaults("export", self.prefs, str(self.prefs_backup))
                self.settings_saved.write_text("1", encoding="utf-8")
                self.defaults("delete", self.prefs)
            for folder in self.folders:
                if folder.exists():
                    folder.rename(WindowsData.saved(folder))
        except BaseException:
            self.restore()
            raise
        self.runs_started.write_text("1", encoding="utf-8")
        print("Your CGS data and settings are set aside; they are put back when the runs end.", file=sys.stderr)

    def clear(self) -> None:
        for folder in self.folders:
            remove_tree(folder)
        self.defaults("delete", self.prefs, check=False)

    def restore(self) -> None:
        running = self.runs_started.exists()
        for folder in self.folders:
            if WindowsData.saved(folder).exists():
                remove_tree(folder)
                WindowsData.saved(folder).rename(folder)
            elif running:
                remove_tree(folder)
        if self.settings_saved.exists():
            self.defaults("delete", self.prefs, check=False)
            self.defaults("import", self.prefs, str(self.prefs_backup))
            self.settings_saved.unlink()
        elif running:
            self.defaults("delete", self.prefs, check=False)
        if running:
            self.runs_started.unlink()
        print("Your CGS data and settings are restored.", file=sys.stderr)


class LinuxData:
    """Points the player at an empty config folder for each run."""

    def __init__(self, work_dir: Path):
        self.config = work_dir / "first-install-config"

    def save(self) -> None:
        pass

    def clear(self) -> None:
        remove_tree(self.config)
        self.config.mkdir(parents=True)
        os.environ["XDG_CONFIG_HOME"] = str(self.config)

    def restore(self) -> None:
        remove_tree(self.config)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--player", help="Path to the CGS player executable")
    parser.add_argument("--build", default="", help="Expected Application.version; mismatched runs are flagged")
    parser.add_argument("--runs", type=int, default=stats.MIN_BUDGET_RUNS)
    parser.add_argument("--timeout", type=float, default=180.0, help="Seconds to wait for each run's result")
    parser.add_argument("--work-dir", default=os.path.join(os.path.expanduser("~"), "cgs-bench"))
    parser.add_argument("--csv", help="Write per-run intervals to this CSV file")
    parser.add_argument("--restore", action="store_true", help="Only put back data set aside by an interrupted run")
    args = parser.parse_args()

    work_dir = Path(args.work_dir)
    work_dir.mkdir(parents=True, exist_ok=True)
    if sys.platform == "win32":
        data = WindowsData(work_dir)
    elif sys.platform == "darwin":
        if not args.player:
            parser.error("--player is required on macOS")
        data = MacData(work_dir, args.player)
    elif sys.platform.startswith("linux"):
        data = LinuxData(work_dir)
    else:
        raise SystemExit("First-install runs are supported on Windows, macOS, and Linux")
    if args.restore:
        data.restore()
        return 0
    if not args.player:
        parser.error("--player is required")

    run_args = argparse.Namespace(player=args.player, steam=False, build=args.build, start_delay_ms=0,
                                  player_args=[], timeout=args.timeout, steam_settle=0)
    records = []
    data.save()
    try:
        for index in range(1, args.runs + 1):
            data.clear()
            record = bench.run_once(run_args, index, work_dir)
            records.append(record)
            print(f"run {record.run_id}: {'ok' if record.ok else record.failure} {record.intervals_ms} "
                  f"{record.flags}", file=sys.stderr)
            time.sleep(1.0)
    finally:
        data.restore()

    report = stats.summarize(records, {"launchToMenuMs": stats.LAUNCH_TO_MENU_BUDGET_MS,
                                       "launchToActionMs": stats.LAUNCH_TO_ACTION_BUDGET_MS,
                                       "startToActionMs": stats.START_TO_ACTION_BUDGET_MS})
    report["condition"] = "firstInstall"
    stats.write_reports(records, report, args.csv)
    return 0 if report["failures"] == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
