/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.IO;
using Cgs;
using Cgs.Performance;
using NUnit.Framework;

namespace Tests.PlayMode
{
    public class StartupTimelineTests
    {
        private double _now;
        private List<string> _log;
        private List<(StartupSummaryKind kind, string json)> _summaries;

        private StartupTimelineSession CreateSession()
        {
            _now = 0;
            _log = new List<string>();
            _summaries = new List<(StartupSummaryKind, string)>();
            var session = new StartupTimelineSession(() => _now, _log.Add, 30_000, 60_000);
            session.SummaryWritten += (kind, json) => _summaries.Add((kind, json));
            return session;
        }

        private void At(double ms) => _now = ms;

        [Test]
        public void MarksKeepFirstOccurrenceInOrder()
        {
            var session = CreateSession();
            At(1);
            session.Mark(StartupMilestone.EngineReady);
            At(20);
            session.Mark(StartupMilestone.FirstSceneLoaded);
            At(40);
            Assert.IsFalse(session.Mark(StartupMilestone.FirstSceneLoaded));

            Assert.AreEqual(1, session.GetMark(StartupMilestone.EngineReady));
            Assert.AreEqual(20, session.GetMark(StartupMilestone.FirstSceneLoaded));
            Assert.IsNull(session.GetMark(StartupMilestone.FirstFrameDrawn));
        }

        [Test]
        public void MenuOnlySessionWritesExactlyOneLaunchSummary()
        {
            var session = CreateSession();
            session.Mark(StartupMilestone.EngineReady);
            At(300);
            session.Mark(StartupMilestone.MainMenuInteractive);
            At(500_000);
            session.Tick();
            session.Mark(StartupMilestone.MainMenuInteractive);

            Assert.AreEqual(1, _summaries.Count);
            Assert.AreEqual(StartupSummaryKind.Launch, _summaries[0].kind);
            StringAssert.Contains("\"outcome\":\"ok\"", _summaries[0].json);
            StringAssert.Contains("\"mainMenuInteractive\":300", _summaries[0].json);
            Assert.AreEqual(1, _log.Count);
            StringAssert.StartsWith(StartupTimelineSession.LogPrefix, _log[0]);
        }

        [Test]
        public void PlaySummaryIsWrittenAfterFirstCardActionSucceeds()
        {
            var session = CreateSession();
            session.Mark(StartupMilestone.MainMenuInteractive);
            At(400);
            session.Mark(StartupMilestone.StartGameChosen);
            At(600);
            session.Mark(StartupMilestone.PlaySceneLoaded);
            At(700);
            session.Mark(StartupMilestone.TablePlayable);
            Assert.IsFalse(session.IsPlaySummaryWritten, "The table becoming playable must not end the play summary");

            At(750);
            session.Mark(StartupMilestone.FirstCardAction);

            Assert.AreEqual(2, _summaries.Count);
            var play = _summaries[1].json;
            Assert.AreEqual(StartupSummaryKind.Play, _summaries[1].kind);
            StringAssert.Contains("\"outcome\":\"ok\"", play);
            StringAssert.Contains("\"tablePlayable\":700", play);
            StringAssert.Contains("\"firstCardAction\":750", play);
        }

        [Test]
        public void TableReadyButFirstActionFailsReportsNoPlayableTime()
        {
            var session = CreateSession();
            session.Mark(StartupMilestone.MainMenuInteractive);
            session.Mark(StartupMilestone.StartGameChosen);
            At(700);
            session.Mark(StartupMilestone.TablePlayable);
            At(5_700);
            session.MarkFirstCardActionFailed("noCardDrawn");
            At(5_800);
            session.Mark(StartupMilestone.FirstCardAction);

            var plays = _summaries.FindAll(summary => summary.kind == StartupSummaryKind.Play);
            Assert.AreEqual(1, plays.Count);
            var play = plays[0].json;
            StringAssert.Contains("\"outcome\":\"failed\"", play);
            StringAssert.Contains("\"tablePlayable\":null", play);
            StringAssert.Contains("\"firstCardAction\":null", play);
            StringAssert.Contains("\"failedMilestone\":\"firstCardAction\"", play);
            StringAssert.Contains("\"tableReadyMs\":700", play);
        }

        [Test]
        public void LaunchTimeoutMarksMenuNotReached()
        {
            var session = CreateSession();
            session.Mark(StartupMilestone.EngineReady);
            At(30_000);
            session.Tick();

            Assert.AreEqual(1, _summaries.Count);
            StringAssert.Contains("\"outcome\":\"timeout\"", _summaries[0].json);
            StringAssert.Contains("\"mainMenuInteractive\":null", _summaries[0].json);
            StringAssert.Contains("\"failedMilestone\":\"mainMenuInteractive\"", _summaries[0].json);
        }

        [Test]
        public void PlayTimeoutMarksFirstMissingMilestone()
        {
            var session = CreateSession();
            session.Mark(StartupMilestone.MainMenuInteractive);
            At(1_000);
            session.Mark(StartupMilestone.StartGameChosen);
            At(1_500);
            session.Mark(StartupMilestone.PlaySceneLoaded);
            At(61_000);
            session.Tick();

            var play = _summaries[^1].json;
            StringAssert.Contains("\"summary\":\"play\"", play);
            StringAssert.Contains("\"outcome\":\"timeout\"", play);
            StringAssert.Contains("\"failedMilestone\":\"tablePlayable\"", play);
        }

