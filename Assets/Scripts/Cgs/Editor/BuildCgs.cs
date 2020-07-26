/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
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
        public static void BuildUwp()
        {
            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(s => s.path).ToArray();

            // Define BuildPlayer Options
            var buildOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = "build",
                target = BuildTarget.WSAPlayer,
            };

            // Perform build
            BuildReport buildReport = BuildPipeline.BuildPlayer(buildOptions);

            // Summary
            BuildSummary summary = buildReport.summary;
            ReportSummary(summary);

            // Result
            BuildResult result = summary.result;
            ExitWithResult(result);
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
