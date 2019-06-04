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

// In the Unity 2017 series the EditorUserBuildSettings.buildAppBundle field was introduced in 2017.4.17.
// It might seem preferable to modify buildAppBundle using reflection, but the field is extern.
// Instead check for quite a few versions in the 2017.4.17+ series.
// NOTE: this supports up to UNITY_2017_4_40 and will have to be extended if additional versions are released.
#if UNITY_2018_3_OR_NEWER || UNITY_2017_4_17 || UNITY_2017_4_18 || UNITY_2017_4_19 || UNITY_2017_4_20 || UNITY_2017_4_21 || UNITY_2017_4_22 || UNITY_2017_4_23 || UNITY_2017_4_24 || UNITY_2017_4_25 || UNITY_2017_4_26 || UNITY_2017_4_27 || UNITY_2017_4_28 || UNITY_2017_4_29 || UNITY_2017_4_30 || UNITY_2017_4_31 || UNITY_2017_4_32 || UNITY_2017_4_33 || UNITY_2017_4_34 || UNITY_2017_4_35 || UNITY_2017_4_36 || UNITY_2017_4_37 || UNITY_2017_4_38 || UNITY_2017_4_39 || UNITY_2017_4_40
#define PLAY_INSTANT_HAS_NATIVE_ANDROID_APP_BUNDLE
#endif

using UnityEditor;
using UnityEngine;

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// Helper to build <a href="https://developer.android.com/platform/technology/app-bundle/">Android App Bundle</a>
    /// files suitable for publishing on Play Console.
    /// </summary>
    public static class AppBundlePublisher
    {
        /// <summary>
        /// Builds an Android App Bundle at a user-specified file location.
        /// </summary>
        public static void Build()
        {
#if !PLAY_INSTANT_ENABLE_NATIVE_ANDROID_APP_BUNDLE
            if (!AndroidAssetPackagingTool.CheckConvert())
            {
                return;
            }

            if (!Bundletool.CheckBundletool())
            {
                return;
            }
#endif

            if (!PlayInstantBuilder.CheckBuildAndPublishPrerequisites())
            {
                return;
            }

            // TODO: add checks for preferred Scripting Backend and Target Architectures.

            var aabFilePath = EditorUtility.SaveFilePanel("Create Android App Bundle", null, null, "aab");
            if (string.IsNullOrEmpty(aabFilePath))
            {
                // Assume cancelled.
                return;
            }

            Build(aabFilePath);
        }

        /// <summary>
        /// Builds an Android App Bundle at the specified location. Assumes that all dependencies are already in-place,
        /// e.g. aapt2 and bundletool.
        /// </summary>
        /// <returns>True if the build succeeded, false if it failed or was cancelled.</returns>
        public static bool Build(string aabFilePath)
        {
            bool buildResult;
            Debug.LogFormat("Building app bundle: {0}", aabFilePath);
            // As of February 2019, every released version of Unity natively supporting AAB has used Android Gradle
            // Plugin 3.2.0 which includes bundletool 0.5.0. Bundletool 0.6.0+ is needed for uncompressNativeLibraries
            // and version 0.6.1+ is needed for uncompressNativeLibraries with instant apps.
            // One can #define PLAY_INSTANT_ENABLE_NATIVE_ANDROID_APP_BUNDLE to build using the native AAB builder.
#if PLAY_INSTANT_ENABLE_NATIVE_ANDROID_APP_BUNDLE
    #if PLAY_INSTANT_HAS_NATIVE_ANDROID_APP_BUNDLE
            EditorUserBuildSettings.buildAppBundle = true;
            var buildPlayerOptions = PlayInstantBuilder.CreateBuildPlayerOptions(aabFilePath, BuildOptions.None);
            buildResult = PlayInstantBuilder.Build(buildPlayerOptions);
    #else
            throw new System.Exception("Cannot enable native app bundle build on an unsupported Unity version.");
    #endif
#else
    #if PLAY_INSTANT_HAS_NATIVE_ANDROID_APP_BUNDLE
            // Disable Unity's built-in AAB build on newer Unity versions before performing the custom AAB build.
            EditorUserBuildSettings.buildAppBundle = false;
            // Note: fall through here to the actual build.
    #endif
            buildResult = AppBundleBuilder.Build(aabFilePath);
#endif
            if (buildResult)
            {
                // Do not log in case of failure. The method we called was responsible for logging.
                Debug.LogFormat("Finished building app bundle: {0}", aabFilePath);
            }

            return buildResult;
        }
    }
}