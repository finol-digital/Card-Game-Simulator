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

using System;
using System.Linq;
using GooglePlayInstant.Editor.AndroidManifest;
using UnityEditor;
using UnityEngine;

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// A window for managing settings related to building an instant app, e.g. the instant app's URL.
    /// </summary>
    public class BuildSettingsWindow : EditorWindow
    {
        public const string WindowTitle = "Play Instant Build Settings";
        private const string InstantAppsHostName = "instant.apps";
        private const int WindowMinWidth = 475;
        private const int WindowMinHeight = 400;
        private const int FieldWidth = 175;
        private static readonly string[] PlatformOptions = {"Installed", "Instant"};

        private readonly IAndroidManifestUpdater _androidManifestUpdater =
#if UNITY_2018_1_OR_NEWER
            new PostGenerateGradleProjectAndroidManifestUpdater();
#else
            new LegacyAndroidManifestUpdater();
#endif
        private static BuildSettingsWindow _windowInstance;

        private bool _isInstant;
        private string _instantUrl;
        private string _scenesInBuild;
        private string _assetBundleManifestPath;

        /// <summary>
        /// Displays this window, creating it if necessary.
        /// </summary>
        public static void ShowWindow()
        {
            _windowInstance = (BuildSettingsWindow) GetWindow(typeof(BuildSettingsWindow), true, WindowTitle);
            _windowInstance.minSize = new Vector2(WindowMinWidth, WindowMinHeight);
        }

        private void OnDestroy()
        {
            _windowInstance = null;
        }

        private void Awake()
        {
            ReadFromBuildConfiguration();
        }

        /// <summary>
        /// Read and update the window with most recent build configuration values.
        /// </summary>
        void ReadFromBuildConfiguration()
        {
            _isInstant = PlayInstantBuildConfiguration.IsInstantBuildType();
            _instantUrl = PlayInstantBuildConfiguration.InstantUrl;
            _scenesInBuild = GetScenesInBuildAsString(PlayInstantBuildConfiguration.ScenesInBuild);
            _assetBundleManifestPath = PlayInstantBuildConfiguration.AssetBundleManifestPath;
        }

        /// <summary>
        /// Update window with most recent build configuration values if the window is open.
        /// </summary>
        public static void UpdateWindowIfOpen()
        {
            if (_windowInstance != null)
            {
                _windowInstance.ReadFromBuildConfiguration();
                _windowInstance.Repaint();
            }
        }

        private void OnGUI()
        {
            // Edge case that takes place when the plugin code gets re-compiled while this window is open.
            if (_windowInstance == null)
            {
                _windowInstance = this;
            }

            var descriptionTextStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Italic,
                wordWrap = true
            };

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Android Build Type", EditorStyles.boldLabel, GUILayout.Width(FieldWidth));
            var index = EditorGUILayout.Popup(_isInstant ? 1 : 0, PlatformOptions);
            _isInstant = index == 1;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            if (_isInstant)
            {
                _instantUrl = GetLabelAndTextField("Instant Apps URL (Optional)", _instantUrl);

                var packageName = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android) ?? "package-name";
                EditorGUILayout.LabelField(
                    "Instant apps are launched from web search, advertisements, etc via a URL. Specify the URL here " +
                    "and configure Digital Asset Links. Or, leave the URL blank and one will automatically be " +
                    "provided at:", descriptionTextStyle);
                EditorGUILayout.SelectableLabel(string.Format(
                    "https://{0}/{1}", InstantAppsHostName, packageName), descriptionTextStyle);
                EditorGUILayout.Space();
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Scenes in Build", EditorStyles.boldLabel);
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(
                    "The scenes in the build are selected via Unity's \"Build Settings\" window. " +
                    "This can be overridden by specifying a comma separated scene list below.", descriptionTextStyle);
                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();
                var defaultScenes = string.IsNullOrEmpty(_scenesInBuild)
                    ? string.Join(", ", PlayInstantBuilder.GetEditorBuildEnabledScenes())
                    : "(overridden)";
                EditorGUILayout.LabelField(
                    string.Format("\"Build Settings\" Scenes: {0}", defaultScenes), EditorStyles.wordWrappedLabel);
                if (GUILayout.Button("Update", GUILayout.Width(100)))
                {
                    GetWindow(Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"), true);
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();

                _scenesInBuild = GetLabelAndTextField("Override Scenes (Optional)", _scenesInBuild);

                _assetBundleManifestPath =
                    GetLabelAndTextField("AssetBundle Manifest (Optional)", _assetBundleManifestPath);

                EditorGUILayout.LabelField(
                    "If you use AssetBundles, provide the path to your AssetBundle Manifest file to ensure that " +
                    "required types are not stripped during the build process.", descriptionTextStyle);
            }
            else
            {
                EditorGUILayout.LabelField(
                    "The \"Installed\" build type is used when creating a traditional installed APK. " +
                    "Select \"Instant\" to build a Google Play Instant APK.", descriptionTextStyle);
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            // Disable the Save button unless one of the fields has changed.
            GUI.enabled = IsAnyFieldChanged();

            if (GUILayout.Button("Save"))
            {
                if (_isInstant)
                {
                    SelectPlatformInstant();
                }
                else
                {
                    SelectPlatformInstalled();
                }
            }

            GUI.enabled = true;
        }

        private bool IsAnyFieldChanged()
        {
            if (_isInstant)
            {
                return !PlayInstantBuildConfiguration.IsInstantBuildType() ||
                       _instantUrl != PlayInstantBuildConfiguration.InstantUrl ||
                       _scenesInBuild != GetScenesInBuildAsString(PlayInstantBuildConfiguration.ScenesInBuild) ||
                       _assetBundleManifestPath != PlayInstantBuildConfiguration.AssetBundleManifestPath;
            }

            // If changing the build type to "Installed", then we don't care about the other fields
            return PlayInstantBuildConfiguration.IsInstantBuildType();
        }

        private static string GetLabelAndTextField(string label, string text)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(FieldWidth));
            var result = EditorGUILayout.TextField(text);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            return result;
        }

        private void SelectPlatformInstant()
        {
            string instantUrlError;
            var uri = GetInstantUri(_instantUrl, out instantUrlError);
            if (instantUrlError != null)
            {
                Debug.LogErrorFormat("Invalid URL: {0}", instantUrlError);
                EditorUtility.DisplayDialog("Invalid URL", instantUrlError, WindowUtils.OkButtonText);
                return;
            }

            // The URL is valid, so save any clean-ups performed by conversion through Uri, e.g. HTTPS->https.
            _instantUrl = uri == null ? string.Empty : uri.ToString();

            var errorMessage = _androidManifestUpdater.SwitchToInstant(uri);
            if (errorMessage != null)
            {
                var message = string.Format("Error updating AndroidManifest.xml: {0}", errorMessage);
                Debug.LogError(message);
                EditorUtility.DisplayDialog("Error Saving", message, WindowUtils.OkButtonText);
                return;
            }

            var scenesInBuild =
                _scenesInBuild.Split(',').Where(s => s.Trim().Length > 0).Select(s => s.Trim()).ToArray();
            _scenesInBuild = GetScenesInBuildAsString(scenesInBuild);
            _assetBundleManifestPath = _assetBundleManifestPath.Trim();
            PlayInstantBuildConfiguration.SaveConfiguration(_instantUrl, scenesInBuild, _assetBundleManifestPath);
            PlayInstantBuildConfiguration.SetInstantBuildType();
            Debug.Log("Saved Play Instant Build Settings.");

            // If a TextField is in focus, it won't update to reflect the Trim(). So reassign focus to controlID 0.
            GUIUtility.keyboardControl = 0;
            Repaint();
        }

        private static string GetScenesInBuildAsString(string[] scenesInBuild)
        {
            return string.Join(",", scenesInBuild);
        }

        private void SelectPlatformInstalled()
        {
            PlayInstantBuildConfiguration.SetInstalledBuildType();
            _androidManifestUpdater.SwitchToInstalled();
            Debug.Log("Switched to Android Build Type \"Installed\".");
        }

        // Visible for testing.
        /// <summary>
        /// Checks whether the specified URL is valid, and if so returns it as a <see cref="Uri"/>. If the specified
        /// URL is null or a blank string, this method returns null and doesn't set an error message. If the
        /// specified URL is invalid, this method returns null and sets the reason as an out parameter.
        /// </summary>
        internal static Uri GetInstantUri(string instantUrl, out string instantUrlError)
        {
            instantUrl = instantUrl == null ? string.Empty : instantUrl.Trim();
            if (instantUrl.Length == 0)
            {
                instantUrlError = null;
                return null;
            }

            Uri uri;
            try
            {
                uri = new Uri(instantUrl);
            }
            catch (Exception ex)
            {
                instantUrlError = string.Format("The URL is invalid: {0}", ex.Message);
                return null;
            }

            if (uri.Scheme.ToLower() != "https")
            {
                instantUrlError = "The URL scheme should be \"https\"";
                return null;
            }

            if (uri.Host.ToLower() == InstantAppsHostName)
            {
                instantUrlError =
                    string.Format(
                        "Leave \"Instant Apps URL\" blank to get the automatic URL https://{0}/package",
                        InstantAppsHostName);
                return null;
            }

            instantUrlError = null;
            return uri;
        }
    }
}