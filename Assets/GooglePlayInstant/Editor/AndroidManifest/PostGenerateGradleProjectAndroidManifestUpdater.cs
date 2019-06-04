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

#if UNITY_2018_1_OR_NEWER
using System;
using System.IO;
using System.Xml.Linq;
using UnityEditor.Android;
using UnityEngine;

namespace GooglePlayInstant.Editor.AndroidManifest
{
    /// <summary>
    /// An IAndroidManifestUpdater for Unity 2018+ that obtains the AndroidManifest.xml after it is fully merged
    /// but before the build occurs and updates it according to whether this is a Play Instant build.
    /// </summary>
    public class PostGenerateGradleProjectAndroidManifestUpdater : IAndroidManifestUpdater,
        IPostGenerateGradleAndroidProject
    {
        public int callbackOrder
        {
            get { return 100; }
        }

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            if (!PlayInstantBuildConfiguration.IsInstantBuildType())
            {
                return;
            }

            // Update the final merged AndroidManifest.xml prior to the gradle build.
            var manifestPath = Path.Combine(path, "src/main/AndroidManifest.xml");
            Debug.LogFormat("Updating manifest for Play Instant: {0}", manifestPath);

            Uri uri = null;
            var instantUrl = PlayInstantBuildConfiguration.InstantUrl;
            if (!string.IsNullOrEmpty(instantUrl))
            {
                uri = new Uri(instantUrl);
            }

            var doc = XDocument.Load(manifestPath);
            var errorMessage = AndroidManifestHelper.ConvertManifestToInstant(doc, uri);
            if (errorMessage != null)
            {
                PlayInstantBuilder.DisplayBuildError(
                    string.Format("Error updating AndroidManifest.xml: {0}", errorMessage));
                return;
            }

            doc.Save(manifestPath);
        }

        public string CheckInstantManifest()
        {
            // Unused on 2018+
            return null;
        }

        public string SwitchToInstant(Uri uri)
        {
            // Unused on 2018+
            return null;
        }

        public void SwitchToInstalled()
        {
            // Unused on 2018+
        }
    }
}
#endif