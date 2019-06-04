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

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// Helper for running "aapt2 convert".
    /// </summary>
    public static class AndroidAssetPackagingTool
    {
        /// <summary>
        /// Minimum version of Android SDK Build-Tools where aapt2 supports the "convert" command.
        /// </summary>
        private const string BuildToolsMinimumVersion = "28.0.0";

        /// <summary>
        /// Latest version of Android SDK Build-Tools.
        /// </summary>
        private const string BuildToolsLatestVersion = "28.0.3";

        private const string BuildToolsDisplayName = "Android SDK Build-Tools";
        private const string BuildToolsPackageName = "build-tools;" + BuildToolsLatestVersion;

        /// <summary>
        /// Checks the latest version of Android SDK Build-Tools that is installed, prompting to upgrade if necessary.
        /// Returns true if the latest version of Android SDK Build-Tools supports the "aapt2 convert" command,
        /// and false otherwise.
        /// </summary>
        public static bool CheckConvert()
        {
            var newestBuildToolsVersion = AndroidBuildTools.GetNewestBuildToolsVersion();
            if (newestBuildToolsVersion == null)
            {
                PlayInstantBuilder.DisplayBuildError(string.Format("Failed to locate {0}", BuildToolsDisplayName));
                return false;
            }

            if (AndroidBuildTools.IsBuildToolsVersionAtLeast(newestBuildToolsVersion, BuildToolsMinimumVersion))
            {
                return true;
            }

            var message = string.Format(
                "App Bundle creation requires {0} version {1} or later.\n\nClick \"OK\" to install {0} version {2}.",
                BuildToolsDisplayName, BuildToolsMinimumVersion, BuildToolsLatestVersion);
            if (PlayInstantBuilder.DisplayBuildErrorDialog(message))
            {
                AndroidSdkPackageInstaller.InstallPackage(BuildToolsPackageName, BuildToolsDisplayName);
            }

            return false;
        }

        /// <summary>
        /// Given the specified APK, produces a new APK where resources.arsc is converted to proto format.
        /// </summary>
        /// <returns>An error message if there was a problem running aapt2, or null if successful.</returns>
        public static string Convert(string inputPath, string outputPath)
        {
            var arguments = string.Format(
                "convert -o {0} --output-format proto {1}",
                CommandLine.QuotePath(outputPath),
                CommandLine.QuotePath(inputPath));
            var result = CommandLine.Run(GetAapt2Path(), arguments);
            return result.exitCode == 0 ? null : result.message;
        }

        private static string GetAapt2Path()
        {
            var newestBuildToolsVersion = AndroidBuildTools.GetNewestBuildToolsVersion();
            var newestBuildToolsPath = Path.Combine(AndroidBuildTools.GetBuildToolsPath(), newestBuildToolsVersion);
            return Path.Combine(newestBuildToolsPath, "aapt2");
        }
    }
}