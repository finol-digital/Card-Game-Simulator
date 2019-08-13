#if UNITY_IPHONE
using System;
using System.IO;
using System.Text;

using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

public class IPhoneBuildPostprocessor : MonoBehaviour
{
    [PostProcessBuildAttribute(1)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        Debug.Log("IPhone Script. Executing post process build phase.");
        Debug.Log("Path to XCode project: " + pathToBuiltProject);
        var xcodeProjectPath = Path.Combine(pathToBuiltProject, "Unity-iPhone.xcodeproj");
        var pbxPath = Path.Combine(xcodeProjectPath, "project.pbxproj");
        var xcodeProjectLines = File.ReadAllLines(pbxPath);
        var sb = new StringBuilder();
        foreach (var line in xcodeProjectLines)
        {
            if (line.Contains("GCC_ENABLE_OBJC_EXCEPTIONS") ||
                line.Contains("GCC_ENABLE_CPP_EXCEPTIONS") ||
                line.Contains("CLANG_ENABLE_MODULES"))
            {
                var newLine = line.Replace("NO", "YES");
                Debug.Log(line);
                sb.AppendLine(newLine);
            }
            else
            {
                sb.AppendLine(line);
            }
        }

        File.WriteAllText(pbxPath, sb.ToString());

        Debug.Log("IPhone Script. Finished executing post process build phase.");
    }
}
#endif
