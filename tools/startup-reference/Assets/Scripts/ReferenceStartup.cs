/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

/// <summary>
/// Minimal Unity reference app for CGS startup comparisons: one scene, one working button. It records the time to
/// an interactive button with the same clocks, run-file handoff, markers, and result format as CGS benchmark mode,
/// so the CGS harnesses in tools/startup-benchmark measure it unchanged.
/// </summary>
public sealed class ReferenceStartup : MonoBehaviour
{
    private const string LogPrefix = "[CgsBenchmark] ";
    private static Stopwatch _stopwatch;
    private static long _zeroMonotonicNs;

    [SerializeField] Button button;
    [SerializeField] Image menuMarker;

    private bool _clicked;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern double CgsPerformanceNow();

    [DllImport("__Internal")]
    private static extern void CgsSetBenchmarkResult(string json);
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Begin()
    {
        _stopwatch = Stopwatch.StartNew();
        _zeroMonotonicNs = MonotonicNowNs();
    }

    private static double ElapsedMs => _stopwatch.Elapsed.TotalMilliseconds;

    private static long MonotonicNowNs()
    {
        var ticks = Stopwatch.GetTimestamp();
        var frequency = Stopwatch.Frequency;
        return 1_000_000_000L % frequency == 0
            ? ticks * (1_000_000_000L / frequency)
            : (long)((decimal)ticks * 1_000_000_000m / frequency);
    }

    private void Awake()
    {
        button.onClick.AddListener(() => _clicked = true);
        menuMarker.gameObject.SetActive(false);
    }

    private IEnumerator Start()
    {
        // Interactive means the button actually handles a submit, as CGS's Main Menu must accept an action
        yield return null;
        ExecuteEvents.Execute(button.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
        while (!_clicked)
            yield return null;
        var interactiveMs = ElapsedMs;
        ReportFullyDrawn();

        menuMarker.gameObject.SetActive(true);
        yield return new WaitForEndOfFrame();
        yield return null;
        var markerMs = ElapsedMs;

        Finish(interactiveMs, markerMs);
    }

    private static void Finish(double interactiveMs, double markerMs)
    {
        var arguments = Environment.GetCommandLineArgs();
        var runFilePath = Argument(arguments, "-cgsRunFile=");
        var resultPath = Argument(arguments, "-cgsResultFile=");
        var runId = Argument(arguments, "-cgsRun=") ?? "0";
        string runFileError = null;
        double? launchRequestOffsetMs = null;
        if (!string.IsNullOrEmpty(runFilePath))
        {
            try
            {
                var runFile = JsonUtility.FromJson<RunFile>(File.ReadAllText(runFilePath));
                File.WriteAllText(runFilePath + ".consumed", "1");
                if (!string.IsNullOrEmpty(runFile.build) && runFile.build != Application.version)
                    runFileError = "runFileBuildMismatch";
                else if (runFile.monotonicNs <= 0 || runFile.monotonicNs > _zeroMonotonicNs)
                    runFileError = "runFileClockMismatch";
                else
                    launchRequestOffsetMs = (_zeroMonotonicNs - runFile.monotonicNs) / 1_000_000.0;
                if (!string.IsNullOrEmpty(runFile.runId))
                    runId = runFile.runId;
            }
            catch (Exception exception)
            {
                runFileError = "runFileError:" + exception.GetType().Name;
            }
        }

        var processStartOffsetMs = ProcessStartOffsetMs();
        var json = "{\"benchmark\":1,\"reference\":true,\"runId\":\"" + runId + "\",\"build\":\"" +
                   Application.version + "\",\"platform\":\"" + Application.platform + "\",\"device\":\"" +
                   SystemInfo.deviceModel.Replace("\"", "'") + "\",\"outcome\":\"ok\",\"failedMilestone\":null," +
                   "\"runFileError\":" + (runFileError == null ? "null" : "\"" + runFileError + "\"") +
                   ",\"markersPresentedMs\":{\"menu\":" + Number(markerMs) + "}" +
                   ",\"launch\":{\"summary\":\"launch\",\"outcome\":\"ok\",\"zeroMonotonicNs\":" + _zeroMonotonicNs +
                   ",\"processStartOffsetMs\":" + Number(processStartOffsetMs) +
                   ",\"launchRequestOffsetMs\":" + Number(launchRequestOffsetMs) +
                   ",\"milestonesMs\":{\"mainMenuInteractive\":" + Number(interactiveMs) + "}},\"play\":null}";

        Debug.Log(LogPrefix + json);
#if UNITY_WEBGL && !UNITY_EDITOR
        CgsSetBenchmarkResult(json);
#else
        try
        {
            resultPath ??= Path.Combine(Application.persistentDataPath, "benchmark", $"run-{runId}.json");
            Directory.CreateDirectory(Path.GetDirectoryName(resultPath) ?? ".");
            File.WriteAllText(resultPath, json);
        }
        catch (Exception exception)
        {
            Debug.LogError(LogPrefix + exception.Message);
        }
#endif
#if UNITY_STANDALONE && !UNITY_EDITOR
        Application.Quit(0);
#endif
    }

    private static double? ProcessStartOffsetMs()
    {
        try
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using var process = new AndroidJavaClass("android.os.Process");
            using var systemClock = new AndroidJavaClass("android.os.SystemClock");
            var uptime = systemClock.CallStatic<long>("elapsedRealtime") -
                         process.CallStatic<long>("getStartElapsedRealtime");
            return Math.Max(0, uptime - ElapsedMs);
#elif UNITY_WEBGL && !UNITY_EDITOR
            return Math.Max(0, CgsPerformanceNow() - ElapsedMs);
#else
            return null;
#endif
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static void ReportFullyDrawn()
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
            Debug.LogWarning(exception.Message);
        }
#endif
    }

    private static string Argument(string[] arguments, string prefix)
    {
        foreach (var argument in arguments)
            if (argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return argument[prefix.Length..].Trim('"');
        return null;
    }

    private static string Number(double? value) =>
        value.HasValue ? value.Value.ToString("0.###", CultureInfo.InvariantCulture) : "null";

    [Serializable]
    private sealed class RunFile
    {
        // ReSharper disable InconsistentNaming
        public string runId;
        public string build;
        public long monotonicNs;
        // ReSharper restore InconsistentNaming
    }
}