        [Test]
        public void OnlyTheFirstGameOfASessionIsSummarized()
        {
            var session = CreateSession();
            session.Mark(StartupMilestone.MainMenuInteractive);
            session.Mark(StartupMilestone.StartGameChosen);
            session.Mark(StartupMilestone.TablePlayable);
            session.Mark(StartupMilestone.FirstCardAction);
            session.Mark(StartupMilestone.StartGameChosen);
            session.MarkFirstCardActionFailed("second game");
            At(200_000);
            session.Tick();

            Assert.AreEqual(1, _summaries.FindAll(summary => summary.kind == StartupSummaryKind.Play).Count);
        }

        [Test]
        public void ArgumentsEnableBenchmarkMode()
        {
            var options = BenchmarkOptions.Parse(new[]
            {
                "cgs", "-cgsBenchmark", "-cgsRunFile=/tmp/run.json", "-cgsResultFile=/tmp/result.json",
                "-cgsStartDelayMs=250", "-cgsRun=7"
            }, null);

            Assert.IsTrue(options.IsEnabled);
            Assert.AreEqual("arguments", options.Source);
            Assert.AreEqual("/tmp/run.json", options.RunFilePath);
            Assert.AreEqual("/tmp/result.json", options.ResultFilePath);
            Assert.AreEqual(250, options.StartDelayMs);
            Assert.AreEqual("7", options.RunId);
            Assert.AreEqual(Tags.StandardPlayingCardsDirectoryName, options.GameId);
        }

        [Test]
        public void LaunchLinkEnablesBenchmarkModeWithoutUsingGameUrl()
        {
            var options = BenchmarkOptions.Parse(new[] { "cgs" },
                "cardgamesim://main?benchmark=1&run=3&delay=0&url=https%3A%2F%2Fexample.com%2Fcgs.json");

            Assert.IsTrue(options.IsEnabled);
            Assert.AreEqual("deepLink", options.Source);
            Assert.AreEqual("3", options.RunId);
            Assert.AreEqual(0, options.StartDelayMs);
            Assert.IsNull(options.RunFilePath);
            Assert.IsNull(options.ResultFilePath);
        }

        [Test]
        public void OrdinaryLinksDoNotEnableBenchmarkMode()
        {
            Assert.IsFalse(BenchmarkOptions.Parse(new[] { "cgs" }, "cardgamesim://main?url=x").IsEnabled);
            Assert.IsFalse(BenchmarkOptions.Parse(new[] { "cgs" }, "cardgamesim://main?benchmark=0").IsEnabled);
            Assert.IsFalse(BenchmarkOptions.Parse(null, null).IsEnabled);
            Assert.IsTrue(BenchmarkOptions.IsBenchmarkLink("https://cgs.example/?benchmark=true"));
            Assert.IsFalse(BenchmarkOptions.IsBenchmarkLink("not a url"));
        }

        [Test]
        public void RunFileIsAcceptedOnceAndRejectedWhenStaleOrMismatched()
        {
            var directory = Path.Combine(Path.GetTempPath(), "cgs-run-file-tests-" + System.Guid.NewGuid());
            Directory.CreateDirectory(directory);
            try
            {
                const long now = 500_000_000_000;
                var valid = WriteRunFile(directory, "valid", "1.2.3", now - 2_000_000_000);
                var runFile = BenchmarkRunFile.Consume(valid, "1.2.3", now, out var error);
                Assert.IsNull(error);
                Assert.AreEqual("valid", runFile.runId);

                Assert.IsNull(BenchmarkRunFile.Consume(valid, "1.2.3", now, out error));
                Assert.AreEqual("runFileConsumed", error);

                var stale = WriteRunFile(directory, "stale", "1.2.3", now - 200_000_000_000);
                Assert.IsNull(BenchmarkRunFile.Consume(stale, "1.2.3", now, out error));
                Assert.AreEqual("runFileStale", error);

                var mismatch = WriteRunFile(directory, "mismatch", "9.9.9", now - 1_000_000);
                Assert.IsNull(BenchmarkRunFile.Consume(mismatch, "1.2.3", now, out error));
                Assert.AreEqual("runFileBuildMismatch", error);

                var future = WriteRunFile(directory, "future", "1.2.3", now + 1_000_000);
                Assert.IsNull(BenchmarkRunFile.Consume(future, "1.2.3", now, out error));
                Assert.AreEqual("runFileClockMismatch", error);

                Assert.IsNull(BenchmarkRunFile.Consume(Path.Combine(directory, "missing.json"), "1.2.3", now,
                    out error));
                Assert.AreEqual("runFileMissing", error);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        private static string WriteRunFile(string directory, string runId, string build, long monotonicNs)
        {
            var path = Path.Combine(directory, runId + ".json");
            File.WriteAllText(path,
                $"{{\"runId\":\"{runId}\",\"build\":\"{build}\",\"monotonicNs\":{monotonicNs}}}");
            return path;
        }
    }
}
