/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace Cgs.Editor
{
    /// <summary>
    /// Declares in Info.plist that CGS uses no non-exempt encryption (only HTTPS through the system), so App Store
    /// Connect does not hold each TestFlight build for the export compliance question.
    /// </summary>
    public class SetIosExportCompliance : IPostprocessBuildWithReport
    {
        private const string EncryptionKey = "ITSAppUsesNonExemptEncryption";

        public int callbackOrder => 1;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS)
                return;

            var plistPath = Path.Combine(report.summary.outputPath, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetBoolean(EncryptionKey, false);
            plist.WriteToFile(plistPath);
            Debug.Log($"Set {EncryptionKey} to false in {plistPath}");
        }
    }
}
#endif
