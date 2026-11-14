/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Diagnostics;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Cgs.Performance
{
    /// <summary>
    /// Platform clock access for startup timing. Offsets are only reported where the conversion is exact;
    /// everything else is reported as unavailable instead of estimated.
    /// </summary>
    public static class StartupClocks
    {
        /// <summary>
        /// The OS monotonic clock in nanoseconds, as exposed through <see cref="Stopwatch"/>
        /// (CLOCK_MONOTONIC on Linux/Android, QueryPerformanceCounter on Windows).
        /// </summary>
        public static long MonotonicNowNs()
        {
            var ticks = Stopwatch.GetTimestamp();
            var frequency = Stopwatch.Frequency;
            if (1_000_000_000L % frequency == 0)
                return ticks * (1_000_000_000L / frequency);
            return (long)((decimal)ticks * 1_000_000_000m / frequency);
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern double CgsPerformanceNow();

        [DllImport("__Internal")]
        private static extern void CgsSetBenchmarkResult(string json);
#endif

        /// <summary>
        /// Milliseconds from process start (Android) or navigation start (Web) to now, when exact.
        /// </summary>
        public static bool TryGetProcessUptimeMs(out double uptimeMs, out string clockName)
        {
            uptimeMs = 0;
            clockName = "unavailable";
            try
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                using var process = new AndroidJavaClass("android.os.Process");
                using var systemClock = new AndroidJavaClass("android.os.SystemClock");
                // Both values share the elapsedRealtime origin (API 24+)
                var startElapsed = process.CallStatic<long>("getStartElapsedRealtime");
                var nowElapsed = systemClock.CallStatic<long>("elapsedRealtime");
                if (startElapsed <= 0 || nowElapsed < startElapsed)
                    return false;
                uptimeMs = nowElapsed - startElapsed;
                clockName = "android.elapsedRealtime";
                return true;
#elif UNITY_WEBGL && !UNITY_EDITOR
                uptimeMs = CgsPerformanceNow();
                clockName = "web.performance.now";
                return uptimeMs >= 0;
#else
                return false;
#endif
            }
            catch (Exception exception)
            {
                Debug.LogWarning("StartupClocks: process clock unavailable: " + exception.Message);
                return false;
            }
        }

        /// <summary>Tells Android that the app is fully drawn so the system reports time-to-full-display.</summary>
        public static void ReportFullyDrawn()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                activity?.Call("reportFullyDrawn");
            }
            catch (Exception exception)
            {
                Debug.LogWarning("StartupClocks: reportFullyDrawn failed: " + exception.Message);
            }
#endif
        }

        /// <summary>Publishes a benchmark result to the page for a browser harness.</summary>
        public static void PublishWebResult(string json)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            CgsSetBenchmarkResult(json);
#endif
        }
    }
}
