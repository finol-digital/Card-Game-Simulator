/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Unity.Netcode;
using UnityEngine;

namespace Cgs.Play.Multiplayer
{
    // BroadcastChannel carries Netcode packets between tabs on the same origin. Players on other devices on the
    // same network connect through WebRTC data channels after BrowserNetworkLan exchanges offers through Lobby.
    public sealed class BrowserLanTransport : NetworkTransport, IBrowserLanSignals
    {
        private const string ConnectionFailureMessage = "Unable to start browser LAN {0}: {1}";
        private const string MissingRoomMessage = "a room ID is required.";
        public static bool IsBrowser => Application.platform == RuntimePlatform.WebGLPlayer;
        public override ulong ServerClientId => 0;
        public override bool IsSupported => IsBrowser;
        public string RoomId { get; set; }
        public string ServerName { get; set; }

        private void Awake()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // Newtonsoft constructs dictionary wrappers through reflection. Explicitly
            // retain their value-type specialization for IL2CPP before discovery runs.
            Newtonsoft.Json.Utilities.AotHelper.EnsureDictionary<string, DiscoveryResponseData>();
#endif
        }

        public override void Initialize(NetworkManager networkManager = null)
        {
            // The bridge is opened on demand, also before networking for discovery.
        }

        public override bool StartServer()
        {
            RoomId = Guid.NewGuid().ToString("N");
            return StartConnection(true);
        }

        public override bool StartClient() => StartConnection(false);

        private bool StartConnection(bool server)
        {
            if (string.IsNullOrWhiteSpace(RoomId))
                return ReportConnectionFailure(server, MissingRoomMessage);
#if UNITY_WEBGL && !UNITY_EDITOR
            if (CgsBrowserLanStart(RoomId, ServerName ?? "", server ? 1 : 0) != 0)
                return true;
            const string reason = "the browser bridge could not open the room. Check the room ID and browser support.";
#else
            const string reason = "this transport requires a Web player and cannot run in the Editor or a native player.";
#endif
            return ReportConnectionFailure(server, reason);
        }

        private static bool ReportConnectionFailure(bool server, string reason)
        {
            Debug.LogError(string.Format(ConnectionFailureMessage, server ? "host" : "client", reason));
            return false;
        }

        public override void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery networkDelivery)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // BroadcastChannel preserves packet order; all delivery modes may be reliable.
            CgsBrowserLanSend((int)clientId, Convert.ToBase64String(payload.Array, payload.Offset, payload.Count));
#endif
        }

        public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
        {
            clientId = 0;
            payload = default;
            receiveTime = Time.realtimeSinceStartup;
#if UNITY_WEBGL && !UNITY_EDITOR
            var json = CgsBrowserLanPoll();
            if (!string.IsNullOrEmpty(json))
            {
                var packet = JsonConvert.DeserializeObject<Packet>(json);
                clientId = packet.ClientId;
                if (packet.Data != null)
                    payload = new ArraySegment<byte>(Convert.FromBase64String(packet.Data));
                return packet.Type;
            }
#endif
            return NetworkEvent.Nothing;
        }

        public bool StartDiscovery()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return CgsBrowserLanDiscover() != 0;
#else
            return false;
#endif
        }

        public bool TryReadDiscoveredRooms(out Dictionary<string, DiscoveryResponseData> rooms)
        {
            rooms = null;
#if UNITY_WEBGL && !UNITY_EDITOR
            var json = CgsBrowserLanRooms();
            if (!string.IsNullOrEmpty(json))
            {
                rooms = JsonConvert.DeserializeObject<Dictionary<string, DiscoveryResponseData>>(json);
                return rooms != null;
            }
#endif
            return false;
        }

        public void StopDiscovery()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            CgsBrowserLanStopDiscovery();
#endif
        }

        public override void DisconnectRemoteClient(ulong clientId)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            CgsBrowserLanDisconnect((int)clientId);
#endif
        }

        public override void DisconnectLocalClient() => DisconnectRemoteClient(ServerClientId);
        public override ulong GetCurrentRtt(ulong clientId) => 0;

        public override void Shutdown()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            CgsBrowserLanShutdown();
