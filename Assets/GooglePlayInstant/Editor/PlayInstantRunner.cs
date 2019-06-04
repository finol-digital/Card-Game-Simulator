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

using System.IO;
using GooglePlayInstant.Editor.GooglePlayServices;
using UnityEditor;
using UnityEngine;

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// Helper to build Play Instant APKs and run them on a device.
    /// </summary>
    public static class PlayInstantRunner
    {
        public const string InstantAppsSdkDisplayName = "Google Play Instant Development SDK";
        public const string InstantAppsSdkShortDisplayName = "Play Instant SDK";
        private const string InstantAppsSdkPackageName = "extras;google;instantapps";
        private const string InstantAppsJarPath = "extras/google/instantapps/tools/ia.jar";

        /// <summary>
        /// Builds an APK to a temporary location using the scenes selected in Unity's main Build Settings.
        /// </summary>
        public static void BuildAndRun()
        {
            if (!PlayInstantBuilder.CheckBuildPrerequisites())
            {
                return;
            }

            var jarPath = Path.Combine(AndroidSdkManager.AndroidSdkRoot, InstantAppsJarPath);
            if (!File.Exists(jarPath))
            {
                Debug.LogErrorFormat("Build and Run failed to locate ia.jar file at: {0}", jarPath);
                var message =
                    string.Format(
                        "Failed to locate version 1.2 or later of the {0}.\n\nClick \"OK\" to install the {0}.",
                        InstantAppsSdkDisplayName);
                if (PlayInstantBuilder.DisplayBuildErrorDialog(message))
                {
                    InstallPlayInstantSdk();
                }

                return;
            }

#if UNITY_2018_3_OR_NEWER
            EditorUserBuildSettings.buildAppBundle = false;
#endif

            var apkPath = Path.Combine(Path.GetTempPath(), "temp.apk");
            Debug.LogFormat("Build and Run package location: {0}", apkPath);

            var buildPlayerOptions = PlayInstantBuilder.CreateBuildPlayerOptions(apkPath,
                EditorUserBuildSettings.development ? BuildOptions.Development : BuildOptions.None);
            if (!PlayInstantBuilder.BuildAndSign(buildPlayerOptions))
            {
                // Do not log here. The method we called was responsible for logging.
                return;
            }

            var window = PostBuildCommandLineDialog.CreateDialog("Install and run app");
            window.modal = false;
            window.summaryText = "Installing app on device";
            window.bodyText = "The APK built successfully.\n\n";
            window.autoScrollToBottom = true;
            window.CommandLineParams = new CommandLineParameters
            {
                FileName = JavaUtilities.JavaBinaryPath,
                Arguments = string.Format(
                    "-jar {0} run {1}",
                    CommandLine.QuotePath(jarPath),
                    CommandLine.QuotePath(apkPath))
            };
            window.CommandLineParams.AddEnvironmentVariable(
                AndroidSdkManager.AndroidHome, AndroidSdkManager.AndroidSdkRoot);
            window.Show();
        }

        /// <summary>
        /// Performs installation or upgrade of the Google Play Instant Development SDK.
        /// </summary>
        public static void InstallPlayInstantSdk()
        {
            AndroidSdkPackageInstaller.InstallPackage(InstantAppsSdkPackageName, InstantAppsSdkDisplayName);
        }
    }
}