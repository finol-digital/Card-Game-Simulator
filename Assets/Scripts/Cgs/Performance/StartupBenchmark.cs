/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Cgs.Menu;
using Cgs.Play;
using Cgs.Play.Drawer;
using UnityEngine;
using UnityEngine.UI;
using UnityExtensionMethods;

namespace Cgs.Performance
{
    /// <summary>
    /// Opt-in benchmark mode: drives the real UI from launch to the first card action of a bundled game, shows
    /// video markers, and delivers machine-readable results. It never changes saved preferences or game selection,
    /// and never downloads content named by a link or argument.
    /// </summary>
    public sealed class StartupBenchmark : MonoBehaviour
    {
        public const string LogPrefix = "[CgsBenchmark] ";
        private const string StartDecksMessageKey = "play.load.decks";
        private const double ActionTimeoutMs = 5_000;
        private const double DealWithoutDealerAfterMs = 1_000;
        private const double GameLoadTimeoutMs = 30_000;
        private const int MarkerSize = 96;

        private enum Marker
        {
            Menu,
            Start,
            Action,
            Failed
        }

        private static readonly Color[] MarkerColors =
        {
            new(0f, 0.85f, 0f), new(0f, 0.45f, 1f), new(1f, 0.85f, 0f), new(1f, 0f, 0f)
        };

        private static readonly Vector2[] MarkerAnchors =
        {
            new(0, 1), new(1, 1), new(0, 0), new(1, 0)
        };

        public static StartupBenchmark Instance { get; private set; }

        private BenchmarkOptions _options;
        private string _runId;
        private string _runFileError;
        private bool _gameSwitched;
        private string _gameLoadFailure;
        private string _launchJson;
        private string _playJson;
        private readonly Dictionary<Marker, double> _markersPresentedMs = new();
        private readonly Dictionary<Marker, GameObject> _markers = new();
        private Text _resultsText;
        private string _resultPath;

        internal static void Begin(BenchmarkOptions options)
        {
            if (Instance != null)
                return;
            var host = new GameObject(nameof(StartupBenchmark));
            DontDestroyOnLoad(host);
            Instance = host.AddComponent<StartupBenchmark>();
            Instance.Initialize(options);
        }

        private void Initialize(BenchmarkOptions options)
        {
            _options = options;
            _runId = options.RunId;
            StartupTimeline.Session.SummaryWritten += OnSummaryWritten;
            ConsumeRunFile();
            CreateOverlay();
        }

        private void OnDestroy()
        {
            if (StartupTimeline.Session != null)
                StartupTimeline.Session.SummaryWritten -= OnSummaryWritten;
            if (Instance == this)
                Instance = null;
        }

        private void OnSummaryWritten(StartupSummaryKind kind, string json)
        {
            if (kind == StartupSummaryKind.Launch)
                _launchJson = json;
            else
                _playJson = json;
        }

        private void ConsumeRunFile()
        {
            // Only desktop and Steam harnesses hand off a run file; mobile and web launches have none
            if (string.IsNullOrEmpty(_options.RunFilePath))
                return;
            var session = StartupTimeline.Session;
            var runFile = BenchmarkRunFile.Consume(_options.RunFilePath, Application.version,
                StartupClocks.MonotonicNowNs(), out _runFileError);
            if (runFile == null || session.ZeroMonotonicNs == null)
                return;
            session.LaunchRequestOffsetMs = (session.ZeroMonotonicNs.Value - runFile.monotonicNs) / 1_000_000.0;
            if (!string.IsNullOrEmpty(runFile.runId))
                _runId = BenchmarkOptions.SanitizeRunId(runFile.runId);
        }

        private IEnumerator Start()
        {
            var session = StartupTimeline.Session;

            // Launch: press through the title screen as early as it accepts input, then wait for the Main Menu
            var pressedTitle = false;
            while (!session.IsLaunchSummaryWritten)
            {
                if (!pressedTitle)
                {
                    var titleScreen = FindObjectsByType<TitleScreen>().FirstOrDefault(title => title.IsReadyForInput);
                    if (titleScreen != null)
                    {
                        pressedTitle = true;
                        titleScreen.GoToMainMenu();
                    }
                }

                yield return null;
            }

            if (!session.HasMark(StartupMilestone.MainMenuInteractive))
            {
                Finish();
                yield break;
            }

            yield return ShowMarker(Marker.Menu);

            // A first-launch welcome prompt is declined, which opens nothing and saves nothing in benchmark mode
            if (CardGameManager.Instance.Messenger.IsShowing)
                CardGameManager.Instance.Messenger.ChooseNo();

            if (_options.StartDelayMs > 0)
                yield return new WaitForSecondsRealtime((float)(_options.StartDelayMs / 1000.0));

            yield return EnsureBenchmarkGame();
            if (_gameLoadFailure != null)
            {
                session.Mark(StartupMilestone.StartGameChosen);
                session.MarkFirstCardActionFailed(_gameLoadFailure);
                Finish();
                yield break;
            }

            var mainMenu = FindObjectsByType<MainMenu>().FirstOrDefault();
            if (mainMenu == null)
            {
                session.Mark(StartupMilestone.StartGameChosen);
                session.MarkFirstCardActionFailed("mainMenuMissing");
                Finish();
                yield break;
            }

            // Start Game is dispatched only after the menu marker has been presented (see ShowMarker)
            mainMenu.StartGame();
            yield return ShowMarker(Marker.Start);

            yield return PlayToFirstCardAction();

            yield return ShowMarker(session.PlayOutcome == StartupTimelineSession.OutcomeOk
                ? Marker.Action
                : Marker.Failed);
            Finish();
        }

