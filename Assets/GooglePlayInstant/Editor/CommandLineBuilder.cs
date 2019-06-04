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
using GooglePlayInstant.Editor.AndroidManifest;
using UnityEditor;

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// Provides methods for building from the command line.
    /// </summary>
    public static class CommandLineBuilder
    {
        private const string ApkPathArg = "-outputFile";

        /// <summary>
        /// Configures the project to prepare for building the specified scenes in an instant app.
        /// </summary>
        public static void ConfigureProject(string[] scenesInBuild)
        {
            var requiredPolicies = PlayInstantSettingPolicy.GetRequiredPolicies();
            foreach (var policy in requiredPolicies)
            {
                var policyChangeCompleted = policy.ChangeState();
                if (!policyChangeCompleted)
                {
                    throw new Exception(string.Format("Failed to change policy: {0}", policy.Name));
                }
            }

            SetTargetArchitectures();

            var manifestUpdater = AndroidManifestHelper.GetAndroidManifestUpdater();
            var errorMessage = manifestUpdater.SwitchToInstant(null);
            if (errorMessage != null)
            {
                throw new Exception(string.Format("Error updating AndroidManifest.xml: {0}", errorMessage));
            }

            PlayInstantBuildConfiguration.AddScriptingDefineSymbol(
                PlaySignatureVerifier.SkipVerifyGooglePlayServicesScriptingDefineSymbol);
            PlayInstantBuildConfiguration.SaveConfiguration("", scenesInBuild, "");
            PlayInstantBuildConfiguration.SetInstantBuildType();
        }

        /// <summary>
        /// Gets the apk path passed in via command line.
        /// </summary>
        public static string GetApkPath()
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == ApkPathArg)
                {
                    return args[i + 1];
                }
            }

            throw new Exception(string.Format("Missing required argument \"{0}\"", ApkPathArg));
        }

        private static void SetTargetArchitectures()
        {
            // Unity 2017.4.16 added support for ARM64. There is no UNITY_2017_4_16_OR_NEWER, so we make a best effort.
            // NOTE: this supports up to UNITY_2017_4_40 and will have to be extended if more versions are released.
#if UNITY_2018_1_OR_NEWER || UNITY_2017_4_16 || UNITY_2017_4_17 || UNITY_2017_4_18 || UNITY_2017_4_19 || UNITY_2017_4_20 || UNITY_2017_4_21 || UNITY_2017_4_22 || UNITY_2017_4_23 || UNITY_2017_4_24 || UNITY_2017_4_25 || UNITY_2017_4_26 || UNITY_2017_4_27 || UNITY_2017_4_28 || UNITY_2017_4_29 || UNITY_2017_4_30 || UNITY_2017_4_31 || UNITY_2017_4_32 || UNITY_2017_4_33 || UNITY_2017_4_34 || UNITY_2017_4_35 || UNITY_2017_4_36 || UNITY_2017_4_37 || UNITY_2017_4_38 || UNITY_2017_4_39 || UNITY_2017_4_40
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.X86 | AndroidArchitecture.ARMv7;
#else
            PlayerSettings.Android.targetDevice = AndroidTargetDevice.FAT;
#endif
        }
    }
}