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

using UnityEngine;
using UnityEngine.Networking;

#if !UNITY_2017_2_OR_NEWER
using System.IO;
#endif

namespace GooglePlayInstant
{
    /// <summary>
    /// Methods and constants that are specific to Google Play Instant and/or the plugin.
    /// </summary>
    public static class GooglePlayInstantUtils
    {
        /// <summary>
        /// The version of the Google Play Instant Unity Plugin.
        /// </summary>
        public const string PluginVersion = "0.10";

        /// <summary>
        /// Return true if this is an instant app build, false if an installed app build.
        /// This is an alternative to checking "#if PLAY_INSTANT" directly.
        /// </summary>
        public static bool IsInstantApp()
        {
#if PLAY_INSTANT
            return true;
#else
            return false;
#endif
        }

        /// <summary>
        /// Initiates the specified UnityWebRequest and returns an AsyncOperation.
        /// </summary>
        public static AsyncOperation SendWebRequest(UnityWebRequest request)
        {
#if UNITY_2017_2_OR_NEWER
            return request.SendWebRequest();
#else
            return request.Send();
#endif
        }

        /// <summary>
        /// Starts a file download from the specified URL to the specified file path, returning a
        /// UnityWebRequest representing the in-flight request.
        /// </summary>
        public static UnityWebRequest StartFileDownload(string url, string fileSavePath)
        {
#if UNITY_2017_2_OR_NEWER
            var downloadHandler = new DownloadHandlerFile(fileSavePath)
            {
                removeFileOnAbort = true
            };
            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET, downloadHandler, null);
#else
            var request = UnityWebRequest.Get(url);
#endif
            SendWebRequest(request);
            return request;
        }

        /// <summary>
        /// Finishes saving the file whose download was initiated with <see cref="StartFileDownload"/>.
        /// </summary>
        public static void FinishFileDownload(UnityWebRequest request, string fileSavePath)
        {
#if !UNITY_2017_2_OR_NEWER
            File.WriteAllBytes(fileSavePath, request.downloadHandler.data);
#endif
        }

        /// <summary>
        /// Returns true if the specified request has encountered an error, and false otherwise.
        /// </summary>
        public static bool IsNetworkError(UnityWebRequest request)
        {
#if UNITY_2017_1_OR_NEWER
            return request.isHttpError || request.isNetworkError;
#else
            return request.isError;
#endif
        }
    }
}