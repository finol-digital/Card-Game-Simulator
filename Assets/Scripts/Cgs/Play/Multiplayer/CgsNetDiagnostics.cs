/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Cgs.CardGameView.Multiplayer;
using Cgs.Menu;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;

namespace Cgs.Play.Multiplayer
{
    /// <summary>Local, bounded diagnostics enabled by Settings > Developer Mode.</summary>
    public class CgsNetDiagnostics : MonoBehaviour
    {
        private const int MaxEntries = 2000;
        private const int MaxEntryLength = 2000;
        private const string ExportError = "Could not export multiplayer diagnostics: ";
        private readonly Queue<string> _entries = new();
        private readonly Dictionary<string, float> _lastSamples = new();
        private readonly List<RaycastResult> _hits = new();
        private static CgsNetDiagnostics _instance;
        private CgsNetManager _manager;
        private GameObject _toolbar;
        private Text _status;
        private bool _recording;
        private float _nextStatusTime;
        private string _lastState;
        private string _header;
        private int _marker;

        public static bool IsRecording => _instance != null && _instance._recording;

        public static string PointerState
        {
            get
            {
                var pressed = 0;
                if (Touchscreen.current != null)
                    foreach (var touch in Touchscreen.current.touches)
                        if (touch.press.isPressed)
                            pressed++;
                var enhancedCount = EnhancedTouchSupport.enabled
                    ? UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count : -1;
                return $"touchDevice={Touchscreen.current?.deviceId} rawPressed={pressed} " +
                       $"enhancedTouches={enhancedCount} focused={Application.isFocused}";
            }
        }

        protected void Awake()
        {
            RegisterRecorder(this);
            _manager = GetComponent<CgsNetManager>();
            _manager.OnClientConnectedCallback += OnConnected;
            _manager.OnClientDisconnectCallback += OnDisconnected;
            Application.logMessageReceived += OnLog;
        }

        private static void RegisterRecorder(CgsNetDiagnostics recorder)
        {
            _instance = recorder;
        }

        private static void UnregisterRecorder(CgsNetDiagnostics recorder)
        {
            if (_instance == recorder)
                _instance = null;
        }

        protected void Update()
        {
            if (_recording != Settings.DeveloperMode)
            {
                _recording = Settings.DeveloperMode;
                if (_recording)
                {
                    if (_toolbar == null)
                        CreateToolbar();
                    StartCapture();
                }
                if (_toolbar != null)
                    _toolbar.SetActive(_recording);
            }
            if (!_recording)
                return;

            if (Time.unscaledTime >= _nextStatusTime)
            {
                _nextStatusTime = Time.unscaledTime + 1;
                var state = ConnectionState();
                if (state != _lastState)
                {
                    Record("connection-state", details: state);
                    _lastState = state;
                }
                var role = "Offline";
                if (_manager.IsHost)
                    role = "Host";
                else if (_manager.IsConnectedClient)
                    role = "Client";
                _status.text = $"NET TRACE  {role} " +
                               $"{_manager.LocalClientId}\n{_entries.Count}/{MaxEntries} events";
            }

            if (Touchscreen.current != null)
                foreach (var touch in Touchscreen.current.touches)
                    if (touch.press.wasPressedThisFrame)
                        TraceHit(touch.position.ReadValue(), touch.touchId.ReadValue(), "touch");
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                TraceHit(Mouse.current.position.ReadValue(), -1, "mouse");
        }

        private void StartCapture()
        {
            _entries.Clear();
            _lastSamples.Clear();
            _marker = 0;
            _lastState = null;
            _header = $"CGS network trace {DateTime.UtcNow:O}\n" +
                      $"app={Application.version} build={Application.buildGUID} unity={Application.unityVersion}\n" +
                      $"platform={Application.platform} os={SystemInfo.operatingSystem} device={SystemInfo.deviceModel}\n" +
                      $"netcode={typeof(NetworkManager).Assembly.GetName().Version} " +
                      $"input={typeof(InputSystem).Assembly.GetName().Version}\n" +
                      "High-frequency events are sampled once per second per stage/object/sender.\n";
            Snapshot();
        }

        public static void Record(string stage, NetworkBehaviour subject = null, string details = null,
            bool sampled = false, string sampleKey = null)
        {
            if (!IsRecording)
                return;
            var recorder = _instance;
            if (sampled)
            {
                var key = $"{stage}/{subject?.GetInstanceID()}/{sampleKey}";
                if (recorder._lastSamples.TryGetValue(key, out var last) && Time.unscaledTime - last < 1)
                    return;
                if (recorder._lastSamples.Count >= 512)
                    recorder._lastSamples.Clear();
                recorder._lastSamples[key] = Time.unscaledTime;
            }
            var identity = subject == null ? "session" :
                $"{subject.GetType().Name} instance={subject.GetInstanceID()} object={subject.NetworkObjectId} " +
                $"spawned={subject.IsSpawned} owner={subject.OwnerClientId} isOwner={subject.IsOwner}";
            recorder.Append($"{DateTime.UtcNow:O} frame={Time.frameCount} local={recorder._manager.LocalClientId} " +
                            $"server={recorder._manager.IsServer} {stage} {identity} {details}");
        }

