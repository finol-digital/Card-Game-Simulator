/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Cgs.Performance
{
    /// <summary>
    /// Opt-in benchmark settings, read only at launch from command-line arguments or the launching deep link.
    /// </summary>
    public sealed class BenchmarkOptions
    {
        public const string ArgumentFlag = "-cgsBenchmark";
        public const string ArgumentRunFile = "-cgsRunFile=";
        public const string ArgumentResultFile = "-cgsResultFile=";
        public const string ArgumentStartDelay = "-cgsStartDelayMs=";
        public const string ArgumentGame = "-cgsBenchmarkGame=";
        public const string ArgumentRun = "-cgsRun=";
        public const string QueryKey = "benchmark";

        public const double RunFileMaxAgeMs = 120_000;

        public bool IsEnabled { get; private set; }
        public const int MaxRunIdLength = 64;
        public string RunId { get; private set; } = "0";
        public string RunFilePath { get; private set; }
        public string ResultFilePath { get; private set; }
        public double StartDelayMs { get; private set; }
        public string GameId { get; private set; } = Tags.StandardPlayingCardsDirectoryName;
        public string Source { get; private set; } = "none";

        public static BenchmarkOptions Disabled => new();

        /// <summary>Parses command-line arguments, then (if not enabled there) the launching deep link.</summary>
        public static BenchmarkOptions Parse(IReadOnlyList<string> arguments, string launchUrl)
        {
            var options = new BenchmarkOptions();
#if CGS_SINGLEGAME
            // Single-game web builds are excluded from benchmark mode
            return options;
#else
            if (arguments != null)
            {
                foreach (var argument in arguments)
                {
                    if (string.IsNullOrEmpty(argument))
                        continue;
                    if (argument.Equals(ArgumentFlag, StringComparison.OrdinalIgnoreCase))
                    {
                        options.IsEnabled = true;
                        options.Source = "arguments";
                    }
                    else if (TryGetValue(argument, ArgumentRunFile, out var runFile))
                        options.RunFilePath = runFile;
                    else if (TryGetValue(argument, ArgumentResultFile, out var resultFile))
                        options.ResultFilePath = resultFile;
                    else if (TryGetValue(argument, ArgumentStartDelay, out var delay))
                        options.StartDelayMs = ParseDelay(delay);
                    else if (TryGetValue(argument, ArgumentGame, out var game))
                        options.GameId = game;
                    else if (TryGetValue(argument, ArgumentRun, out var run))
                        options.RunId = SanitizeRunId(run);
                }
            }

            if (options.IsEnabled || !IsBenchmarkLink(launchUrl, out var query))
                return options;

            options.IsEnabled = true;
            options.Source = "deepLink";
            if (query.TryGetValue("run", out var queryRun) && !string.IsNullOrEmpty(queryRun))
                options.RunId = SanitizeRunId(queryRun);
            if (query.TryGetValue("delay", out var queryDelay))
                options.StartDelayMs = ParseDelay(queryDelay);
            if (query.TryGetValue("game", out var queryGame) && !string.IsNullOrEmpty(queryGame))
                options.GameId = queryGame;
            // Links never choose files or download sources; the "url" parameter is ignored in benchmark mode
            return options;
#endif
        }

        /// <summary>True when a deep link asks for benchmark mode (honored only when it launches CGS).</summary>
        /// <summary>
        /// Run IDs name result files, so only letters, digits, '-' and '_' are kept (at most 64 characters).
        /// </summary>
        public static string SanitizeRunId(string runId)
        {
            if (string.IsNullOrEmpty(runId))
                return "0";
            var builder = new System.Text.StringBuilder(Math.Min(runId.Length, MaxRunIdLength));
            foreach (var character in runId)
            {
                if (builder.Length == MaxRunIdLength)
                    break;
                if (character is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '-' or '_')
                    builder.Append(character);
            }

            return builder.Length > 0 ? builder.ToString() : "0";
        }

        public static bool IsBenchmarkLink(string url) => IsBenchmarkLink(url, out _);

        private static bool IsBenchmarkLink(string url, out Dictionary<string, string> query)
        {
            query = null;
            if (string.IsNullOrEmpty(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return false;
            query = ParseQuery(uri.Query);
            return query.TryGetValue(QueryKey, out var value) &&
                   (value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase));
        }

        private static Dictionary<string, string> ParseQuery(string query)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(query))
                return values;
            foreach (var pair in query.TrimStart('?').Split('&'))
            {
                if (string.IsNullOrEmpty(pair))
                    continue;
                var separator = pair.IndexOf('=');
                var key = Uri.UnescapeDataString(separator < 0 ? pair : pair[..separator]);
                var value = separator < 0 ? string.Empty : Uri.UnescapeDataString(pair[(separator + 1)..]);
                values[key] = value;
            }

            return values;
        }

        private static bool TryGetValue(string argument, string prefix, out string value)
        {
            value = null;
            if (!argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;
            value = argument[prefix.Length..].Trim('"');
            return true;
        }

        private static double ParseDelay(string value) =>
            double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var delay) && delay > 0
                ? delay
                : 0;
    }

    /// <summary>
    /// Per-run handoff written by a harness immediately before it requests a launch, so the budget clock starts
    /// before the OS or Steam handles the launch.
    /// </summary>
    [Serializable]
    public sealed class BenchmarkRunFile
    {
        public const string ConsumedSuffix = ".consumed";

        // ReSharper disable InconsistentNaming
        public string runId;
        public string build;
        public long monotonicNs;
        // ReSharper restore InconsistentNaming

        /// <summary>
        /// Reads and validates a run file, then marks it consumed. Returns null and an error when the file is
        /// missing, already consumed, stale, from the future, or for a different build.
        /// </summary>
        public static BenchmarkRunFile Consume(string path, string expectedBuild, long nowMonotonicNs,
            out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(path))
            {
                error = "noRunFile";
                return null;
            }

            try
            {
                if (!File.Exists(path))
                {
                    error = "runFileMissing";
                    return null;
                }

                if (File.Exists(path + ConsumedSuffix))
                {
                    error = "runFileConsumed";
                    return null;
                }

                var runFile = JsonUtility.FromJson<BenchmarkRunFile>(File.ReadAllText(path));
                File.WriteAllText(path + ConsumedSuffix, nowMonotonicNs.ToString(CultureInfo.InvariantCulture));
                if (runFile == null || runFile.monotonicNs <= 0)
                {
                    error = "runFileInvalid";
                    return null;
                }

                if (!string.IsNullOrEmpty(runFile.build) && !string.IsNullOrEmpty(expectedBuild) &&
                    !runFile.build.Equals(expectedBuild, StringComparison.Ordinal))
                {
                    error = "runFileBuildMismatch";
                    return null;
                }

                var ageMs = (nowMonotonicNs - runFile.monotonicNs) / 1_000_000.0;
                if (ageMs < 0)
                {
                    error = "runFileClockMismatch";
                    return null;
                }

                if (ageMs > BenchmarkOptions.RunFileMaxAgeMs)
                {
                    error = "runFileStale";
                    return null;
                }

                return runFile;
            }
            catch (Exception exception)
            {
                error = "runFileError:" + exception.GetType().Name;
                return null;
            }
        }
    }
}
