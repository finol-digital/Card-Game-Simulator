/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace Cgs.Performance
{
    /// <summary>
    /// Records in-app startup milestones from the earliest managed startup callback. Release builds write only the
    /// launch and play summary log lines; nothing is shown to the player.
    /// </summary>
    public static class StartupTimeline
    {
        private static Stopwatch _stopwatch;

        public static StartupTimelineSession Session { get; private set; }

        public static BenchmarkOptions Benchmark { get; private set; } = BenchmarkOptions.Disabled;

        public static bool IsBenchmarking => Benchmark.IsEnabled;

        public static double ElapsedMs => _stopwatch?.Elapsed.TotalMilliseconds ?? 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Begin()
        {
            // Read the monotonic anchor immediately before starting the stopwatch so both share one origin
            var zeroMonotonicNs = StartupClocks.MonotonicNowNs();
            _stopwatch = Stopwatch.StartNew();
            Session = new StartupTimelineSession(() => ElapsedMs, Debug.Log)
            {
                ZeroMonotonicNs = zeroMonotonicNs
            };
            Session.Mark(StartupMilestone.EngineReady);
            Benchmark = BenchmarkOptions.Disabled;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BeforeSceneLoad()
        {
            if (Session == null)
                Begin();

            if (StartupClocks.TryGetProcessUptimeMs(out var uptimeMs, out var clockName))
            {
                // Uptime is measured now; subtract the time elapsed since elapsed-zero to anchor it
                Session.ProcessStartOffsetMs = Math.Max(0, uptimeMs - ElapsedMs);
                Session.ProcessClock = clockName;
            }

            SceneManager.sceneLoaded -= OnFirstSceneLoaded;
            SceneManager.sceneLoaded += OnFirstSceneLoaded;

            if (Application.isPlaying)
            {
                var host = new GameObject(nameof(StartupTimelineRunner));
                UnityEngine.Object.DontDestroyOnLoad(host);
                host.AddComponent<StartupTimelineRunner>();
            }

#if !UNITY_EDITOR
            string[] arguments;
            try
            {
                arguments = Environment.GetCommandLineArgs();
            }
            catch (Exception)
            {
                arguments = Array.Empty<string>();
            }

            Benchmark = BenchmarkOptions.Parse(arguments, Application.absoluteURL);
#endif
            if (Benchmark.IsEnabled)
                StartupBenchmark.Begin(Benchmark);
        }

        /// <summary>Enables benchmark mode from code; used by tests and editor tooling.</summary>
        internal static void EnableBenchmarkForTests(BenchmarkOptions options) =>
            Benchmark = options ?? BenchmarkOptions.Disabled;

        private static void OnFirstSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnFirstSceneLoaded;
            Mark(StartupMilestone.FirstSceneLoaded);
        }

        public static void Mark(StartupMilestone milestone)
        {
            if (Session == null || !Session.Mark(milestone))
                return;
            if (milestone == StartupMilestone.MainMenuInteractive)
                StartupClocks.ReportFullyDrawn();
            Marked?.Invoke(milestone);
        }

        public static void MarkFirstCardActionFailed(string reason) => Session?.MarkFirstCardActionFailed(reason);

        public static event Action<StartupMilestone> Marked;

        internal static void Tick() => Session?.Tick();

        internal static void OnResumed()
        {
            if (Session == null)
                return;
            Debug.Log(StartupTimelineSession.LogPrefix + "{\"summary\":\"resume\",\"elapsedMs\":" +
                      StartupTimelineSession.Number(ElapsedMs) + ",\"monotonicNs\":" +
                      StartupClocks.MonotonicNowNs() + "}");
        }
    }

    /// <summary>Detects the first rendered frame, drives summary timeouts, and records resume anchors.</summary>
    internal sealed class StartupTimelineRunner : MonoBehaviour
    {
        private bool _wasPaused;

        private IEnumerator Start()
        {
            yield return new WaitForEndOfFrame();
            StartupTimeline.Mark(StartupMilestone.FirstFrameDrawn);
        }

        private void Update()
        {
            StartupTimeline.Tick();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                _wasPaused = true;
                return;
            }

            if (!_wasPaused)
                return;
            _wasPaused = false;
            StartupTimeline.OnResumed();
        }
    }
}
