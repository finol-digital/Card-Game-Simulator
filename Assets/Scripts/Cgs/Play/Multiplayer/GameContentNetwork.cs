/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cgs.CardGameView.Multiplayer;
using Cgs.GameContent;
using FinolDigital.Cgs.Json.Unity;
using Newtonsoft.Json;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Cgs.Play.Multiplayer
{
    // Optional named messages: baseline RPCs/network variables never acquire new fields.
    // No session persistence is introduced; all scopes below are live connections.
    public sealed class GameContentNetwork : MonoBehaviour
    {
        private const string Channel = "game-content-labels-v1";
        private const int MaxMessageBytes = 512 * 1024;
        private const float NegotiationSeconds = 5;
        private const float RetrySeconds = 2;
        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            TypeNameHandling = TypeNameHandling.None, MaxDepth = 16,
            NullValueHandling = NullValueHandling.Ignore
        };

        private sealed class Packet
        {
            public int Version;
            public string Type;
            public string Game;
            public string Session;
            public string Connection;
            public string Request;
            public ulong Player;
            public ulong Object;
            public string Incarnation;
            public long Revision;
            public ulong Owner;
            public string Name;
            public string[] Cards;
            public bool Deck;
            public float X, Y, Qx, Qy, Qz, Qw;
            public bool Faceup, AutoStack;
            public GameContentLabel Label;
        }

        private sealed class Peer
        {
            public string Connection = Guid.NewGuid().ToString("N");
            public float Deadline;
            public bool Ready;
            // Retain request outcomes for the entire connection, including despawned results.
            public readonly Dictionary<string, Packet> Results = new(StringComparer.Ordinal);
            public readonly Dictionary<CardStack, string> Probes = new();
        }

        private sealed class Record
        {
            public string Incarnation = Guid.NewGuid().ToString("N");
            public long Revision = 1;
            public ulong Owner;
        }

        private sealed class Observation
        {
            public string Probe = Guid.NewGuid().ToString("N");
            public string Incarnation;
            public long Revision;
            public ulong Owner;
        }

        private CgsNetManager _manager;
        private UnityCardGame _game;
        private long _definition = -1;
        private string _session;
        private string _connection;
        private Packet _offer;
        private float _offerDeadline;
        private bool _ready;
        private bool _registered;
        private float _nextRetry;
        private readonly Dictionary<ulong, Peer> _peers = new();
        private readonly Dictionary<CardStack, Record> _records = new();
        private readonly Dictionary<CardStack, Observation> _observations = new();
        private readonly Dictionary<string, Packet> _pending = new(StringComparer.Ordinal);

        public static GameContentNetwork For(NetworkManager manager) => manager == null ? null :
            manager.GetComponent<GameContentNetwork>();
        public static GameContentNetwork Current => For(CgsNetManager.Instance);

        private void OnEnable()
        {
            _manager = GetComponent<CgsNetManager>();
            if (_manager == null)
                return;
            _manager.OnServerStarted += Started;
            _manager.OnClientStarted += Started;
            _manager.OnServerStopped += Stopped;
            _manager.OnClientStopped += Stopped;
            _manager.OnClientConnectedCallback += Connected;
            _manager.OnClientDisconnectCallback += Disconnected;
            if (_manager.IsListening)
                Started();
        }

        private void OnDisable()
        {
            if (_manager == null)
                return;
            _manager.OnServerStarted -= Started;
            _manager.OnClientStarted -= Started;
            _manager.OnServerStopped -= Stopped;
            _manager.OnClientStopped -= Stopped;
            _manager.OnClientConnectedCallback -= Connected;
            _manager.OnClientDisconnectCallback -= Disconnected;
            Stopped(false);
        }

        private void Started()
        {
            if (_registered || _manager.CustomMessagingManager == null)
                return;
            _manager.CustomMessagingManager.RegisterNamedMessageHandler(Channel, Receive);
            _registered = true;
            ChangeGame();
        }

        private void Stopped(bool wasServer)
        {
            if (_registered)
                _manager.CustomMessagingManager?.UnregisterNamedMessageHandler(Channel);
            _registered = false;
            Clear();
            _game = null;
            _definition = -1;
        }

        private void Clear()
        {
            foreach (var stack in _observations.Keys.ToArray())
                if (stack != null)
                    stack.SetContentLabel(null);
            _peers.Clear();
            _records.Clear();
            _observations.Clear();
            _pending.Clear();
            _offer = null;
            _ready = false;
            _session = null;
            _connection = null;
        }

        private bool Localized => _game?.Localization?.IsUsable == true;

        private void ChangeGame()
        {
            Clear();
            _game = CardGameManager.Current;
            _definition = _game?.DefinitionGeneration ?? -1;
            if (!_manager.IsServer || !Localized)
                return;
            _session = Guid.NewGuid().ToString("N");
            foreach (var client in _manager.ConnectedClientsIds)
                Connected(client);
        }

        private void Connected(ulong client)
        {
            if (!_manager.IsServer || !Localized || client == NetworkManager.ServerClientId)
                return;
            var peer = new Peer { Deadline = Time.realtimeSinceStartup + NegotiationSeconds };
            _peers[client] = peer;
            Send(client, Envelope("offer", peer.Connection));
        }

        private void Disconnected(ulong client)
        {
            if (_manager.IsServer)
                _peers.Remove(client);
            else if (client == _manager.LocalClientId)
                Clear();
        }

        private void Update()
        {
            if (!_registered || !_manager.IsListening)
                return;
            if (_game != CardGameManager.Current || _definition != (_game?.DefinitionGeneration ?? -1))
                ChangeGame();
            if (!Localized)
                return;
            if (!_manager.IsServer && _offer != null && !_ready && Time.realtimeSinceStartup <= _offerDeadline &&
                _offer.Game == _game.Id)
            {
                _session = _offer.Session;
                _connection = _offer.Connection;
                Send(NetworkManager.ServerClientId, Envelope("ack", _connection));
                _offer = null;
            }
            if (!_manager.IsServer && _ready)
            {
                if (Time.realtimeSinceStartup >= _nextRetry)
                {
                    _nextRetry = Time.realtimeSinceStartup + RetrySeconds;
                    foreach (var packet in _pending.Values.ToArray())
                        Send(NetworkManager.ServerClientId, packet);
                }
            }
        }

        private Packet Envelope(string type, string connection) => new()
        {
            Version = 1, Type = type, Game = _game?.Id, Session = _session, Connection = connection
        };

        private bool Send(ulong recipient, Packet packet)
        {
            var json = JsonConvert.SerializeObject(packet, JsonSettings);
            var bytes = Encoding.UTF8.GetBytes(json);
            if (bytes.Length > MaxMessageBytes)
                return false;
            using var writer = new FastBufferWriter(bytes.Length + sizeof(int), Allocator.Temp);
            writer.WriteValueSafe(bytes.Length);
            writer.WriteBytesSafe(bytes);
            _manager.CustomMessagingManager.SendNamedMessage(Channel, recipient, writer,
                NetworkDelivery.ReliableFragmentedSequenced);
            return true;
        }

        private void Receive(ulong sender, FastBufferReader reader)
        {
            try
            {
                // NGO positions this reader after the named-message hash; Length
                // still includes that transport header. Bound only the remaining payload.
                var remaining = reader.Length - reader.Position;
                if (remaining < sizeof(int) || remaining > MaxMessageBytes + sizeof(int))
                    return;
                reader.ReadValueSafe(out int length);
                if (length <= 0 || length > MaxMessageBytes || length != reader.Length - reader.Position)
                    return;
                var bytes = new byte[length];
                reader.ReadBytesSafe(ref bytes, length);
                var packet = JsonConvert.DeserializeObject<Packet>(new UTF8Encoding(false, true).GetString(bytes), JsonSettings);
                if (packet == null || packet.Version != 1)
                    return;
                if (_manager.IsServer)
                    ReceiveClient(sender, packet);
                else if (sender == NetworkManager.ServerClientId)
                    ReceiveHost(packet);
            }
            catch (Exception error)
            {
                Debug.LogWarning("Optional game-content message ignored: " + error.Message);
            }
        }

        private void ReceiveClient(ulong sender, Packet packet)
        {
            if (!Localized || !_peers.TryGetValue(sender, out var peer) || packet.Session != _session ||
                packet.Connection != peer.Connection || packet.Game != _game.Id)
                return;
            if (packet.Type == "ack" && (peer.Ready || Time.realtimeSinceStartup <= peer.Deadline))
            {
                peer.Ready = true;
                Send(sender, Envelope("ready", peer.Connection));
                return;
            }
            if (!peer.Ready)
                return;
            if (packet.Type == "probe" && IsToken(packet.Request) &&
                _manager.SpawnManager.SpawnedObjects.TryGetValue(packet.Object, out var networkObject) &&
                networkObject.TryGetComponent<CardStack>(out var stack))
            {
                peer.Probes[stack] = packet.Request;
                SendState(sender, peer, stack);
            }
            else if (packet.Type == "create")
                Create(sender, peer, packet);
        }

        private void ReceiveHost(Packet packet)
        {
            if (packet.Type == "offer" && IsToken(packet.Session) && IsToken(packet.Connection))
            {
                Clear();
                _offer = packet;
                _offerDeadline = Time.realtimeSinceStartup + NegotiationSeconds;
                return;
            }
            if (!Localized || packet.Game != _game.Id || packet.Session != _session || packet.Connection != _connection)
                return;
            if (packet.Type == "ready")
            {
                _ready = true;
                foreach (var liveObject in _manager.SpawnManager.SpawnedObjects.Values)
                    if (liveObject.TryGetComponent<CardStack>(out var liveStack))
                        Observe(liveStack);
                return;
            }
            if (!_ready)
                return;
            if (packet.Type == "result")
            {
                if (packet.Request != null)
                    _pending.Remove(packet.Request);
                return;
            }
            if (packet.Type != "state" || !_manager.SpawnManager.SpawnedObjects.TryGetValue(packet.Object, out var obj) ||
                !obj.TryGetComponent<CardStack>(out var stack) || !_observations.TryGetValue(stack, out var observation) ||
                packet.Request != observation.Probe || packet.Owner != stack.OwnerClientId ||
                !IsToken(packet.Incarnation) || packet.Revision <= observation.Revision ||
                (observation.Incarnation != null && observation.Incarnation != packet.Incarnation))
                return;
            observation.Incarnation = packet.Incarnation;
            observation.Revision = packet.Revision;
            stack.SetContentLabel(packet.Name == stack.Name && packet.Label?.IsValid(_game, stack.Name) == true
                ? packet.Label : null);
        }

        private static bool IsToken(string value) => Guid.TryParseExact(value, "N", out _);
        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private void Create(ulong sender, Peer peer, Packet packet)
        {
            if (!IsToken(packet.Request))
                return;
            if (peer.Results.TryGetValue(packet.Request, out var previous))
            {
                Send(sender, previous);
                return;
            }
            // Record before calling gameplay code: even an exception cannot turn a retry
            // into a second creation. Never route a rejected/uncertain request to an RPC.
            var result = Envelope("result", peer.Connection);
            result.Request = packet.Request;
            peer.Results[packet.Request] = result;
            try
            {
                if (!_manager.SpawnManager.SpawnedObjects.TryGetValue(packet.Player, out var obj) ||
                    obj.OwnerClientId != sender || !obj.IsPlayerObject ||
                    !obj.TryGetComponent<CgsNetPlayer>(out var player) ||
                    packet.Cards == null || packet.Cards.Any(id => id == null || !_game.Cards.ContainsKey(id)) ||
                    !new[] { packet.X, packet.Y, packet.Qx, packet.Qy, packet.Qz, packet.Qw }.All(IsFinite))
                    return;
                var stack = player.CreateCanonicalStack(packet.Name,
                    packet.Cards.Select(id => (CgsNetString)id).ToArray(), packet.Deck,
                    new Vector2(packet.X, packet.Y), new Quaternion(packet.Qx, packet.Qy, packet.Qz, packet.Qw),
                    packet.Faceup, packet.AutoStack,
                    packet.Label?.IsValid(_game, packet.Name) == true ? packet.Label : null);
                result.Object = stack.NetworkObjectId;
            }
            finally
            {
                Send(sender, result);
            }
        }

        public bool TryCreate(CgsNetPlayer player, string name, CgsNetString[] cards, bool deck,
            Vector2 position, Quaternion rotation, bool faceup, bool autoStack, GameContentLabel label)
        {
            if (!Localized || label?.IsValid(_game, name) != true || !player.IsOwner)
                return false;
            if (_manager.IsServer)
            {
                player.CreateCanonicalStack(name, cards, deck, position, rotation, faceup, autoStack, label);
                return true;
            }
            if (!_ready)
                return false;
            var packet = Envelope("create", _connection);
            packet.Request = Guid.NewGuid().ToString("N");
            packet.Player = player.NetworkObjectId;
            packet.Name = name;
            packet.Cards = cards.Select(id => (string)id).ToArray();
            packet.Deck = deck;
            packet.X = position.x; packet.Y = position.y;
            packet.Qx = rotation.x; packet.Qy = rotation.y; packet.Qz = rotation.z; packet.Qw = rotation.w;
            packet.Faceup = faceup; packet.AutoStack = autoStack; packet.Label = label;
            // Size rejection occurs before sending, so the unchanged legacy path is safe.
            if (!Send(NetworkManager.ServerClientId, packet))
                return false;
            _pending[packet.Request] = packet;
            return true;
        }

        public void Observe(CardStack stack)
        {
            if (!_registered || !Localized || !stack.IsSpawned || _manager.IsServer || !_ready)
                return;
            var observation = new Observation { Owner = stack.OwnerClientId };
            _observations[stack] = observation;
            stack.SetContentLabel(null);
            var packet = Envelope("probe", _connection);
            packet.Object = stack.NetworkObjectId;
            packet.Request = observation.Probe;
            Send(NetworkManager.ServerClientId, packet);
        }

        public void Changed(CardStack stack)
        {
            if (!_registered || !Localized || !stack.IsSpawned)
                return;
            if (!_manager.IsServer)
            {
                Observe(stack);
                return;
            }
            var record = GetRecord(stack);
            record.Owner = stack.OwnerClientId;
            record.Revision++;
            Publish(stack);
        }

        private Record GetRecord(CardStack stack)
        {
            if (!_records.TryGetValue(stack, out var record))
            {
                record = new Record { Owner = stack.OwnerClientId };
                _records[stack] = record;
            }
            return record;
        }

        private void Publish(CardStack stack)
        {
            foreach (var peer in _peers)
                if (peer.Value.Ready && peer.Value.Probes.ContainsKey(stack))
                    SendState(peer.Key, peer.Value, stack);
        }

        private void SendState(ulong client, Peer peer, CardStack stack)
        {
            var record = GetRecord(stack);
            var packet = Envelope("state", peer.Connection);
            packet.Object = stack.NetworkObjectId;
            packet.Request = peer.Probes[stack];
            packet.Incarnation = record.Incarnation;
            packet.Revision = record.Revision;
            packet.Owner = stack.OwnerClientId;
            packet.Name = stack.Name;
            packet.Label = stack.ContentLabel;
            Send(client, packet);
        }

        public void Despawned(CardStack stack)
        {
            _records.Remove(stack);
            _observations.Remove(stack);
            foreach (var peer in _peers.Values)
                peer.Probes.Remove(stack);
            stack.SetContentLabel(null);
        }
    }
}
