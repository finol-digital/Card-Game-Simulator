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
using UnityEngine.Networking;

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// Downloads <a href="https://developer.android.com/studio/command-line/bundletool">bundletool</a>.
    /// </summary>
    public class BundletoolDownloadWindow : EditorWindow
    {
        private UnityWebRequest _downloadRequest;

        private void OnGUI()
        {
            GUI.enabled = _downloadRequest == null;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "Bundletool is a command line java program used for creating Android App Bundles (.aab files). " +
                "Bundletool is also used to generate a set of APKs from an .aab file.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();
            WindowUtils.CreateRightAlignedButton("Learn more",
                () => { Application.OpenURL("https://developer.android.com/studio/command-line/bundletool"); });

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(string.Format("Click \"Download\" to download bundletool version {0}.",
                Bundletool.BundletoolVersion), EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();
            WindowUtils.CreateRightAlignedButton("Download", StartDownload);

            GUI.enabled = true;
        }

        private void Update()
        {
            if (_downloadRequest == null)
            {
                return;
            }

            if (_downloadRequest.isDone)
            {
                EditorUtility.ClearProgressBar();

                if (GooglePlayInstantUtils.IsNetworkError(_downloadRequest))
                {
                    var downloadRequestError = _downloadRequest.error;
                    _downloadRequest.Dispose();
                    _downloadRequest = null;

                    Debug.LogErrorFormat("Bundletool download error: {0}", downloadRequestError);
                    if (EditorUtility.DisplayDialog("Download Failed",
                        string.Format("{0}\n\nClick \"{1}\" to retry.", downloadRequestError, WindowUtils.OkButtonText),
                        WindowUtils.OkButtonText,
                        WindowUtils.CancelButtonText))
                    {
                        StartDownload();
                    }
                    else
                    {
                        EditorApplication.delayCall += Close;
                    }

                    return;
                }

                // Download succeeded.
                var bundletoolJarPath = Bundletool.BundletoolJarPath;
                GooglePlayInstantUtils.FinishFileDownload(_downloadRequest, bundletoolJarPath);
                _downloadRequest.Dispose();
                _downloadRequest = null;

                Debug.LogFormat("Bundletool downloaded: {0}", bundletoolJarPath);
                var message = string.Format(
                    "Bundletool has been downloaded to your project's \"Library\" directory: {0}", bundletoolJarPath);
                if (EditorUtility.DisplayDialog("Download Complete", message, WindowUtils.OkButtonText))
                {
                    EditorApplication.delayCall += Close;
                }

                return;
            }

            // Download is in progress.
            if (EditorUtility.DisplayCancelableProgressBar(
                "Downloading bundletool", null, _downloadRequest.downloadProgress))
            {
                EditorUtility.ClearProgressBar();
                _downloadRequest.Abort();
                _downloadRequest.Dispose();
                _downloadRequest = null;
                Debug.Log("Cancelled bundletool download.");
            }
        }

        private void OnDestroy()
        {
            if (_downloadRequest != null)
            {
                _downloadRequest.Dispose();
                _downloadRequest = null;
            }
        }

        private void StartDownload()
        {
            Debug.Log("Downloading bundletool...");
            _downloadRequest =
                GooglePlayInstantUtils.StartFileDownload(
                    Bundletool.BundletoolDownloadUrl, Bundletool.BundletoolJarPath);
        }

        /// <summary>
        /// Displays this window, creating it if necessary.
        /// </summary>
        public static void ShowWindow()
        {
            GetWindow(typeof(BundletoolDownloadWindow), true, "Bundletool Download Required");
        }
    }
}