        private IEnumerator EnsureBenchmarkGame()
        {
            var manager = CardGameManager.Instance;
            if (CardGameManager.Current.Id == _options.GameId)
                yield break;
            if (!manager.AllCardGames.ContainsKey(_options.GameId))
            {
                // Never report a result for a game that was not measured
                _gameLoadFailure = "benchmarkGameUnavailable";
                yield break;
            }

            // Selection is not persisted in benchmark mode; the run is flagged so it is not used as a budget result
            _gameSwitched = true;
            manager.Select(_options.GameId);
            var deadline = StartupTimeline.ElapsedMs + GameLoadTimeoutMs;
            while (CardGameManager.Current.Id == _options.GameId && !CardGameManager.Current.HasLoaded &&
                   string.IsNullOrEmpty(CardGameManager.Current.Error) && StartupTimeline.ElapsedMs < deadline)
                yield return null;

            if (CardGameManager.Current.Id != _options.GameId || !CardGameManager.Current.HasLoaded)
                _gameLoadFailure = StartupTimeline.ElapsedMs >= deadline
                    ? "benchmarkGameLoadTimeout"
                    : "benchmarkGameLoadFailed";
        }

        private IEnumerator PlayToFirstCardAction()
        {
            var session = StartupTimeline.Session;
            var answeredStartDecks = false;
            double? actionRequestedMs = null;

            while (!session.IsPlaySummaryWritten)
            {
                var now = StartupTimeline.ElapsedMs;
                var playController = PlayController.Instance;
                if (playController != null)
                {
                    var messenger = CardGameManager.Instance.Messenger;
                    var dealer = FindObjectsByType<HandDealer>().FirstOrDefault(d => d.gameObject.activeInHierarchy);
                    if (actionRequestedMs == null && dealer != null)
                    {
                        dealer.Confirm();
                        actionRequestedMs = now;
                    }
                    else if (messenger.IsShowing)
                    {
                        if (!answeredStartDecks && messenger.CurrentMessageKey == StartDecksMessageKey)
                        {
                            answeredStartDecks = true;
                            messenger.ChooseYes();
                        }
                        else
                            messenger.ChooseNo();
                    }
                    else if (actionRequestedMs == null &&
                             session.GetMark(StartupMilestone.TablePlayable) is { } tableMs &&
                             now - tableMs >= DealWithoutDealerAfterMs)
                    {
                        // Games without a starting hand get one card drawn through the same deal path
                        playController.DealHand(1);
                        actionRequestedMs = now;
                    }

                    if (actionRequestedMs.HasValue && now - actionRequestedMs.Value >= ActionTimeoutMs &&
                        !session.HasMark(StartupMilestone.FirstCardAction))
                        StartupTimeline.MarkFirstCardActionFailed("noCardDrawn");
                }

                yield return null;
            }
        }

        private void CreateOverlay()
        {
            var canvasObject = new GameObject("BenchmarkOverlay");
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            canvasObject.AddComponent<CanvasScaler>();
            // Markers ignore raycasts; only the results panel (mobile) receives taps
            canvasObject.AddComponent<GraphicRaycaster>();

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            foreach (Marker marker in Enum.GetValues(typeof(Marker)))
            {
                var markerObject = new GameObject("Marker" + marker);
                markerObject.transform.SetParent(canvasObject.transform, false);
                var image = markerObject.AddComponent<Image>();
                image.color = MarkerColors[(int)marker];
                image.raycastTarget = false;
                var rect = (RectTransform)markerObject.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = MarkerAnchors[(int)marker];
                rect.sizeDelta = new Vector2(MarkerSize, MarkerSize);
                rect.anchoredPosition = Vector2.zero;

                var label = new GameObject("Run").AddComponent<Text>();
                label.transform.SetParent(markerObject.transform, false);
                label.font = font;
                label.fontSize = 28;
                label.color = Color.black;
                label.alignment = TextAnchor.MiddleCenter;
                label.raycastTarget = false;
                label.text = _runId;
                var labelRect = label.rectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;

                markerObject.SetActive(false);
                _markers[marker] = markerObject;
            }

            _resultsText = new GameObject("Results").AddComponent<Text>();
            _resultsText.transform.SetParent(canvasObject.transform, false);
            _resultsText.font = font;
            _resultsText.fontSize = 24;
            _resultsText.color = Color.white;
            _resultsText.raycastTarget = false;
            var resultsRect = _resultsText.rectTransform;
            resultsRect.anchorMin = new Vector2(0.1f, 0.3f);
            resultsRect.anchorMax = new Vector2(0.9f, 0.7f);
            resultsRect.offsetMin = resultsRect.offsetMax = Vector2.zero;
            _resultsText.gameObject.SetActive(false);
        }

