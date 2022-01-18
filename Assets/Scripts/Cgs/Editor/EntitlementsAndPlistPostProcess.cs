/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
using System.IO;
#endif

namespace Cgs.Editor
{
    public class EntitlementsAndPlistPostProcess : ScriptableObject
    {
        public DefaultAsset entitlementsFile;

        // For Firebase Dynamic Links
        [PostProcessBuild]
        public static void OnPostProcess(BuildTarget buildTarget, string buildPath)
        {
            if (buildTarget != BuildTarget.iOS)
                return;

            var dummy = CreateInstance<EntitlementsAndPlistPostProcess>();
            var file = dummy.entitlementsFile;
            if (file == null)
            {
                Debug.LogError("EntitlementsAndPlistPostProcess::entitlementsFileMissing!");
                return;
            }

            DestroyImmediate(dummy);

#if UNITY_IOS
            var pbxProjectPath = PBXProject.GetPBXProjectPath(buildPath);
            var pbxProject = new PBXProject();
            pbxProject.ReadFromFile(pbxProjectPath);

            const string targetName = "Unity-iPhone";
            var targetGuid = pbxProject.GetUnityMainTargetGuid();
            var src = AssetDatabase.GetAssetPath(file);
            var fileName = Path.GetFileName(src);
            var dst = buildPath + "/" + targetName + "/" + fileName;
            if (!File.Exists(dst))
                FileUtil.CopyFileOrDirectory(src, dst);
            pbxProject.AddFile(targetName + "/" + fileName, fileName);
            pbxProject.AddBuildProperty(targetGuid, "CODE_SIGN_ENTITLEMENTS", targetName + "/" + fileName);
            pbxProject.WriteToFile(pbxProjectPath);

            var plistPath = buildPath + "/Info.plist";
            var plistDocument = new PlistDocument();
            plistDocument.ReadFromString(File.ReadAllText(plistPath));
            var rootDict = plistDocument.root;
            rootDict.SetBoolean("FirebaseAppDelegateProxyEnabled", false);
            PlistElementArray customDomains = rootDict.CreateArray("FirebaseDynamicLinksCustomDomains");
            customDomains.AddString("https://cgs.link");
            File.WriteAllText(plistPath, plistDocument.WriteToString());
#endif
        }
    }
}
