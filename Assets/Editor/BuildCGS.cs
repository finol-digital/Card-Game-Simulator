using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

class BuildCGS
{
    private static string EOL = Environment.NewLine;

    static void SetAndroidSdkNdk()
    {
#if UNITY_EDITOR_LINUX
        EditorPrefs.SetString("AndroidSdkRoot", "/home/david/Android/Sdk");
        EditorPrefs.SetString("AndroidNdkRoot", "/home/david/android-ndk-r16b");
#elif UNITY_EDITOR_OSX
        EditorPrefs.SetString("AndroidSdkRoot", "/Applications/Android/sdk");
        EditorPrefs.SetString("AndroidNdkRoot", "/Applications/android-ndk-r16b");
#elif UNITY_EDITOR_WIN
        EditorPrefs.SetString("AndroidSdkRoot", "C:/Users/Public/android-sdk");
        EditorPrefs.SetString("AndroidNdkRoot", "C:/Users/Public/android-ndk-r16b");
#endif
    }

    [MenuItem("CGS/Build Android")]
    static void Android()
    {
        SetAndroidSdkNdk();
        string [] buildArgs = Environment.GetCommandLineArgs();
        PlayerSettings.Android.keystorePass = buildArgs[buildArgs.Length - 1];
        PlayerSettings.Android.keyaliasPass = buildArgs[buildArgs.Length - 1];

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(s => s.path).ToArray();
        buildPlayerOptions.locationPathName = buildArgs[buildArgs.Length - 2];
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = BuildOptions.None;
        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }

    [MenuItem("CGS/Build iOS")]
    static void iOS()
    {
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(s => s.path).ToArray();
        buildPlayerOptions.locationPathName = Environment.GetCommandLineArgs().Last();
        buildPlayerOptions.target = BuildTarget.iOS;
        buildPlayerOptions.options = BuildOptions.AcceptExternalModificationsToPlayer;
        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }

    public static void BuildProject()
    {
      // Gather values from args
      var options = GetValidatedOptions();

      // Gather values from project
      var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(s => s.path).ToArray();

      // Define BuildPlayer Options
      var buildOptions = new BuildPlayerOptions {
        scenes = scenes,
        locationPathName = options["customBuildPath"],
        target = (BuildTarget) Enum.Parse(typeof(BuildTarget), options["buildTarget"]),
      };

      // Support Android signing
      if (buildOptions.target == BuildTarget.Android) {
//        SetAndroidSdkNdk();
        PlayerSettings.Android.keystorePass = options["keystorePass"];
        PlayerSettings.Android.keyaliasPass = options["keyaliasPass"];
        if (options.ContainsKey("androidAppBundle")) {
          EditorUserBuildSettings.buildAppBundle = true;
        }
      }

      // Perform build
      BuildReport buildReport = BuildPipeline.BuildPlayer(buildOptions);

      // Summary
      BuildSummary summary = buildReport.summary;
      ReportSummary(summary);

      // Result
      BuildResult result = summary.result;
      ExitWithResult(result);
    }

    private static void ParseCommandLineArguments(out Dictionary<string, string> providedArguments)
    {
      providedArguments = new Dictionary<string, string>();
      string[] args = Environment.GetCommandLineArgs();

      Console.WriteLine(
        $"{EOL}" +
        $"###########################{EOL}" +
        $"#    Parsing settings     #{EOL}" +
        $"###########################{EOL}" +
        $"{EOL}"
      );

      // Extract flags with optional values
      for (int current = 0, next = 1; current < args.Length; current++, next++) {
        // Parse flag
        bool isFlag = args[current].StartsWith("-");
        if (!isFlag) continue;
        string flag = args[current].TrimStart('-');

        // Parse optional value
        bool flagHasValue = next < args.Length && !args[next].StartsWith("-");
        string value = flagHasValue ? args[next].TrimStart('-') : "";

        // Assign
//        Console.WriteLine($"Found flag \"{flag}\" with value \"{value}\".");
        providedArguments.Add(flag, value);
      }
    }

    private static Dictionary<string, string> GetValidatedOptions()
    {
      ParseCommandLineArguments(out var validatedOptions);

      if (!validatedOptions.TryGetValue("projectPath", out var projectPath)) {
        Console.WriteLine("Missing argument -projectPath");
        EditorApplication.Exit(110);
      }

      if (!validatedOptions.TryGetValue("buildTarget", out var buildTarget)) {
        Console.WriteLine("Missing argument -buildTarget");
        EditorApplication.Exit(120);
      }

      if (!Enum.IsDefined(typeof(BuildTarget), buildTarget)) {
        EditorApplication.Exit(121);
      }

      if (!validatedOptions.TryGetValue("customBuildPath", out var customBuildPath)) {
        Console.WriteLine("Missing argument -customBuildPath");
        EditorApplication.Exit(130);
      }

      string defaultCustomBuildName = "TestBuild";
      if (!validatedOptions.TryGetValue("customBuildName", out var customBuildName)) {
        Console.WriteLine($"Missing argument -customBuildName, defaulting to {defaultCustomBuildName}.");
        validatedOptions.Add("customBuildName", defaultCustomBuildName);
      }
      else if (customBuildName == "") {
        Console.WriteLine($"Invalid argument -customBuildName, defaulting to {defaultCustomBuildName}.");
        validatedOptions.Add("customBuildName", defaultCustomBuildName);
      }

      return validatedOptions;
    }

    private static void ReportSummary(BuildSummary summary)
    {
      Console.WriteLine(
        $"{EOL}" +
        $"###########################{EOL}" +
        $"#      Build results      #{EOL}" +
        $"###########################{EOL}" +
        $"{EOL}" +
        $"Duration: {summary.totalTime.ToString()}{EOL}" +
        $"Warnings: {summary.totalWarnings.ToString()}{EOL}" +
        $"Errors: {summary.totalErrors.ToString()}{EOL}" +
        $"Size: {summary.totalSize.ToString()} bytes{EOL}" +
        $"{EOL}"
      );
    }

    private static void ExitWithResult(BuildResult result)
    {
      if (result == BuildResult.Succeeded) {
        Console.WriteLine("Build succeeded!");
        EditorApplication.Exit(0);
      }

      if (result == BuildResult.Failed) {
        Console.WriteLine("Build failed!");
        EditorApplication.Exit(101);
      }

      if (result == BuildResult.Cancelled) {
        Console.WriteLine("Build cancelled!");
        EditorApplication.Exit(102);
      }

      if (result == BuildResult.Unknown) {
        Console.WriteLine("Build result is unknown!");
        EditorApplication.Exit(103);
      }
    }

}