        /// <summary>Shows a marker and waits until the frame containing it has been presented.</summary>
        private IEnumerator ShowMarker(Marker marker)
        {
            _markers[marker].SetActive(true);
            yield return new WaitForEndOfFrame();
            // The next frame starting means the frame with the marker has been presented
            yield return null;
            _markersPresentedMs[marker] = StartupTimeline.ElapsedMs;
        }

        private void Finish()
        {
            var json = BuildResult();
            Debug.Log(LogPrefix + json);
            StartupClocks.PublishWebResult(json);
            WriteResultFile(json);

#if UNITY_ANDROID || UNITY_IOS
            ShowResults(IsSuccess());
#elif UNITY_STANDALONE && !UNITY_EDITOR
            Application.Quit(IsSuccess() ? 0 : 1);
#endif
        }

        private bool IsSuccess()
        {
            var session = StartupTimeline.Session;
            return session.HasMark(StartupMilestone.MainMenuInteractive) &&
                   session.PlayOutcome == StartupTimelineSession.OutcomeOk;
        }

        private void WriteResultFile(string json)
        {
#if !UNITY_WEBGL
            try
            {
                _resultPath = !string.IsNullOrEmpty(_options.ResultFilePath)
                    ? _options.ResultFilePath
                    : Path.Combine(Application.persistentDataPath, "benchmark", $"run-{_runId}.json");
                var directory = Path.GetDirectoryName(_resultPath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);
                File.WriteAllText(_resultPath, json);
            }
            catch (Exception exception)
            {
                Debug.LogError(LogPrefix + "could not write result file: " + exception.Message);
                _resultPath = null;
            }
#endif
        }

        private void ShowResults(bool ok)
        {
            var session = StartupTimeline.Session;
            _resultsText.text = $"Run {_runId}: {(ok ? "OK" : "FAILED")}\n" +
                                $"Menu interactive: {StartupTimelineSession.Number(session.GetMark(StartupMilestone.MainMenuInteractive))} ms\n" +
                                $"First card action: {StartupTimelineSession.Number(session.GetMark(StartupMilestone.FirstCardAction))} ms\n" +
                                "Tap to share results";
            _resultsText.gameObject.SetActive(true);
            _resultsText.raycastTarget = true;
            var button = _resultsText.gameObject.AddComponent<Button>();
            button.onClick.AddListener(ShareResults);
        }

        private void ShareResults()
        {
            if (!string.IsNullOrEmpty(_resultPath))
                TextSharing.ShareFile(_resultPath, feedback => Debug.Log(LogPrefix + feedback), "application/json");
        }

        private string BuildResult()
        {
            var session = StartupTimeline.Session;
            var json = new StringBuilder("{\"benchmark\":1");
            AppendString(json, "runId", _runId);
            AppendString(json, "build", Application.version);
            AppendString(json, "platform", Application.platform.ToString());
            AppendString(json, "device", SystemInfo.deviceModel);
            AppendString(json, "os", SystemInfo.operatingSystem);
            AppendString(json, "graphics", SystemInfo.graphicsDeviceType.ToString());
            AppendString(json, "source", _options.Source);
            AppendString(json, "gameId", _options.GameId);
            json.Append(",\"startDelayMs\":").Append(StartupTimelineSession.Number(_options.StartDelayMs));
            json.Append(",\"gameSwitched\":").Append(_gameSwitched ? "true" : "false");
            json.Append(",\"runFileError\":")
                .Append(_runFileError == null ? "null" : "\"" + StartupTimelineSession.Escape(_runFileError) + "\"");
            json.Append(",\"markersPresentedMs\":{");
            var first = true;
            foreach (var (marker, ms) in _markersPresentedMs)
            {
                if (!first)
                    json.Append(',');
                first = false;
                json.Append('"').Append(StartupTimelineSession.ToCamelCase(marker.ToString())).Append("\":")
                    .Append(StartupTimelineSession.Number(ms));
            }

            json.Append('}');
            AppendString(json, "outcome", IsSuccess() ? StartupTimelineSession.OutcomeOk : StartupTimelineSession.OutcomeFailed);
            string failed = null;
            if (!session.HasMark(StartupMilestone.MainMenuInteractive))
                failed = "mainMenuInteractive";
            else if (session.FailedMilestone != null)
                failed = StartupTimelineSession.ToCamelCase(session.FailedMilestone);
            json.Append(",\"failedMilestone\":").Append(failed == null ? "null" : "\"" + failed + "\"");
            json.Append(",\"launch\":").Append(_launchJson ?? "null");
            json.Append(",\"play\":").Append(_playJson ?? "null");
            json.Append('}');
            return json.ToString();
        }

        private static void AppendString(StringBuilder json, string name, string value)
        {
            json.Append(",\"").Append(name).Append("\":\"").Append(StartupTimelineSession.Escape(value))
                .Append('"');
        }
    }
}
