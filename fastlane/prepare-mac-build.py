#!/usr/bin/env python3
"""Prepare the built Steamworks plugin for macOS signing and App Store validation."""

import plistlib
import re
import sys
from pathlib import Path


def prepare_app(app):
    app = Path(app)
    if not (app / "Contents/Info.plist").is_file():
        raise ValueError(f"Not a macOS application bundle: {app}")

    plugin = app / "Contents/PlugIns/steam_api.bundle"
    if not plugin.exists():
        return  # Builds without Steamworks need no adjustment.

    plist_path = plugin / "Contents/Info.plist"
    data = plist_path.read_bytes()
    plist = plistlib.loads(data)
    identifier = plist.get("CFBundleIdentifier")
    # Steamworks.NET 2025.164.1 ships an underscore, which Apple rejects.
    # Keep the bundle/executable names used by native loading unchanged.
    if identifier == "com.rileylabrecque.steam_api":
        plist["CFBundleIdentifier"] = "com.rileylabrecque.steam-api"
        fmt = plistlib.FMT_BINARY if data.startswith(b"bplist00") else plistlib.FMT_XML
        plist_path.write_bytes(plistlib.dumps(plist, fmt=fmt, sort_keys=False))
        print("Prepared Steamworks bundle identifier for macOS signing.")
    elif not isinstance(identifier, str) or not re.fullmatch(r"[A-Za-z0-9.-]+", identifier):
        raise ValueError(f"Invalid Steamworks bundle identifier: {identifier!r}")


if __name__ == "__main__":
    if len(sys.argv) != 2:
        sys.exit("Usage: prepare-mac-build.py <application.app>")
    prepare_app(sys.argv[1])
