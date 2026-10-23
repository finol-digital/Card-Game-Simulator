"""Run with: python3 -B -m unittest discover -s fastlane -p 'test_*.py'."""

import importlib.util
import os
import plistlib
import subprocess
import tempfile
import unittest
from pathlib import Path


spec = importlib.util.spec_from_file_location(
    "prepare_mac_build", Path(__file__).with_name("prepare-mac-build.py"))
prepare_mac_build = importlib.util.module_from_spec(spec)
spec.loader.exec_module(prepare_mac_build)


class PrepareMacBuildTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.directory.cleanup)
        self.app = Path(self.directory.name) / "Card Game Simulator.app"
        self.contents = self.app / "Contents"
        self.contents.mkdir(parents=True)
        self.app_plist = self.contents / "Info.plist"
        self.app_data = plistlib.dumps({"CFBundleIdentifier": "com.finoldigital.CardGameSimulator"})
        self.app_plist.write_bytes(self.app_data)
        self.plugin = self.contents / "PlugIns/steam_api.bundle/Contents"

    def write_plugin(self, identifier, fmt=plistlib.FMT_XML):
        self.plugin.mkdir(parents=True, exist_ok=True)
        plist = {"CFBundleIdentifier": identifier, "CFBundleExecutable": "libsteam_api.dylib",
                 "CFBundleVersion": "1.64", "CFBundlePackageType": "BNDL"}
        path = self.plugin / "Info.plist"
        path.write_bytes(plistlib.dumps(plist, fmt=fmt))
        return path, plist

    def test_corrects_upstream_identifier_without_changing_loading_or_app_identity(self):
        for fmt in (plistlib.FMT_XML, plistlib.FMT_BINARY):
            with self.subTest(format=fmt):
                path, original = self.write_plugin("com.rileylabrecque.steam_api", fmt)
                executable = self.plugin / "MacOS/libsteam_api.dylib"
                executable.parent.mkdir(exist_ok=True)
                executable.write_bytes(b"native Steam API")
                prepare_mac_build.prepare_app(self.app)
                expected = dict(original, CFBundleIdentifier="com.rileylabrecque.steam-api")
                self.assertEqual(expected, plistlib.loads(path.read_bytes()))
                self.assertEqual(fmt == plistlib.FMT_BINARY, path.read_bytes().startswith(b"bplist00"))
                self.assertEqual(b"native Steam API", executable.read_bytes())
                self.assertEqual(self.app_data, self.app_plist.read_bytes())
                prepared = path.read_bytes()
                prepare_mac_build.prepare_app(self.app)
                self.assertEqual(prepared, path.read_bytes())

    def test_leaves_valid_upstream_identifier_unchanged(self):
        path, _ = self.write_plugin("com.valvesoftware.steam-api")
        original = path.read_bytes()
        prepare_mac_build.prepare_app(self.app)
        self.assertEqual(original, path.read_bytes())

    def test_supports_build_without_steam(self):
        prepare_mac_build.prepare_app(self.app)
        self.assertEqual(self.app_data, self.app_plist.read_bytes())

    def test_rejects_unexpected_invalid_identifier_without_rewriting(self):
        path, _ = self.write_plugin("unexpected_invalid_identifier")
        original = path.read_bytes()
        with self.assertRaises(ValueError):
            prepare_mac_build.prepare_app(self.app)
        self.assertEqual(original, path.read_bytes())

    def test_rejects_incomplete_plugin_or_missing_app(self):
        with self.assertRaises(ValueError):
            prepare_mac_build.prepare_app(self.app / "missing.app")
        self.plugin.mkdir(parents=True)
        with self.assertRaises(FileNotFoundError):
            prepare_mac_build.prepare_app(self.app)

    def run_signing_script(self, fail_signing=False):
        tools = Path(self.directory.name) / "tools"
        tools.mkdir()
        # Exercise the real shell flow without Apple certificates or macOS tools.
        stub = tools / "stub"
        stub.write_text("""#!/usr/bin/env python3
import os, pathlib, plistlib, sys
command = pathlib.Path(sys.argv[0]).name
if command == 'openssl':
    print('test-keychain-password')
elif command in ('codesign', 'productbuild'):
    app = pathlib.Path(os.environ['MAC_BUILD_PATH']) / (os.environ['PROJECT_NAME'] + '.app')
    with (app / 'Contents/PlugIns/steam_api.bundle/Contents/Info.plist').open('rb') as stream:
        assert plistlib.load(stream)['CFBundleIdentifier'] == 'com.rileylabrecque.steam-api'
    with open(os.environ['SIGNING_LOG'], 'a') as log:
        log.write(command + '\\n')
    if command == 'codesign' and os.environ.get('FAIL_SIGNING') == '1':
        sys.exit(1)
""")
        stub.chmod(0o755)
        for command in ("openssl", "security", "sleep", "codesign", "productbuild"):
            (tools / command).symlink_to(stub)
        log = Path(self.directory.name) / "signing.log"
        self.app.rename(self.app.with_name("StandaloneOSX.app"))
        env = dict(os.environ, PATH=f"{tools}:{os.environ['PATH']}",
                   MAC_BUILD_PATH=self.directory.name, PROJECT_NAME="Card Game Simulator",
                   MAC_APP_BUNDLE_PATHS="Contents/PlugIns/StandaloneFileBrowser.bundle",
                   SIGNING_LOG=str(log), FAIL_SIGNING="1" if fail_signing else "0")
        for name in ("APPLE_APPLICATION_CERTIFICATE", "APPLE_DISTRIBUTION_CERTIFICATE",
                     "APPLE_INSTALLER_CERTIFICATE"):
            env[name] = ""
        result = subprocess.run(["sh", str(Path(__file__).with_name("sign-mac-build.sh").resolve())],
                                cwd=self.directory.name, env=env, capture_output=True, text=True)
        return result, log.read_text().splitlines() if log.exists() else []

    def test_preparation_precedes_all_signing_and_packaging(self):
        self.write_plugin("com.rileylabrecque.steam_api")
        result, commands = self.run_signing_script()
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual(["codesign", "codesign", "productbuild"], commands)

    def test_signing_failure_prevents_packaging(self):
        self.write_plugin("com.rileylabrecque.steam_api")
        result, commands = self.run_signing_script(fail_signing=True)
        self.assertNotEqual(0, result.returncode)
        self.assertEqual(["codesign"], commands)

    def test_preparation_failure_prevents_signing_and_packaging(self):
        self.write_plugin("unexpected_invalid_identifier")
        result, commands = self.run_signing_script()
        self.assertNotEqual(0, result.returncode)
        self.assertEqual([], commands)


if __name__ == "__main__":
    unittest.main()