        private void Append(string entry)
        {
            if (_entries.Count == MaxEntries)
                _entries.Dequeue();
            _entries.Enqueue(entry.Length > MaxEntryLength ? entry.Substring(0, MaxEntryLength) : entry);
        }

        private string ConnectionState()
        {
            var player = _manager.LocalPlayer;
            return $"listening={_manager.IsListening} connected={_manager.IsConnectedClient} " +
                   $"host={_manager.IsHost} local={_manager.LocalClientId} " +
                   $"playerSpawned={player != null && player.IsSpawned} " +
                   $"playerOwner={player != null && player.IsOwner} " +
                   $"objects={_manager.SpawnManager?.SpawnedObjects.Count} " +
                   $"inputModule={EventSystem.current?.currentInputModule?.GetType().Name}";
        }

        private void OnConnected(ulong clientId) => Record("connected", details: $"client={clientId}");
        private void OnDisconnected(ulong clientId) => Record("disconnected", details: $"client={clientId}");

        private void OnLog(string message, string stackTrace, LogType type)
        {
            if (IsRecording && type != LogType.Log)
                Record("unity-" + type, details: message + "\n" + stackTrace, sampled: true,
                    sampleKey: message.Length > 100 ? message.Substring(0, 100) : message);
        }

        private void TraceHit(Vector2 position, int pointerId, string device)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                Record("input-press", details: $"device={device} pointer={pointerId} no-event-system");
                return;
            }
            _hits.Clear();
            eventSystem.RaycastAll(new PointerEventData(eventSystem) { position = position }, _hits);
            var hit = _hits.Count > 0 ? _hits[0].gameObject : null;
            // Ignore the trace toolbar itself, which intentionally intercepts its own buttons only.
            if (hit != null && hit.transform.IsChildOf(_toolbar.transform))
                return;
            var playable = hit != null ? hit.GetComponentInParent<CgsNetPlayable>() : null;
            Record("input-press", playable,
                $"device={device} pointer={pointerId} screen={position} hits={_hits.Count} top={hit?.name}");
        }

        private void Snapshot()
        {
            Record("marker", details: $"number={++_marker} {ConnectionState()}");
            var game = CardGameManager.Current;
            Record("game", details: $"id={game?.Id} loading={game?.IsLoading} " +
                                    $"downloading={game?.IsDownloading} cards={game?.Cards.Count}");
            if (game != null)
            {
                try
                {
                    if (File.Exists(game.GameFilePath))
                    {
                        using var stream = File.OpenRead(game.GameFilePath);
                        using var sha = SHA256.Create();
                        Record("game-config", details: "sha256=" +
                            BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty));
                    }
                    else
                        Record("game-config", details: "file-missing");
                }
                catch (Exception exception)
                {
                    Record("game-config-unavailable", details: exception.Message);
                }
            }
            var selected = EventSystem.current?.currentSelectedGameObject;
            if (selected != null && selected.TryGetComponent<CgsNetPlayable>(out var playable))
                Record("selected", playable);
        }

        private void Export()
        {
            Snapshot();
            var report = _header + string.Join("\n", _entries);
            try
            {
                var path = Path.Combine(Application.temporaryCachePath, "cgs-network-trace.txt");
                File.WriteAllText(path, report);
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
                new NativeShare().AddFile(path, "text/plain").Share();
#else
                UniClipboard.SetText(report);
                _status.text = "Trace copied\nto clipboard";
                _nextStatusTime = Time.unscaledTime + 3;
#endif
            }
            catch (Exception exception)
            {
                Debug.LogError(ExportError + exception.Message);
            }
        }

        private void CreateToolbar()
        {
            _toolbar = new GameObject("Network diagnostics", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            _toolbar.transform.SetParent(transform, false);
            var canvas = _toolbar.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            var scaler = _toolbar.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            var panel = new GameObject("Trace controls", typeof(RectTransform), typeof(Image),
                typeof(HorizontalLayoutGroup));
            panel.transform.SetParent(_toolbar.transform, false);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -35);
            rect.sizeDelta = new Vector2(580, 60);
            panel.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.12f, 0.95f);
            var layout = panel.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 6, 6);
            layout.spacing = 6;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;
            _status = AddText(panel.transform, "NET TRACE");
            AddButton(panel.transform, "Mark / snapshot", Snapshot);
            AddButton(panel.transform, "Export trace", Export);
            AddButton(panel.transform, "Clear trace", StartCapture);
        }

        private static Text AddText(Transform parent, string value)
        {
            var label = new GameObject(value, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(parent, false);
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 16;
            label.text = value;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        private static void AddButton(Transform parent, string label, UnityAction action)
        {
            var button = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            button.transform.SetParent(parent, false);
            button.GetComponent<Image>().color = new Color(0.15f, 0.22f, 0.3f);
            button.GetComponent<Button>().onClick.AddListener(action);
            var text = (RectTransform)AddText(button.transform, label).transform;
            text.anchorMin = Vector2.zero;
            text.anchorMax = Vector2.one;
            text.offsetMin = text.offsetMax = Vector2.zero;
        }

        protected void OnDestroy()
        {
            Application.logMessageReceived -= OnLog;
            if (_manager != null)
            {
                _manager.OnClientConnectedCallback -= OnConnected;
                _manager.OnClientDisconnectCallback -= OnDisconnected;
            }
            UnregisterRecorder(this);
        }
    }
}
