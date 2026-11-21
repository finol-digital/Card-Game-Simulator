# This Source Code Form is subject to the terms of the Mozilla Public
# License, v. 2.0. If a copy of the MPL was not distributed with this
# file, You can obtain one at http://mozilla.org/MPL/2.0/.
"""Tests for scan_unity_log.py, using excerpts from real Unity 6.6 Editor logs."""

import contextlib
import io
import tempfile
import unittest
from pathlib import Path

from scan_unity_log import Finding, main, scan

LOG = """\
Assets/Tests/PlayMode/Example.cs(20,59): warning UAC1001: Field 'Operation' is skipped by serialization.
Assets/Scripts/Cgs/Example.cs(3,1): error CS0246: The type or namespace name 'Foo' could not be found
Importing 'GUID: e8b802ac01d87144b91f12f40c1f2659 - Path: Packages/com.unity.render-pipelines.universal/Shaders/SpatialMappingWireframe.shader' with importer (ShaderImporter)
WARNING: Shader Unsupported: 'Universal Render Pipeline/VR/SpatialMapping/Wireframe' - All subshaders removed
WARNING: Shader Did you use #pragma only_renderers and omit this platform?
WARNING: Shader If subshaders removal was intentional, you may have forgotten turning Fallback off?
GUID [74f647076bca3d442bc4a6ace5daf59f] for asset 'Assets/Canvas Color.shadersubgraph.meta' conflicts with:
  'Packages/com.unity.shadergraph/GraphTemplates/Subgraphs/Canvas Color.shadersubgraph.meta'
Assigning a new guid.
Serialized file "ProjectSettings/AudioManager.asset" contains a AudioManager object at version 1, below the supported minimum (2). Open and re-save the file to upgrade.
[ServicesCore]: [Environments] No active environment set; defaulting to 'production'.
UnityEngine.Debug:ExtractStackTraceNoAlloc (byte*,int,string)
UnityEngine.StackTraceUtility:ExtractStackTrace () (at /Users/bokken/build/output/unity/unity/Runtime/Export/Scripting/StackTrace.cs:23)
UnityEngine.Logger:LogWarning (string,object)
Unity.Services.Core.Internal.CoreLogger:LogWarning (object)

[Worker0] Only Adaptive icons are supported. Legacy and Round settings are no longer available.
UnityEngine.Debug:ExtractStackTraceNoAlloc (byte*,int,string)
UnityEditor.PlayerSettings:GetProjectSettingsPlayerSettings ()

Loaded the main menu.
UnityEngine.Debug:ExtractStackTraceNoAlloc (byte*,int,string)
UnityEngine.StackTraceUtility:ExtractStackTrace () (at /home/bokken/build/output/unity/unity/Runtime/Export/Scripting/StackTrace.cs:37)
UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)
UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.Object,string,object[])
UnityEngine.Logger:Log (UnityEngine.LogType,object)
UnityEngine.Debug:Log (object)
Cgs.Menu.MainMenu:Start ()

Game data failed to load.
UnityEngine.Debug:ExtractStackTraceNoAlloc (byte*,int,string)
UnityEngine.StackTraceUtility:ExtractStackTrace () (at /home/bokken/build/output/unity/unity/Runtime/Export/Scripting/StackTrace.cs:37)
UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)
UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.Object,string,object[])
UnityEngine.Logger:Log (UnityEngine.LogType,object)
UnityEngine.Debug:LogError (object)
Cgs.CardGameManager:Load ()
"""

