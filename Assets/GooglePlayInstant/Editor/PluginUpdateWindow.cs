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
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// Window that checks for the latest plugin available on GitHub and provides buttons to save or install the
    /// plugin. This uses the <a href="https://developer.github.com/v3/repos/releases/">GitHub Release API</a>.
    /// </summary>
    public class PluginUpdateWindow : EditorWindow
    {
        private const int WindowMinWidth = 425;
        private const int WindowMinHeight = 350;

        private const string LatestReleaseQueryUrl =
            "https://api.github.com/repos/google/play-instant-unity-plugin/releases/latest";

        private const string PluginReleasesButtonText = "Plugin Releases";
        private const string CheckReleasesButtonText = "Check Releases";
        private const string DownloadAndSaveButtonText = "Save Plugin...";
        private const string DownloadAndInstallButtonText = "Install Plugin";

        private UnityWebRequest _versionCheckRequest;
        private UnityWebRequest _pluginDownloadRequest;

        private string _latestReleaseVersion;
        private string _latestReleaseDownloadUrl;
        private string _latestReleaseSavePath;
        private bool _shouldInstallPlugin;

        private void Awake()
        {
            StartVersionCheck();
        }

        private void OnGUI()
        {
            GUI.enabled = _versionCheckRequest == null && _pluginDownloadRequest == null;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(string.Format(
                "This dialog checks for the latest released version of this plugin. " +
                "Click the \"{0}\" button to open the GitHub page with a list of all plugin versions.",
                PluginReleasesButtonText), EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();
            WindowUtils.CreateRightAlignedButton(PluginReleasesButtonText,
                () => { Application.OpenURL("https://github.com/google/play-instant-unity-plugin/releases"); });

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(string.Format("Current version: {0}", GooglePlayInstantUtils.PluginVersion));
            EditorGUILayout.LabelField(string.Format("Latest version:  {0}",
                GetNormalizedVersion(_latestReleaseVersion) ?? "unknown"));
            EditorGUILayout.LabelField(string.Format("Latest package:  {0}",
                GetLatestReleasePluginFileName() ?? "unknown"), EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();
            WindowUtils.CreateRightAlignedButton(CheckReleasesButtonText, StartVersionCheck);

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            GUI.enabled &= _latestReleaseVersion != null;

            EditorGUILayout.LabelField("Download options", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(string.Format(
                "Either click \"{0}\" to download and save the latest plugin to disk, " +
                " or click \"{1}\" to download and install the latest plugin.",
                DownloadAndSaveButtonText, DownloadAndInstallButtonText), EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();
            WindowUtils.CreateRightAlignedButton(DownloadAndSaveButtonText, StartPluginDownloadAndSave);
            EditorGUILayout.Space();
            WindowUtils.CreateRightAlignedButton(DownloadAndInstallButtonText, StartPluginDownloadAndInstall);

            GUI.enabled = true;
        }

        private void Update()
        {
            if (_versionCheckRequest != null && _pluginDownloadRequest != null)
            {
                throw new Exception("A version check request and plugin request are simultaneously in-flight.");
            }

            if (_versionCheckRequest != null && _versionCheckRequest.isDone)
            {
                EditorUtility.ClearProgressBar();
                HandleVersionCheckRequestIsDone();
                _versionCheckRequest.Dispose();
                _versionCheckRequest = null;
                Repaint();
                return;
            }

            if (_pluginDownloadRequest != null && _pluginDownloadRequest.isDone)
            {
                EditorUtility.ClearProgressBar();
                HandlePluginDownloadRequestIsDone();
                _pluginDownloadRequest.Dispose();
                _pluginDownloadRequest = null;
                Repaint();
                return;
            }

            // If a web request is in-flight, display a progress bar.
            DisplayCancelableProgressBarIfNecessary(ref _versionCheckRequest, "Checking Version");
            DisplayCancelableProgressBarIfNecessary(ref _pluginDownloadRequest, "Downloading Plugin");
        }

        private void OnDestroy()
        {
            if (_versionCheckRequest != null)
            {
                _versionCheckRequest.Dispose();
                _versionCheckRequest = null;
            }

            if (_pluginDownloadRequest != null)
            {
                _pluginDownloadRequest.Dispose();
                _pluginDownloadRequest = null;
            }
        }

        private void StartVersionCheck()
        {
            Debug.Log("Checking for latest plugin version...");
            _latestReleaseVersion = null;
            _latestReleaseDownloadUrl = null;
            _versionCheckRequest = UnityWebRequest.Get(LatestReleaseQueryUrl);
            GooglePlayInstantUtils.SendWebRequest(_versionCheckRequest);
        }

        private void StartPluginDownload()
        {
            Debug.LogFormat("Downloading the latest plugin: {0}", _latestReleaseSavePath);
            _pluginDownloadRequest =
                GooglePlayInstantUtils.StartFileDownload(_latestReleaseDownloadUrl, _latestReleaseSavePath);
        }

        private void StartPluginDownloadAndSave()
        {
            _shouldInstallPlugin = false;
            _latestReleaseSavePath = EditorUtility.SaveFilePanel(
                "Save Plugin Package", null, GetLatestReleasePluginFileName(), "unitypackage");
            if (string.IsNullOrEmpty(_latestReleaseSavePath))
            {
                // Assume cancelled.
                return;
            }

            StartPluginDownload();
        }

        private void StartPluginDownloadAndInstall()
        {
            _shouldInstallPlugin = true;
            _latestReleaseSavePath = Path.GetTempFileName();
            StartPluginDownload();
        }

        private void HandleVersionCheckRequestIsDone()
        {
            if (GooglePlayInstantUtils.IsNetworkError(_versionCheckRequest))
            {
                HandleVersionCheckFailed(_versionCheckRequest.error, null);
                return;
            }

            var responseText = _versionCheckRequest.downloadHandler.text;
            LatestReleaseResponse latestReleaseResponse;
            try
            {
                latestReleaseResponse = JsonUtility.FromJson<LatestReleaseResponse>(responseText);
            }
            catch (ArgumentException ex)
            {
                HandleVersionCheckFailed(ex.Message, responseText);
                return;
            }

            if (latestReleaseResponse == null)
            {
                HandleVersionCheckFailed("Response missing.", responseText);
                return;
            }

            if (string.IsNullOrEmpty(latestReleaseResponse.tag_name))
            {
                HandleVersionCheckFailed("Latest release version is missing.", responseText);
                return;
            }

            var latestReleaseAssets = latestReleaseResponse.assets;
            if (latestReleaseAssets == null)
            {
                HandleVersionCheckFailed("Latest release assets are null.", responseText);
                return;
            }

            if (latestReleaseAssets.Length == 0)
            {
                HandleVersionCheckFailed("Latest release assets are empty.", responseText);
                return;
            }

            if (latestReleaseAssets.Length > 1)
            {
                HandleVersionCheckFailed(string.Format(
                    "Latest release unexpectedly has {0} asset files.", latestReleaseAssets.Length), responseText);
                return;
            }

            var latestReleaseAsset = latestReleaseAssets[0];
            if (latestReleaseAsset == null)
            {
                HandleVersionCheckFailed("Latest release asset is null.", responseText);
                return;
            }

            if (string.IsNullOrEmpty(latestReleaseAsset.browser_download_url))
            {
                HandleVersionCheckFailed("Latest release download URL is missing.", responseText);
                return;
            }

            _latestReleaseVersion = latestReleaseResponse.tag_name;
            _latestReleaseDownloadUrl = latestReleaseAsset.browser_download_url;

            Debug.LogFormat("Plugin version check result: current={0} latest={1} url={2}",
                GooglePlayInstantUtils.PluginVersion, _latestReleaseVersion, _latestReleaseDownloadUrl);
        }

        private void HandleVersionCheckFailed(string versionCheckError, string additionalErrorText)
        {
            if (additionalErrorText == null)
            {
                Debug.LogError(versionCheckError);
            }
            else
            {
                Debug.LogErrorFormat("{0}: {1}", versionCheckError, additionalErrorText);
            }

            var message = string.Format("{0}\n\nClick \"{1}\" to retry.", versionCheckError, CheckReleasesButtonText);
            EditorUtility.DisplayDialog("Version Check Failed", message, WindowUtils.OkButtonText);
        }

        private void HandlePluginDownloadRequestIsDone()
        {
            if (GooglePlayInstantUtils.IsNetworkError(_pluginDownloadRequest))
            {
                var downloadRequestError = _pluginDownloadRequest.error;
                Debug.LogErrorFormat("Plugin download error: {0}", downloadRequestError);
                EditorUtility.DisplayDialog("Download Failed", downloadRequestError, WindowUtils.OkButtonText);
                return;
            }

            GooglePlayInstantUtils.FinishFileDownload(_pluginDownloadRequest, _latestReleaseSavePath);

            Debug.LogFormat("Plugin downloaded: {0}", _latestReleaseSavePath);
            if (_shouldInstallPlugin)
            {
                AssetDatabase.ImportPackage(_latestReleaseSavePath, true);
            }
            else
            {
                var message = string.Format(
                    "The plugin has been downloaded: {0}\n\nClick \"{1}\" to locate the file in the {2}.",
                    _latestReleaseSavePath, WindowUtils.OkButtonText,
                    Application.platform == RuntimePlatform.WindowsEditor ? "file explorer" : "finder");
                if (EditorUtility.DisplayDialog(
                    "Download Complete", message, WindowUtils.OkButtonText, WindowUtils.CancelButtonText))
                {
                    EditorUtility.RevealInFinder(_latestReleaseSavePath);
                }

                EditorApplication.delayCall += Close;
            }
        }

        private static string GetNormalizedVersion(string version)
        {
            return !string.IsNullOrEmpty(version) && version[0] == 'v' ? version.Remove(0, 1) : version;
        }

        private string GetLatestReleasePluginFileName()
        {
            return string.IsNullOrEmpty(_latestReleaseDownloadUrl) ? null : Path.GetFileName(_latestReleaseDownloadUrl);
        }

        private static void DisplayCancelableProgressBarIfNecessary(ref UnityWebRequest request, string progressTitle)
        {
            if (request == null)
            {
                return;
            }

            if (EditorUtility.DisplayCancelableProgressBar(progressTitle, null, request.downloadProgress))
            {
                EditorUtility.ClearProgressBar();
                request.Abort();
                request.Dispose();
                request = null;
                Debug.LogFormat("Cancelled request: {0}", progressTitle);
            }
        }

        /// <summary>
        /// Displays this window, creating it if necessary.
        /// </summary>
        public static void ShowWindow()
        {
            var window = GetWindow(typeof(PluginUpdateWindow), true, "Plugin Update Check");
            window.minSize = new Vector2(WindowMinWidth, WindowMinHeight);
        }

        // Classes used for deserializing a JSON response based on https://developer.github.com/v3/repos/releases/
        // Since these fields are only set by JsonUtility.FromJson(), we need to explicitly disable warning CS0649.
#pragma warning disable CS0649
        [Serializable]
        private class LatestReleaseResponse
        {
            public string tag_name;

            public LatestReleaseAssets[] assets;
        }

        [Serializable]
        private class LatestReleaseAssets
        {
            public string browser_download_url;
        }
#pragma warning restore CS0649
    }
}