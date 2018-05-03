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

using System.Collections.Generic;
using System.IO;
using GooglePlayServices;
using UnityEditor;
#if UNITY_2018_1_OR_NEWER
using UnityEditor.Build.Reporting;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlayInstant.Editor
{
    public static class PlayInstantRunner
    {
        public static void BuildAndRun()
        {
            if (!PlayInstantSettingsWindow.IsPlayInstantScriptingSymbolDefined())
            {
                LogError(
                    "The currently selected Android Platform is \"Installed\". " +
                    "Open \"Configure Instant or Installed...\" and change this to \"Instant\".");
                return;
            }

            var tempFilePath = Path.Combine(Path.GetTempPath(), "temp.apk");
            Debug.LogFormat("Build and Run package location: {0}", tempFilePath);

            var buildPlayerOptions = CreateBuildPlayerOptions(tempFilePath);
#if UNITY_2018_1_OR_NEWER
            var buildReport = BuildPipeline.BuildPlayer(buildPlayerOptions);
            switch (buildReport.summary.result)
            {
                case BuildResult.Succeeded:
                    break;
                case BuildResult.Cancelled:
                    return;
                case BuildResult.Failed:
                    LogError(string.Format("Build failed with {0} error(s)", buildReport.summary.totalErrors));
                    return;
                default:
                    LogError("Build failed with unknown error");
                    return;
            }
#else
            var buildPlayerResult = BuildPipeline.BuildPlayer(buildPlayerOptions);
            if (!string.IsNullOrEmpty(buildPlayerResult))
            {
                // Check for intended build cancellation.
                if (buildPlayerResult != "Building Player was cancelled")
                {
                    LogError(buildPlayerResult);
                }

                return;
            }
#endif

            var arguments = string.Format("-jar {0} run {1}", GetAiaJarPath(), tempFilePath);
            var window = CommandLineDialog.CreateCommandLineDialog("Install and run app");
            window.modal = false;
            window.summaryText = "Installing app on device";
            window.autoScrollToBottom = true;
            window.RunAsync(JavaUtilities.JavaBinaryPath, arguments,
                result =>
                {
                    if (result.exitCode == 0)
                    {
                        window.Close();
                    }
                    else
                    {
                        window.noText = "Close";
                        // After adding the button we need to scroll down a little more.
                        window.scrollPosition.y = Mathf.Infinity;
                        window.Repaint();
                    }
                }, envVars: AndroidSdkManager.AndroidHomeEnvironment, maxProgressLines: 5);
            window.Show();
        }

        private static BuildPlayerOptions CreateBuildPlayerOptions(string apkPath)
        {
            var scenes = new List<string>();
            for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                // Skip scenes that are blank/deleted
                var scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                if (!string.IsNullOrEmpty(scenePath))
                {
                    scenes.Add(scenePath);
                }
            }

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = EditorUserBuildSettings.development ? BuildOptions.Development : BuildOptions.None
            };
            return buildPlayerOptions;
        }

        private static string GetAiaJarPath()
        {
            // TODO: switch to $ANDROID_HOME/extras/google/instantapps once CLI is launched
            return "Assets/plugin-unity/PlayInstant/Editor/aia_tools/aia.jar";
        }

        private static void LogError(string message)
        {
            Debug.LogErrorFormat("Build and Run error: {0}", message);
            EditorUtility.DisplayDialog("Build and Run Error", message, "OK");
        }
    }
}