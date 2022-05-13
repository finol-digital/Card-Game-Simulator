/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

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

        private static readonly string[] Secrets =
            {"androidKeystorePass", "androidKeyaliasName", "androidKeyaliasPass"};

        [UsedImplicitly]
        public static void BuildOptions()
        {
            // Gather values from args
            var options = GetValidatedOptions();

            // Set version for this build
            var version = options["buildVersion"];
            PlayerSettings.bundleVersion = version;
            PlayerSettings.macOS.buildNumber = version;
            while (version.Split(new[] {'.'}, StringSplitOptions.RemoveEmptyEntries).Length < 4)
                version += ".0";
            PlayerSettings.WSA.packageVersion = new Version(version);

            // Apply build target
            var buildTarget = (BuildTarget) Enum.Parse(typeof(BuildTarget), options["buildTarget"]);
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (buildTarget)
            {
                case BuildTarget.Android:
                {
                    PlayerSettings.Android.bundleVersionCode = int.Parse(options["androidVersionCode"]);
                    EditorUserBuildSettings.buildAppBundle = options["customBuildPath"].EndsWith(".aab");
                    if (options.TryGetValue("androidKeystoreName", out var keystoreName) &&
                        !string.IsNullOrEmpty(keystoreName))
                        PlayerSettings.Android.keystoreName = keystoreName;
                    if (options.TryGetValue("androidKeystorePass", out var keystorePass) &&
                        !string.IsNullOrEmpty(keystorePass))
                        PlayerSettings.Android.keystorePass = keystorePass;
                    if (options.TryGetValue("androidKeyaliasName", out var keyaliasName) &&
                        !string.IsNullOrEmpty(keyaliasName))
                        PlayerSettings.Android.keyaliasName = keyaliasName;
                    if (options.TryGetValue("androidKeyaliasPass", out var keyaliasPass) &&
                        !string.IsNullOrEmpty(keyaliasPass))
                        PlayerSettings.Android.keyaliasPass = keyaliasPass;
                    break;
                }
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    if (!options["customBuildPath"].EndsWith(".exe"))
                        options["customBuildPath"] += "/cgs.exe";
                    break;
                case BuildTarget.WSAPlayer:
                    EditorUserBuildSettings.wsaUWPBuildType = WSAUWPBuildType.XAML;
                    break;
            }

            // Custom build
            Build(buildTarget, options["customBuildPath"]);
        }

        private static Dictionary<string, string> GetValidatedOptions()
        {
            ParseCommandLineArguments(out var validatedOptions);

            if (!validatedOptions.TryGetValue("projectPath", out _))
            {
                Console.WriteLine("Missing argument -projectPath");
                EditorApplication.Exit(110);
            }

            if (!validatedOptions.TryGetValue("buildTarget", out var buildTarget))
            {
                Console.WriteLine("Missing argument -buildTarget");
                EditorApplication.Exit(120);
            }

            if (!Enum.IsDefined(typeof(BuildTarget), buildTarget ?? string.Empty))
                EditorApplication.Exit(121);

            if (validatedOptions.TryGetValue("buildPath", out var buildPath))
                validatedOptions["customBuildPath"] = buildPath;

            if (validatedOptions.TryGetValue("customBuildPath", out _))
                return validatedOptions;

            Console.WriteLine("Missing argument -customBuildPath");
            EditorApplication.Exit(130);

            return validatedOptions;
        }

        private static void ParseCommandLineArguments(out Dictionary<string, string> providedArguments)
        {
            providedArguments = new Dictionary<string, string>();
            var args = Environment.GetCommandLineArgs();

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
                var isFlag = args[current].StartsWith("-");
                if (!isFlag) continue;
                var flag = args[current].TrimStart('-');

                // Parse optional value
                var flagHasValue = next < args.Length && !args[next].StartsWith("-");
                var value = flagHasValue ? args[next].TrimStart('-') : "";
                var isSecret = Secrets.Contains(flag);
                var displayValue = isSecret ? "*HIDDEN*" : "\"" + value + "\"";

                // Assign
                Console.WriteLine($"Found flag \"{flag}\" with value {displayValue}.");
                providedArguments.Add(flag, value);
            }
        }

        private static void Build(BuildTarget buildTarget, string filePath)
        {
            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(s => s.path).ToArray();
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                target = buildTarget,
                locationPathName = filePath,
            };

            var buildSummary = BuildPipeline.BuildPlayer(buildPlayerOptions).summary;
            ReportSummary(buildSummary);
            ExitWithResult(buildSummary.result);
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
            switch (result)
            {
                case BuildResult.Succeeded:
                    Console.WriteLine("Build succeeded!");
                    EditorApplication.Exit(0);
                    break;
                case BuildResult.Failed:
                    Console.WriteLine("Build failed!");
                    EditorApplication.Exit(101);
                    break;
                case BuildResult.Cancelled:
                    Console.WriteLine("Build cancelled!");
                    EditorApplication.Exit(102);
                    break;
                case BuildResult.Unknown:
                default:
                    Console.WriteLine("Build result is unknown!");
                    EditorApplication.Exit(103);
                    break;
            }
        }
    }
}