# Messages logged while tests run, in the order Unity writes the stack frames.
TEST_LOG = """\
Invalid card game archive.
UnityEngine.Debug:ExtractStackTraceNoAlloc (byte*,int,string)
UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.Object,string,object[])
UnityEngine.Logger:Log (UnityEngine.LogType,object)
UnityEngine.Debug:LogError (object)
Cgs.CardGameManager:ImportCardGame (string,System.Action) (at Assets/Scripts/Cgs/CardGameManager.cs:316)
Tests.PlayMode.CardGameManagerTests:ImportCompletionReleasesInvalidArchive (string,bool)
System.Reflection.RuntimeMethodInfo:Invoke (object,System.Reflection.BindingFlags,System.Reflection.Binder,object[],System.Globalization.CultureInfo)
NUnit.Framework.Internal.Reflect:InvokeMethod (System.Reflection.MethodInfo,object,object[])

CreateSprite::TextureNull
UnityEngine.Debug:ExtractStackTraceNoAlloc (byte*,int,string)
UnityEngine.Logger:Log (UnityEngine.LogType,object)
UnityEngine.Debug:LogWarning (object)
UnityExtensionMethods.UnityFileMethods:CreateSprite (UnityEngine.Texture2D) (at Assets/Scripts/UnityExtensionMethods/UnityFileMethods.cs:591)
UnityEngine.TestTools.TestEnumerator/<Execute>d__8:MoveNext ()
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

RecoverLostPlaceholder: PlayController.Instance == null
UnityEngine.Debug:ExtractStackTraceNoAlloc (byte*,int,string)
UnityEngine.Logger:Log (UnityEngine.LogType,object)
UnityEngine.Debug:LogWarning (object)
Cgs.CardGameView.Multiplayer.CgsNetPlayable:Update () (at Assets/Scripts/Cgs/CardGameView/Multiplayer/CgsNetPlayable.cs:377)
"""


class ScanTests(unittest.TestCase):
    def test_finds_each_kind_of_warning_and_error(self):
        findings = scan(LOG)
        self.assertEqual([(f.category, f.level) for f in findings], [
            ("compiler", "warning"),
            ("compiler", "error"),
            ("shader", "warning"),
            ("guid-conflict", "warning"),
            ("outdated-asset", "warning"),
            ("script-log", "warning"),
            ("editor", "warning"),
            ("script-log", "error"),
        ])

    def test_messages_are_cleaned_and_joined(self):
        findings = scan(LOG)
        self.assertIn("conflicts with: 'Packages/com.unity.shadergraph/GraphTemplates/Subgraphs/Canvas Color",
                      findings[3].message)
        self.assertTrue(findings[6].message.startswith("Only Adaptive icons"))
        self.assertEqual(findings[7], Finding("script-log", "error", "Game data failed to load."))

    def test_plain_debug_log_is_not_reported(self):
        self.assertNotIn("Loaded the main menu.", [f.message for f in scan(LOG)])

    def test_messages_logged_by_a_running_test_are_not_reported(self):
        self.assertEqual(scan(TEST_LOG), [
            Finding("script-log", "warning", "RecoverLostPlaceholder: PlayController.Instance == null")])


class MainTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.directory = Path(self.temporary.name)
        self.allowlist = self.directory / "allowlist.txt"
        self.allowlist.write_text("# comment\n^WARNING: Shader Unsupported: 'Universal Render Pipeline/VR/\n",
                                  encoding="utf-8")

    def run_main(self, log: str, *extra: str) -> tuple[int, str]:
        path = self.directory / "Editor.log"
        path.write_text(log, encoding="utf-8")
        output = io.StringIO()
        with contextlib.redirect_stdout(output):
            status = main([str(path), "--allowlist", str(self.allowlist), *extra])
        return status, output.getvalue()

    def test_allowlisted_messages_pass(self):
        status, output = self.run_main(
            "WARNING: Shader Unsupported: 'Universal Render Pipeline/VR/SpatialMapping/Wireframe' - removed\n")
        self.assertEqual(status, 0)
        self.assertIn("No unexpected warnings or errors found.", output)

    def test_unexpected_messages_fail_with_annotations(self):
        status, output = self.run_main(LOG, "--github")
        self.assertEqual(status, 1)
        self.assertIn("::error title=Unity compiler::", output)
        self.assertIn("::warning title=Unity guid-conflict::", output)
        self.assertNotIn("SpatialMapping", output.split("Found")[1])

    def test_test_log_skips_script_messages_only(self):
        status, output = self.run_main(LOG + TEST_LOG, "--test-log")
        self.assertEqual(status, 1)
        self.assertNotIn("[script-log]", output)
        self.assertIn("[compiler]", output)
        self.assertIn("[editor]", output)

    def test_invalid_allowlist_pattern_is_reported(self):
        self.allowlist.write_text("[unclosed\n", encoding="utf-8")
        with self.assertRaises(SystemExit) as raised:
            self.run_main("")
        self.assertIn("invalid regular expression", str(raised.exception))


if __name__ == "__main__":
    unittest.main()
