using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

class BuildCGS
{
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

    [MenuItem("CGS/Build Android")]
    static void Android()
    {
#if UNITY_EDITOR_OSX
        EditorPrefs.SetString("AndroidSdkRoot", "/Applications/Android/sdk");
        EditorPrefs.SetString("AndroidNdkRoot", "/Applications/android-ndk-r13b");
#elif UNITY_EDITOR_WIN
        EditorPrefs.SetString("AndroidSdkRoot", "C:/Users/Public/android-sdk");
        EditorPrefs.SetString("AndroidNdkRoot", "C:/Users/Public/android-ndk-r13b");
#endif

        string [] buildArgs = Environment.GetCommandLineArgs();
        PlayerSettings.Android.keystorePass = buildArgs[buildArgs.Length - 1];
        PlayerSettings.Android.keyaliasPass = buildArgs[buildArgs.Length - 1];

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = BuildScenes;
        buildPlayerOptions.locationPathName = buildArgs[buildArgs.Length - 2];
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = BuildOptions.None;
        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }

    [MenuItem("CGS/Build iOS")]
    static void iOS()
    {
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = BuildScenes;
        buildPlayerOptions.locationPathName = Environment.GetCommandLineArgs().Last();
        buildPlayerOptions.target = BuildTarget.iOS;
        buildPlayerOptions.options = BuildOptions.AcceptExternalModificationsToPlayer;
        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }
}
