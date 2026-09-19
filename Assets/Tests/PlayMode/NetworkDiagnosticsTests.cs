/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cgs.CardGameView.Multiplayer;
using Cgs.Menu;
using Cgs.Play.Multiplayer;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    public class NetworkDiagnosticsTests
    {
        private GameObject _gameObject;
        private GameObject _networkGameObject;
        private CgsNetManager _manager;
        private NetworkManager _previousNetworkManager;
        private CgsNetDiagnostics _diagnostics;
        private bool _previousDeveloperMode;
        private object _previousRecorder;
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

        private static FieldInfo RecorderField => typeof(CgsNetDiagnostics)
            .GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);

        private string[] Entries => ((Queue<string>)typeof(CgsNetDiagnostics)
            .GetField("_entries", PrivateInstance).GetValue(_diagnostics)).ToArray();

        private GameObject Toolbar => (GameObject)typeof(CgsNetDiagnostics)
            .GetField("_toolbar", PrivateInstance).GetValue(_diagnostics);

        [SetUp]
        public void SetUp()
        {
            _previousDeveloperMode = Settings.DeveloperMode;
            _previousRecorder = RecorderField.GetValue(null);
            _previousNetworkManager = NetworkManager.Singleton;
            Settings.DeveloperMode = false;
            _networkGameObject = new GameObject("Persistent network manager");
            _manager = _networkGameObject.AddComponent<CgsNetManager>();
            _manager.SetSingleton();
            // Avoid starting Unity Services; diagnostics only needs local manager state.
            _manager.enabled = false;
            _gameObject = new GameObject("Play canvas", typeof(RectTransform), typeof(Canvas));
            _diagnostics = _gameObject.AddComponent<CgsNetDiagnostics>();
        }

        [TearDown]
        public void TearDown()
        {
            if (Toolbar != null)
                Object.DestroyImmediate(Toolbar);
            Object.DestroyImmediate(_gameObject);
            Object.DestroyImmediate(_networkGameObject);
            if (_previousNetworkManager != null)
                _previousNetworkManager.SetSingleton();
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
            Assert.IsNull(Toolbar);
        }

        [Test]
        public void EnablingAndDisablingControlsCaptureWithoutStartingNetwork()
        {
            SetRecording(true);
            CgsNetDiagnostics.Record("captured-event");
            Assert.IsTrue(Entries.Any(entry => entry.Contains("captured-event")));
            Assert.IsTrue(Toolbar.activeSelf);
            Assert.IsFalse(_manager.IsListening);

            SetRecording(false);
            var count = Entries.Length;
            CgsNetDiagnostics.Record("disabled-event");
            Assert.AreEqual(count, Entries.Length);
            Assert.IsFalse(Toolbar.activeSelf);
        }

        [Test]
        public void ToolbarKeepsIndependentCanvasScalingInPlayScene()
        {
            SetRecording(true);

            Assert.IsNull(_manager.GetComponent<CgsNetDiagnostics>());
            Assert.IsTrue(Toolbar.GetComponent<Canvas>().isRootCanvas);
            Assert.AreEqual(_gameObject.scene, Toolbar.scene);
            Assert.AreEqual(new Vector2(1280, 720),
                Toolbar.GetComponent<UnityEngine.UI.CanvasScaler>().referenceResolution);
        }

        [UnityTest]
        public IEnumerator LeavingPlayDestroysToolbarButKeepsNetworkManager()
        {
            SetRecording(true);
            var toolbar = Toolbar;

            Object.Destroy(_gameObject);
            yield return null;
            yield return null;

            Assert.IsTrue(toolbar == null);
            Assert.IsTrue(_manager != null);
            Assert.IsTrue(Settings.DeveloperMode);
            Assert.IsFalse(CgsNetDiagnostics.IsRecording);
            Assert.IsNull(RecorderField.GetValue(null));
            Assert.DoesNotThrow(() => CgsNetDiagnostics.Record("back-in-menu"));
        }

        [TestCase(null)]
        [TestCase(true)]
        [TestCase(false)]
        public void PointerTracingHandlesOptionalCanvasGroup(bool? blocksRaycasts)
        {
            SetRecording(true);
            var zone = CreateCardZone();
            if (blocksRaycasts.HasValue)
                zone.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = blocksRaycasts.Value;

            AssertPointerTrace(zone, blocksRaycasts?.ToString() ?? "missing");

            Assert.AreEqual(blocksRaycasts.HasValue, zone.TryGetComponent<CanvasGroup>(out _));
        }

        [Test]
        public void PointerTracingHandlesDestroyedCanvasGroup()
        {
            SetRecording(true);
            var zone = CreateCardZone();
            Object.DestroyImmediate(zone.gameObject.AddComponent<CanvasGroup>());

            AssertPointerTrace(zone, "missing");

            Assert.IsFalse(zone.TryGetComponent<CanvasGroup>(out _));
        }

        private CardZone CreateCardZone()
        {
            var zoneObject = new GameObject("PlayArea CardZone", typeof(RectTransform));
            zoneObject.transform.SetParent(_gameObject.transform, false);
            zoneObject.AddComponent<NetworkObject>();
            return zoneObject.AddComponent<CardZone>();
        }

        private void AssertPointerTrace(CardZone zone, string expectedBlocksRaycasts)
        {
            var pointer = new PointerEventData(EventSystem.current)
            {
                pointerId = 7,
                position = new Vector2(25, 50)
            };

            Assert.DoesNotThrow(() => zone.OnPointerDown(pointer));
            Assert.AreEqual(pointer.position, zone.PointerPositions[pointer.pointerId]);
            StringAssert.Contains("pointer-down", Entries[^1]);
            StringAssert.Contains($"blocksRaycasts={expectedBlocksRaycasts}", Entries[^1]);

            Assert.DoesNotThrow(() => zone.OnPointerUp(pointer));
            Assert.IsEmpty(zone.PointerPositions);
            StringAssert.Contains("pointer-up", Entries[^1]);
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
        public void DestroyingActiveRecorderStopsCapture()
        {
            SetRecording(true);

            Object.DestroyImmediate(_diagnostics);

            Assert.IsNull(RecorderField.GetValue(null));
            Assert.IsFalse(CgsNetDiagnostics.IsRecording);
            Assert.DoesNotThrow(() => CgsNetDiagnostics.Record("after-destroy"));
        }

        [Test]
        public void DestroyingPreviousRecorderPreservesCurrentRecorder()
        {
            var currentRecorder = _gameObject.AddComponent<CgsNetDiagnostics>();

            Object.DestroyImmediate(_diagnostics);

            Assert.AreSame(currentRecorder, RecorderField.GetValue(null));
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
