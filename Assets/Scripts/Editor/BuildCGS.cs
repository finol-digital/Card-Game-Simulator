
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

class BuildCGS
{
    public const string AndroidBuildPath = "builds/CGS.apk";

    public static string[] BuildScenes
    {
        get
        {
            List<string> scenes = new List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
                scenes.Add(scene.path);
            return scenes.ToArray();
        }
    }

    static void Android()
    {
#if UNITY_EDITOR_OSX
EditorPrefs.SetString("AndroidSdkRoot", "/Applications/Android/sdk");
EditorPrefs.SetString("AndroidNdkRoot", "/Applications/android-ndk-r13b");
#endif
        PlayerSettings.Android.keystorePass = Environment.GetCommandLineArgs().Last();
        PlayerSettings.Android.keyaliasPass = Environment.GetCommandLineArgs().Last();

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = BuildScenes;
        buildPlayerOptions.locationPathName = AndroidBuildPath;
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = BuildOptions.None;
        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }
}
