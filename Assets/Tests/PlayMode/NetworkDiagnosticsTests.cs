/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cgs.Menu;
using Cgs.Play.Multiplayer;
using NUnit.Framework;
using UnityEngine;

namespace Tests.PlayMode
{
    public class NetworkDiagnosticsTests
    {
        private GameObject _gameObject;
        private CgsNetDiagnostics _diagnostics;
        private bool _previousDeveloperMode;
        private object _previousRecorder;
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

        private static FieldInfo RecorderField => typeof(CgsNetDiagnostics)
            .GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);

        private string[] Entries => ((Queue<string>)typeof(CgsNetDiagnostics)
            .GetField("_entries", PrivateInstance).GetValue(_diagnostics)).ToArray();

        [SetUp]
        public void SetUp()
        {
            _previousDeveloperMode = Settings.DeveloperMode;
            _previousRecorder = RecorderField.GetValue(null);
            Settings.DeveloperMode = false;
            _gameObject = new GameObject("Diagnostics test");
            _gameObject.AddComponent<CgsNetManager>();
            _diagnostics = _gameObject.AddComponent<CgsNetDiagnostics>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
            RecorderField.SetValue(null, _previousRecorder);
            Settings.DeveloperMode = _previousDeveloperMode;
        }

        private void SetRecording(bool enabled)
        {
            Settings.DeveloperMode = enabled;
            // Apply the setting synchronously, without running CgsNetManager.Start (Unity Services).
            typeof(CgsNetDiagnostics).GetMethod("Update", PrivateInstance).Invoke(_diagnostics, null);
        }

        [Test]
        public void DisabledDiagnosticsDoNotCaptureOrCreateToolbar()
        {
            SetRecording(false);
            CgsNetDiagnostics.Record("test-event");
            Assert.IsEmpty(Entries);
            Assert.IsNull(_gameObject.transform.Find("Network diagnostics"));
        }

        [Test]
        public void EnablingAndDisablingControlsCaptureWithoutStartingNetwork()
        {
            SetRecording(true);
            CgsNetDiagnostics.Record("captured-event");
            Assert.IsTrue(Entries.Any(entry => entry.Contains("captured-event")));
            Assert.IsTrue(_gameObject.transform.Find("Network diagnostics").gameObject.activeSelf);
            Assert.IsFalse(_gameObject.GetComponent<CgsNetManager>().IsListening);

            SetRecording(false);
            var count = Entries.Length;
            CgsNetDiagnostics.Record("disabled-event");
            Assert.AreEqual(count, Entries.Length);
            Assert.IsFalse(_gameObject.transform.Find("Network diagnostics").gameObject.activeSelf);
        }

        [Test]
        public void SamplingIgnoresChangingPositionButSeparatesSendersAndStages()
        {
            SetRecording(true);
            CgsNetDiagnostics.Record("test-position", details: "position=1", sampled: true, sampleKey: "1");
            CgsNetDiagnostics.Record("test-position", details: "position=2", sampled: true, sampleKey: "1");
            CgsNetDiagnostics.Record("test-position", details: "position=3", sampled: true, sampleKey: "2");
            CgsNetDiagnostics.Record("test-applied", sampled: true, sampleKey: "1");
            Assert.AreEqual(2, Entries.Count(entry => entry.Contains("test-position")));
            Assert.IsTrue(Entries.Any(entry => entry.Contains("test-applied")));
        }

        [Test]
        public void LongCaptureRetainsNewestEventsAndBoundsEntryLength()
        {
            SetRecording(true);
            for (var i = 0; i < 2010; i++)
                CgsNetDiagnostics.Record("bounded-event", details: $"sequence={i:D4}");

            var entries = Entries;
            Assert.AreEqual(2000, entries.Length);
            StringAssert.Contains("sequence=0010", entries[0]);
            StringAssert.Contains("sequence=2009", entries[^1]);
            CgsNetDiagnostics.Record("long-event", details: new string('x', 5000));
            Assert.LessOrEqual(Entries[^1].Length, 2000);
        }
    }
}