#endif
        }

        private void OnDestroy()
        {
            Shutdown();
            StopDiscovery();
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [UnityEngine.Scripting.Preserve]
        private sealed class Packet
        {
            [UnityEngine.Scripting.Preserve]
            public NetworkEvent Type { get; set; }
            [UnityEngine.Scripting.Preserve]
            public ulong ClientId { get; set; }
            [UnityEngine.Scripting.Preserve]
            public string Data { get; set; }
        }

        [DllImport("__Internal")] private static extern int CgsBrowserLanStart(string room, string name, int server);
        [DllImport("__Internal")] private static extern void CgsBrowserLanSend(int clientId, string data);
        [DllImport("__Internal")] private static extern string CgsBrowserLanPoll();
        [DllImport("__Internal")] private static extern int CgsBrowserLanDiscover();
        [DllImport("__Internal")] private static extern string CgsBrowserLanRooms();
        [DllImport("__Internal")] private static extern void CgsBrowserLanStopDiscovery();
        [DllImport("__Internal")] private static extern void CgsBrowserLanDisconnect(int clientId);
        [DllImport("__Internal")] private static extern void CgsBrowserLanShutdown();
        [DllImport("__Internal")] private static extern void CgsBrowserLanNetworkKey(int requestId, int fresh, Action<int, IntPtr> callback);
        [DllImport("__Internal")] private static extern void CgsBrowserLanOffer(int requestId, string room, Action<int, IntPtr> callback);
        [DllImport("__Internal")] private static extern void CgsBrowserLanAccept(int requestId, string room, string answer, Action<int, IntPtr> callback);
        [DllImport("__Internal")] private static extern void CgsBrowserLanAnswer(int requestId, string offer, Action<int, IntPtr> callback);
        [DllImport("__Internal")] private static extern void CgsBrowserLanDiscardOffer(string room);
#endif

        private static readonly Dictionary<int, Action<string>> PendingSignals = new();
        private static int _nextSignal;

        public void RequestNetworkKey(bool fresh, Action<string> completed) => Signal(completed,
#if UNITY_WEBGL && !UNITY_EDITOR
            id => CgsBrowserLanNetworkKey(id, fresh ? 1 : 0, CompleteSignal));
#else
            null);
#endif

        public void CreateOffer(string room, Action<string> completed) => Signal(completed,
#if UNITY_WEBGL && !UNITY_EDITOR
            id => CgsBrowserLanOffer(id, room ?? string.Empty, CompleteSignal));
#else
            null);
#endif

        public void AcceptAnswer(string room, string answer, Action<bool> completed) => Signal(
            result => completed?.Invoke(result != null),
#if UNITY_WEBGL && !UNITY_EDITOR
            id => CgsBrowserLanAccept(id, room ?? string.Empty, answer ?? string.Empty, CompleteSignal));
#else
            null);
#endif

        public void AnswerOffer(string offer, Action<string> completed) => Signal(completed,
#if UNITY_WEBGL && !UNITY_EDITOR
            id => CgsBrowserLanAnswer(id, offer ?? string.Empty, CompleteSignal));
#else
            null);
#endif

        public void DiscardOffer(string room)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            CgsBrowserLanDiscardOffer(room ?? string.Empty);
#endif
        }

        // WebRTC is available only in Web players; elsewhere each request completes at once with null.
        private static void Signal(Action<string> completed, Action<int> request)
        {
            var requestId = ++_nextSignal;
            PendingSignals[requestId] = completed;
            if (request == null)
            {
                CompleteSignal(requestId, IntPtr.Zero);
                return;
            }

            try
            {
                request(requestId);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[CgsNet] Browser LAN signaling failed: " + exception.Message);
                CompleteSignal(requestId, IntPtr.Zero);
            }
        }

        [AOT.MonoPInvokeCallback(typeof(Action<int, IntPtr>))]
        private static void CompleteSignal(int requestId, IntPtr value)
        {
            if (!PendingSignals.Remove(requestId, out var completed))
                return;
            completed?.Invoke(value == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(value));
        }
    }
}
