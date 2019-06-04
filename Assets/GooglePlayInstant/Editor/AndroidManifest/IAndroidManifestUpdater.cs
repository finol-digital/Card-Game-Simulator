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

namespace GooglePlayInstant.Editor.AndroidManifest
{
    /// <summary>
    /// An interface for updating the AndroidManifest to target installed vs instant apps.
    /// </summary>
    public interface IAndroidManifestUpdater
    {
        /// <summary>
        /// Checks the AndroidManifest prior to building an instant app.
        /// </summary>
        /// <returns>An error message if there was a problem checking the manifest, or null if successful.</returns>
        string CheckInstantManifest();

        /// <summary>
        /// Update the AndroidManifest for building an instant app.
        /// </summary>
        /// <param name="uri">The Default URL to use, or null for a URL-less instant app.</param>
        /// <returns>An error message if there was a problem updating the manifest, or null if successful.</returns>
        string SwitchToInstant(Uri uri);

        /// <summary>
        /// Update the AndroidManifest for building an installed app.
        /// </summary>
        void SwitchToInstalled();
    }
}