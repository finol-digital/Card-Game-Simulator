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
using UnityEditor;
using UnityEngine;

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// Helper to build an Android App Bundle file on Unity version 2018.2 and earlier.
    /// </summary>
    public static class AppBundleBuilder
    {
        private const string BaseModuleZipFileName = "base.zip";


        /// <summary>
        /// Build an app bundle at the specified path, overwriting an existing file if one exists.
        /// </summary>
        /// <returns>True if the build succeeded, false if it failed or was cancelled.</returns>
        public static bool Build(string aabFilePath)
        {
            var binaryFormatFilePath = Path.GetTempFileName();
            Debug.LogFormat("Building Package: {0}", binaryFormatFilePath);

            // Do not use BuildAndSign since this signature won't be used.
            if (!PlayInstantBuilder.Build(
                PlayInstantBuilder.CreateBuildPlayerOptions(binaryFormatFilePath, BuildOptions.None)))
            {
                // Do not log here. The method we called was responsible for logging.
                return false;
            }

            // TODO: currently all processing is synchronous; consider moving to a separate thread
            try
            {
                DisplayProgress("Running aapt2", 0.2f);
                var workingDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "play-instant-unity"));
                if (workingDirectory.Exists)
                {
                    workingDirectory.Delete(true);
                }

                workingDirectory.Create();
                var sourceDirectoryInfo = workingDirectory.CreateSubdirectory("source");
                var destinationDirectoryInfo = workingDirectory.CreateSubdirectory("destination");

                var protoFormatFileName = Path.GetRandomFileName();
                var protoFormatFilePath = Path.Combine(sourceDirectoryInfo.FullName, protoFormatFileName);
                var aaptResult = AndroidAssetPackagingTool.Convert(binaryFormatFilePath, protoFormatFilePath);
                if (aaptResult != null)
                {
                    DisplayBuildError("aapt2", aaptResult);
                    return false;
                }

                DisplayProgress("Creating base module", 0.4f);
                var unzipFileResult = ZipUtils.UnzipFile(protoFormatFileName, sourceDirectoryInfo.FullName);
                if (unzipFileResult != null)
                {
                    DisplayBuildError("Unzip", unzipFileResult);
                    return false;
                }

                File.Delete(protoFormatFilePath);

                ArrangeFiles(sourceDirectoryInfo, destinationDirectoryInfo);
                var baseModuleZip = Path.Combine(workingDirectory.FullName, BaseModuleZipFileName);
                var zipFileResult = ZipUtils.CreateZipFile(baseModuleZip, destinationDirectoryInfo.FullName, ".");
                if (zipFileResult != null)
                {
                    DisplayBuildError("Zip creation", zipFileResult);
                    return false;
                }

                // If the .aab file exists, EditorUtility.SaveFilePanel() has already prompted for whether to overwrite.
                // Therefore, prevent Bundletool from throwing an IllegalArgumentException that "File already exists."
                File.Delete(aabFilePath);

                DisplayProgress("Running bundletool", 0.6f);
                var buildBundleResult = Bundletool.BuildBundle(baseModuleZip, aabFilePath);
                if (buildBundleResult != null)
                {
                    DisplayBuildError("bundletool", buildBundleResult);
                    return false;
                }

                DisplayProgress("Signing bundle", 0.8f);
                var signingResult = ApkSigner.SignZip(aabFilePath);
                if (signingResult != null)
                {
                    DisplayBuildError("Signing", signingResult);
                    return false;
                }
            }
            finally
            {
                if (!WindowUtils.IsHeadlessMode())
                {
                    EditorUtility.ClearProgressBar();
                }
            }

            return true;
        }

        /// <summary>
        /// Arrange files according to the <a href="https://developer.android.com/guide/app-bundle/#aab_format">
        /// Android App Bundle format</a>.
        /// </summary>
        private static void ArrangeFiles(DirectoryInfo source, DirectoryInfo destination)
        {
            foreach (var sourceFileInfo in source.GetFiles())
            {
                DirectoryInfo destinationSubdirectory;
                var fileName = sourceFileInfo.Name;
                if (fileName == "AndroidManifest.xml")
                {
                    destinationSubdirectory = destination.CreateSubdirectory("manifest");
                }
                else if (fileName == "resources.pb")
                {
                    destinationSubdirectory = destination;
                }
                else if (fileName.EndsWith("dex"))
                {
                    destinationSubdirectory = destination.CreateSubdirectory("dex");
                }
                else
                {
                    destinationSubdirectory = destination.CreateSubdirectory("root");
                }

                sourceFileInfo.MoveTo(Path.Combine(destinationSubdirectory.FullName, fileName));
            }

            foreach (var sourceDirectoryInfo in source.GetDirectories())
            {
                var directoryName = sourceDirectoryInfo.Name;
                switch (directoryName)
                {
                    case "META-INF":
                        // Skip files like MANIFEST.MF
                        break;
                    case "assets":
                    case "lib":
                    case "res":
                        sourceDirectoryInfo.MoveTo(Path.Combine(destination.FullName, directoryName));
                        break;
                    default:
                        var subdirectory = destination.CreateSubdirectory("root");
                        sourceDirectoryInfo.MoveTo(Path.Combine(subdirectory.FullName, directoryName));
                        break;
                }
            }
        }

        private static void DisplayProgress(string info, float progress)
        {
            Debug.LogFormat("{0}...", info);
            if (!WindowUtils.IsHeadlessMode())
            {
                EditorUtility.DisplayProgressBar("Building App Bundle", info, progress);
            }
        }

        private static void DisplayBuildError(string errorType, string errorMessage)
        {
            if (!WindowUtils.IsHeadlessMode())
            {
                EditorUtility.ClearProgressBar();
            }

            PlayInstantBuilder.DisplayBuildError(string.Format("{0} failed: {1}", errorType, errorMessage));
        }
    }
}