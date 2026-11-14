/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Cgs.Performance
{
    /// <summary>
    /// Records startup milestones for one session and writes the launch and play summaries at the emission
    /// points defined by the startup-benchmarking spec. Engine-independent so tests can drive its clock.
    /// </summary>
    public sealed class StartupTimelineSession
    {
        public const string LogPrefix = "[CgsStartup] ";
        public const double DefaultLaunchTimeoutMs = 30_000;
        public const double DefaultPlayTimeoutMs = 60_000;

        public const string OutcomeOk = "ok";
        public const string OutcomeFailed = "failed";
        public const string OutcomeTimeout = "timeout";

        private static readonly StartupMilestone[] LaunchMilestones =
        {
            StartupMilestone.EngineReady, StartupMilestone.FirstSceneLoaded, StartupMilestone.FirstFrameDrawn,
            StartupMilestone.MainMenuInteractive
        };

        private static readonly StartupMilestone[] PlayMilestones =
        {
            StartupMilestone.StartGameChosen, StartupMilestone.PlaySceneLoaded, StartupMilestone.TablePlayable,
            StartupMilestone.FirstCardAction
        };

        private readonly Func<double> _elapsedMs;
        private readonly Action<string> _log;
        private readonly double _launchTimeoutMs;
        private readonly double _playTimeoutMs;
        private readonly Dictionary<StartupMilestone, double> _marks = new();

        public event Action<StartupSummaryKind, string> SummaryWritten;

        public bool IsLaunchSummaryWritten { get; private set; }
        public bool IsPlaySummaryWritten { get; private set; }
        public string PlayOutcome { get; private set; }
        public string FailedMilestone { get; private set; }
        public string FailureReason { get; private set; }

        /// <summary>Monotonic timestamp (ns) of elapsed time zero; lets harnesses convert milestones.</summary>
        public long? ZeroMonotonicNs { get; set; }

        /// <summary>Exact process-start (or navigation-start) to elapsed-zero offset, when the platform has one.</summary>
        public double? ProcessStartOffsetMs { get; set; }

        /// <summary>Harness launch request to elapsed-zero offset, from a validated run file.</summary>
        public double? LaunchRequestOffsetMs { get; set; }

        public string ProcessClock { get; set; } = "unavailable";

        public StartupTimelineSession(Func<double> elapsedMs, Action<string> log,
            double launchTimeoutMs = DefaultLaunchTimeoutMs, double playTimeoutMs = DefaultPlayTimeoutMs)
        {
            _elapsedMs = elapsedMs ?? throw new ArgumentNullException(nameof(elapsedMs));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _launchTimeoutMs = launchTimeoutMs;
            _playTimeoutMs = playTimeoutMs;
        }

        public bool HasMark(StartupMilestone milestone) => _marks.ContainsKey(milestone);

        public double? GetMark(StartupMilestone milestone) =>
            _marks.TryGetValue(milestone, out var value) ? value : null;

        /// <summary>Records the first occurrence of a milestone. Later occurrences are ignored.</summary>
        public bool Mark(StartupMilestone milestone)
        {
            if (_marks.ContainsKey(milestone))
                return false;
            // The play summary describes the session's first game only
            if (IsPlaySummaryWritten && Array.IndexOf(PlayMilestones, milestone) >= 0)
                return false;

            _marks[milestone] = _elapsedMs();

            if (milestone == StartupMilestone.MainMenuInteractive && !IsLaunchSummaryWritten)
                WriteLaunchSummary();
            else if (milestone == StartupMilestone.FirstCardAction && !IsPlaySummaryWritten &&
                     HasMark(StartupMilestone.StartGameChosen))
                WritePlaySummary(OutcomeOk, null, null);
            return true;
        }

        /// <summary>The first card action of the first game did not succeed.</summary>
        public void MarkFirstCardActionFailed(string reason)
        {
            if (IsPlaySummaryWritten || !HasMark(StartupMilestone.StartGameChosen))
                return;
            WritePlaySummary(OutcomeFailed, nameof(StartupMilestone.FirstCardAction), reason);
        }

        /// <summary>Writes summaries whose timeouts have expired. Call once per frame.</summary>
        public void Tick()
        {
            var now = _elapsedMs();
            if (!IsLaunchSummaryWritten && now >= _launchTimeoutMs)
                WriteLaunchSummary();

            if (IsPlaySummaryWritten || !_marks.TryGetValue(StartupMilestone.StartGameChosen, out var startGame) ||
                now - startGame < _playTimeoutMs)
                return;

            var missing = Array.Find(PlayMilestones, milestone => !_marks.ContainsKey(milestone));
            WritePlaySummary(OutcomeTimeout, missing.ToString(), "timeout");
        }

        private void WriteLaunchSummary()
        {
            IsLaunchSummaryWritten = true;
            var outcome = HasMark(StartupMilestone.MainMenuInteractive) ? OutcomeOk : OutcomeTimeout;
            var json = new StringBuilder();
            BeginSummary(json, "launch", outcome);
            AppendMilestones(json, LaunchMilestones, true);
            EndSummary(json, outcome == OutcomeOk ? null : nameof(StartupMilestone.MainMenuInteractive),
                outcome == OutcomeOk ? null : "timeout", null);
            Emit(StartupSummaryKind.Launch, json.ToString());
        }

        private void WritePlaySummary(string outcome, string failedMilestone, string reason)
        {
            IsPlaySummaryWritten = true;
            PlayOutcome = outcome;
            FailedMilestone = failedMilestone;
            FailureReason = reason;

            var json = new StringBuilder();
            BeginSummary(json, "play", outcome);
            // Table-playable time is only a result when the first card action succeeded
            AppendMilestones(json, PlayMilestones, outcome == OutcomeOk);
            EndSummary(json, failedMilestone, reason,
                outcome == OutcomeOk ? null : GetMark(StartupMilestone.TablePlayable));
            Emit(StartupSummaryKind.Play, json.ToString());
        }

        private void BeginSummary(StringBuilder json, string kind, string outcome)
        {
            json.Append("{\"summary\":\"").Append(kind).Append("\",\"outcome\":\"").Append(outcome).Append('"');
            json.Append(",\"processClock\":\"").Append(Escape(ProcessClock)).Append('"');
            json.Append(",\"zeroMonotonicNs\":").Append(Number(ZeroMonotonicNs));
            json.Append(",\"processStartOffsetMs\":").Append(Number(ProcessStartOffsetMs));
            json.Append(",\"launchRequestOffsetMs\":").Append(Number(LaunchRequestOffsetMs));
        }

        private void AppendMilestones(StringBuilder json, StartupMilestone[] milestones, bool includeTableResult)
        {
            json.Append(",\"milestonesMs\":{");
            for (var i = 0; i < milestones.Length; i++)
            {
                if (i > 0)
                    json.Append(',');
                var milestone = milestones[i];
                var value = GetMark(milestone);
                if (!includeTableResult && milestone is StartupMilestone.TablePlayable
                        or StartupMilestone.FirstCardAction)
                    value = null;
                json.Append('"').Append(ToCamelCase(milestone.ToString())).Append("\":").Append(Number(value));
            }

            json.Append('}');
        }

        private static void EndSummary(StringBuilder json, string failedMilestone, string reason,
            double? tableReadyDiagnosticMs)
        {
            json.Append(",\"failedMilestone\":")
                .Append(failedMilestone == null ? "null" : "\"" + ToCamelCase(failedMilestone) + "\"");
            json.Append(",\"reason\":").Append(reason == null ? "null" : "\"" + Escape(reason) + "\"");
            if (tableReadyDiagnosticMs.HasValue)
                json.Append(",\"diagnostics\":{\"tableReadyMs\":").Append(Number(tableReadyDiagnosticMs)).Append('}');
            json.Append('}');
        }

        private void Emit(StartupSummaryKind kind, string json)
        {
            _log(LogPrefix + json);
            SummaryWritten?.Invoke(kind, json);
        }

        internal static string Number(double? value) =>
            value.HasValue ? value.Value.ToString("0.###", CultureInfo.InvariantCulture) : "null";

        internal static string Number(long? value) =>
            value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "null";

        internal static string ToCamelCase(string value) =>
            string.IsNullOrEmpty(value) ? value : char.ToLowerInvariant(value[0]) + value[1..];

        internal static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            var builder = new StringBuilder(value.Length);
            foreach (var c in value)
            {
                switch (c)
                {
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    default:
                        if (c < ' ')
                            builder.Append("\\u").Append(((int)c).ToString("x4"));
                        else
                            builder.Append(c);
                        break;
                }
            }

            return builder.ToString();
        }
    }
}
