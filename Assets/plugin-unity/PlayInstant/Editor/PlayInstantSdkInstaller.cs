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
using GooglePlayServices;
using UnityEditor;
using UnityEngine;

namespace PlayInstant.Editor
{
    public static class PlayInstantSdkInstaller
    {
        private const string InstantAppsSdkManagerPackageName = "extras;google;instantapps";

        public static void SetUp()
        {
            AndroidSdkManager.Create(manager =>
            {
                manager.QueryPackages(collection =>
                {
                    var package = collection.GetMostRecentAvailablePackage(InstantAppsSdkManagerPackageName);
                    if (package == null)
                    {
                        ShowMessage("Unable to locate the Play Instant SDK package");
                        return;
                    }

                    if (package.Installed)
                    {
                        ShowMessage(string.Format(
                            "The Play Instant SDK package is already installed at the latest available version ({0})",
                            package.VersionString));
                        return;
                    }

                    var packages = new HashSet<AndroidSdkPackageNameVersion> {package};
                    manager.InstallPackages(packages, success =>
                    {
                        if (success)
                        {
                            ShowMessage("Successfully updated the Play Instant SDK package to version " +
                                        package.VersionString);
                        }
                        else
                        {
                            ShowMessage("Failed to set up the Play Instant SDK package");
                        }
                    });
                });
            });
        }

        private static void ShowMessage(string message)
        {
            Debug.LogFormat("PlayInstantSdkInstaller: {0}", message);
            EditorUtility.DisplayDialog("Play Instant SDK", message, "OK");
        }
    }
}