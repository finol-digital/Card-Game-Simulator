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
using GooglePlayInstant.Editor.QuickDeploy;

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// Provides all "PlayInstant" menu items for the Unity Editor.
    /// </summary>
    public static class PlayInstantEditorMenu
    {
        // Note: cannot use string.Format() in attribute arguments below.
        private const string PlayInstant = "PlayInstant/";
        private const string PlayInstantDocumentation = PlayInstant + "Documentation/";

        [MenuItem(PlayInstantDocumentation + "GitHub Project", false, 100)]
        private static void GitHubProject()
        {
            Application.OpenURL("https://github.com/google/play-instant-unity-plugin");
        }

        [MenuItem(PlayInstantDocumentation + "Developer Site", false, 101)]
        private static void OpenDevDocumentation()
        {
            Application.OpenURL("https://g.co/InstantApps");
        }

        [MenuItem(PlayInstantDocumentation + "Digital Asset Links", false, 102)]
        private static void VerifyAndroidAppLinks()
        {
            Application.OpenURL("https://developer.android.com/training/app-links/verify-site-associations#web-assoc");
        }

        [MenuItem(PlayInstantDocumentation + "Open Source License", false, 103)]
        private static void OpenSourceLicense()
        {
            Application.OpenURL("https://www.apache.org/licenses/LICENSE-2.0");
        }

        [MenuItem(PlayInstant + "Report a Bug", false, 110)]
        private static void OpenReportBug()
        {
            Application.OpenURL("https://github.com/google/play-instant-unity-plugin/issues");
        }

        [MenuItem(PlayInstant + "Check for Plugin Updates...", false, 111)]
        private static void OpenPluginUpdateWindow()
        {
            PluginUpdateWindow.ShowWindow();
        }

        [MenuItem(PlayInstant + "Build Settings...", false, 200)]
        private static void OpenEditorSettings()
        {
            BuildSettingsWindow.ShowWindow();
        }

        [MenuItem(PlayInstant + "Player Settings...", false, 201)]
        private static void CheckPlayerSettings()
        {
            PlayerSettingsWindow.ShowWindow();
        }

        [MenuItem(PlayInstant + "Set up " + PlayInstantRunner.InstantAppsSdkShortDisplayName + "...", false, 202)]
        private static void SetUpPlayInstantSdk()
        {
            PlayInstantRunner.InstallPlayInstantSdk();
        }

        [MenuItem(PlayInstant + "Quick Deploy...", false, 203)]
        private static void QuickDeployOverview()
        {
            QuickDeployWindow.ShowWindow();
        }

#if PLAY_INSTANT_ENABLE_ANDROID_APP_BUNDLE
        [MenuItem(PlayInstant + "Build Android App Bundle...", false, 300)]
        private static void BuildAndroidAppBundle()
        {
            AppBundlePublisher.Build();
        }
#endif

        [MenuItem(PlayInstant + "Build for Play Console...", false, 301)]
        private static void BuildForPlayConsole()
        {
            PlayInstantPublisher.Build();
        }

        [MenuItem(PlayInstant + "Build and Run #%r", false, 302)]
        private static void BuildAndRun()
        {
            PlayInstantRunner.BuildAndRun();
        }
    }
}