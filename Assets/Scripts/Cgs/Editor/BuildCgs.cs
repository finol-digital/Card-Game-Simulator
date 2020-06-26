using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace Cgs.Editor
{
    internal static class BuildCgs
    {
        private static readonly string Eol = Environment.NewLine;

        [UsedImplicitly]
        public static void BuildProject()
        {
            // Gather values from args
            Dictionary<string, string> options = GetValidatedOptions();

            // Gather values from project
            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(s => s.path).ToArray();

            // Define BuildPlayer Options
            var buildOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = options["customBuildPath"],
                target = (BuildTarget) Enum.Parse(typeof(BuildTarget), options["buildTarget"]),
            };

            // Support automatic versioning
            PlayerSettings.bundleVersion = options["version"];
            PlayerSettings.macOS.buildNumber = options["version"];

            // Support Android signing
            if (buildOptions.target == BuildTarget.Android)
            {
                PlayerSettings.Android.bundleVersionCode = int.Parse(options["androidVersionCode"]);
                PlayerSettings.Android.keystorePass = options["keystorePass"];
                PlayerSettings.Android.keyaliasPass = options["keyaliasPass"];
                if (options.ContainsKey("androidAppBundle"))
                {
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
                $"{Eol}" +
                $"###########################{Eol}" +
                $"#    Parsing settings     #{Eol}" +
                $"###########################{Eol}" +
                $"{Eol}"
            );

            // Extract flags with optional values
            for (int current = 0, next = 1; current < args.Length; current++, next++)
            {
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
            ParseCommandLineArguments(out Dictionary<string, string> validatedOptions);

            if (!validatedOptions.TryGetValue("projectPath", out string _))
            {
                Console.WriteLine("Missing argument -projectPath");
                EditorApplication.Exit(110);
            }

            if (!validatedOptions.TryGetValue("buildTarget", out string buildTarget))
            {
                Console.WriteLine("Missing argument -buildTarget");
                EditorApplication.Exit(120);
            }

            if (!Enum.IsDefined(typeof(BuildTarget), buildTarget ?? throw new ArgumentNullException()))
            {
                EditorApplication.Exit(121);
            }

            if (!validatedOptions.TryGetValue("customBuildPath", out string customBuildPath))
            {
                Console.WriteLine("Missing argument -customBuildPath");
                EditorApplication.Exit(130);
            }
            else if (validatedOptions.ContainsKey("androidAppBundle") && customBuildPath.Contains("apk"))
            {
                validatedOptions["customBuildPath"] = customBuildPath.Replace("apk", "aab");
            }

            const string defaultCustomBuildName = "TestBuild";
            if (!validatedOptions.TryGetValue("customBuildName", out string customBuildName))
            {
                Console.WriteLine($"Missing argument -customBuildName, defaulting to {defaultCustomBuildName}.");
                validatedOptions.Add("customBuildName", defaultCustomBuildName);
            }
            else if (customBuildName == "")
            {
                Console.WriteLine($"Invalid argument -customBuildName, defaulting to {defaultCustomBuildName}.");
                validatedOptions.Add("customBuildName", defaultCustomBuildName);
            }

            return validatedOptions;
        }

        private static void ReportSummary(BuildSummary summary)
        {
            Console.WriteLine(
                $"{Eol}" +
                $"###########################{Eol}" +
                $"#      Build results      #{Eol}" +
                $"###########################{Eol}" +
                $"{Eol}" +
                $"Duration: {summary.totalTime.ToString()}{Eol}" +
                $"Warnings: {summary.totalWarnings.ToString()}{Eol}" +
                $"Errors: {summary.totalErrors.ToString()}{Eol}" +
                $"Size: {summary.totalSize.ToString()} bytes{Eol}" +
                $"{Eol}"
            );
        }

        private static void ExitWithResult(BuildResult result)
        {
            if (result == BuildResult.Succeeded)
            {
                Console.WriteLine("Build succeeded!");
                EditorApplication.Exit(0);
            }

            if (result == BuildResult.Failed)
            {
                Console.WriteLine("Build failed!");
                EditorApplication.Exit(101);
            }

            if (result == BuildResult.Cancelled)
            {
                Console.WriteLine("Build cancelled!");
                EditorApplication.Exit(102);
            }

            if (result == BuildResult.Unknown)
            {
                Console.WriteLine("Build result is unknown!");
                EditorApplication.Exit(103);
            }
        }
    }
}
