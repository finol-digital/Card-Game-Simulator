// Copyright 2018 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using UnityEditor;
using UnityEngine;

namespace PlayInstant.Editor
{
    public static class PlayInstantEditorMenu
    {
        [MenuItem("PlayInstant/Documentation/Developers Site", false, 100)]
        private static void OpenDocumentation()
        {
            // TODO: Also provide Unity/Games specific links
            Application.OpenURL("https://developer.android.com/topic/instant-apps/index.html");
        }

        [MenuItem("PlayInstant/Documentation/Configuring Digital Asset Links", false, 101)]
        private static void VerifyAndroidAppLinks()
        {
            Application.OpenURL("https://developer.android.com/training/app-links/verify-site-associations#web-assoc");
        }

        [MenuItem("PlayInstant/Report Bug", false, 102)]
        private static void OpenReportBug()
        {
            // TODO: Use GitHub issue tracker instead
            Application.OpenURL("https://issuetracker.google.com/issues/new?component=316045");
        }

        [MenuItem("PlayInstant/Configure Instant or Installed...", false, 200)]
        private static void OpenEditorSettings()
        {
            EditorWindow.GetWindow(typeof(PlayInstantSettingsWindow), true, "Play Instant Settings");
        }

        [MenuItem("PlayInstant/Check Player Settings...", false, 201)]
        private static void CheckPlayerSettings()
        {
            EditorWindow.GetWindow(typeof(PlayerAndBuildSettingsWindow), true, "Check Player Settings");
        }

        [MenuItem("PlayInstant/Set up Play Instant SDK...", false, 202)]
        private static void SetUpPlayInstantSdk()
        {
            PlayInstantSdkInstaller.SetUp();
        }

        [MenuItem("PlayInstant/Build and Run #%r", false, 300)]
        private static void RunOnDevice()
        {
            PlayInstantRunner.BuildAndRun();
        }
    }
}