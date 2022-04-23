using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;

namespace LightReflectiveMirror
{
    public partial class LightReflectiveMirrorTransport : Transport
    {
        public override bool ServerActive() => _isServer;
        public override bool Available() => _connectedToRelay;
        public override void ClientConnect(Uri uri) => ClientConnect(uri.Host);
        public override int GetMaxPacketSize(int channelId = 0) => clientToServerTransport.GetMaxPacketSize(channelId);
        public override bool ClientConnected() => _isClient;

        public override void ServerLateUpdate()
        {
            if (_directConnectModule != null)
                _directConnectModule.directConnectTransport.ServerLateUpdate();
        }

        public override string ServerGetClientAddress(int connectionId)
        {
            if (_connectedRelayClients.TryGetBySecond(connectionId, out int relayId))
                return relayId.ToString();

            if (_connectedDirectClients.TryGetBySecond(connectionId, out int directId))
                return "DIRECT-" + directId;

            // Shouldn't ever get here.
            return "?";
        }

        public override void ClientEarlyUpdate()
        {
            clientToServerTransport.ClientEarlyUpdate();

            if (_directConnectModule != null)
                _directConnectModule.directConnectTransport.ClientEarlyUpdate();
        }

        public override void ClientLateUpdate()
        {
            clientToServerTransport.ClientLateUpdate();

            if (_directConnectModule != null)
                _directConnectModule.directConnectTransport.ClientLateUpdate();
        }

        public override void ServerEarlyUpdate()
        {
            if (_directConnectModule != null)
                _directConnectModule.directConnectTransport.ServerEarlyUpdate();
        }

        public override void ClientConnect(string address)
        {
            if (!Available())
            {
                Debug.Log("Not connected to relay!");
                OnClientDisconnected?.Invoke();
                return;
            }

            if (_isClient || _isServer)
                throw new Exception("Cannot connect while hosting/already connected!");

            _cachedHostID = address;

            var room = GetServerForID(address);

            if (!useLoadBalancer)
            {
                int pos = 0;
                _directConnected = false;
                _clientSendBuffer.WriteByte(ref pos, (byte)OpCodes.JoinServer);
                _clientSendBuffer.WriteString(ref pos, address);
                _clientSendBuffer.WriteBool(ref pos, _directConnectModule != null);

                if (_directConnectModule == null)
                {
                    _clientSendBuffer.WriteString(ref pos, "0.0.0.0");
                }
                else
                {
                    if (GetLocalIp() == null)
                        _clientSendBuffer.WriteString(ref pos, "0.0.0.0");
                    else
                        _clientSendBuffer.WriteString(ref pos, GetLocalIp());
                }

                _isClient = true;
#if MIRROR_40_0_OR_NEWER
                clientToServerTransport.ClientSend(new System.ArraySegment<byte>(_clientSendBuffer, 0, pos), 0);
#else
                clientToServerTransport.ClientSend(0, new System.ArraySegment<byte>(_clientSendBuffer, 0, pos));
#endif

            }
            else
            {
                StartCoroutine(JoinOtherRelayAndMatch(room, address));
            }
        }

        public override void ClientDisconnect()
        {
            _isClient = false;

            // make sure we are even connected to a relay
            if (Available())
            {
                int pos = 0;
                _clientSendBuffer.WriteByte(ref pos, (byte)OpCodes.LeaveRoom);
#if MIRROR_40_0_OR_NEWER
                clientToServerTransport.ClientSend(new ArraySegment<byte>(_clientSendBuffer, 0, pos), 0);
#else
                clientToServerTransport.ClientSend(0, new ArraySegment<byte>(_clientSendBuffer, 0, pos));
#endif
            }

            if (_directConnectModule != null)
                _directConnectModule.ClientDisconnect();
        }

#if MIRROR_40_0_OR_NEWER
        public override void ClientSend(ArraySegment<byte> segment, int channelId)
#else
        public override void ClientSend(int channelId, ArraySegment<byte> segment)

#endif
        {
            if (_directConnected)
            {
                _directConnectModule.ClientSend(segment, channelId);
            }
            else
            {
                int pos = 0;
                _clientSendBuffer.WriteByte(ref pos, (byte)OpCodes.SendData);
                _clientSendBuffer.WriteBytes(ref pos, segment.Array.Take(segment.Count).ToArray());
                _clientSendBuffer.WriteInt(ref pos, 0);
#if MIRROR_40_0_OR_NEWER
                clientToServerTransport.ClientSend(new ArraySegment<byte>(_clientSendBuffer, 0, pos), channelId);
#else
                clientToServerTransport.ClientSend(channelId, new ArraySegment<byte>(_clientSendBuffer, 0, pos));
#endif
            }
        }

#if !MIRROR_37_0_OR_NEWER

        public override bool ServerDisconnect(int connectionId)
        {
            if (_connectedRelayClients.TryGetBySecond(connectionId, out int relayId))
            {
                int pos = 0;
                _clientSendBuffer.WriteByte(ref pos, (byte)OpCodes.KickPlayer);
                _clientSendBuffer.WriteInt(ref pos, relayId);
                clientToServerTransport.ClientSend(0, new ArraySegment<byte>(_clientSendBuffer, 0, pos));
                return true;
            }

            if (_connectedDirectClients.TryGetBySecond(connectionId, out int directId))
                return _directConnectModule.KickClient(directId);

            return false;
        }

#else

        public override void ServerDisconnect(int connectionId)
        {
            if (_connectedRelayClients.TryGetBySecond(connectionId, out int relayId))
            {
                int pos = 0;
                _clientSendBuffer.WriteByte(ref pos, (byte)OpCodes.KickPlayer);
                _clientSendBuffer.WriteInt(ref pos, relayId);
                clientToServerTransport.ClientSend(new ArraySegment<byte>(_clientSendBuffer, 0, pos), 0);
                return;
            }

            if (_connectedDirectClients.TryGetBySecond(connectionId, out int directId))
                _directConnectModule.KickClient(directId);
        }

#endif

#if MIRROR_40_0_OR_NEWER
        public override void ServerSend(int connectionId, ArraySegment<byte> segment, int channelId)
#else
        public override void ServerSend(int connectionId, int channelId, ArraySegment<byte> segment)
#endif
        {
            if (_directConnectModule != null && _connectedDirectClients.TryGetBySecond(connectionId, out int directId))
            {
                _directConnectModule.ServerSend(directId, segment, channelId);
            }
            else
            {
                int pos = 0;
                _clientSendBuffer.WriteByte(ref pos, (byte)OpCodes.SendData);
                _clientSendBuffer.WriteBytes(ref pos, segment.Array.Take(segment.Count).ToArray());
                _clientSendBuffer.WriteInt(ref pos, _connectedRelayClients.GetBySecond(connectionId));
#if MIRROR_40_0_OR_NEWER
                clientToServerTransport.ClientSend(new ArraySegment<byte>(_clientSendBuffer, 0, pos), channelId);
#else
                clientToServerTransport.ClientSend(channelId, new ArraySegment<byte>(_clientSendBuffer, 0, pos));
#endif
            }
        }

        public override void ServerStart()
        {
            if (!Available())
            {
                Debug.Log("Not connected to relay! Server failed to start.");
                return;
            }

            if (_isClient || _isServer)
            {
                Debug.Log("Cannot host while already hosting or connected!");
                return;
            }

            _isServer = true;
            _connectedRelayClients = new BiDictionary<int, int>();
            _currentMemberId = 1;
            _connectedDirectClients = new BiDictionary<int, int>();

            var keys = new List<IPEndPoint>(_serverProxies.GetAllKeys());

            for (int i = 0; i < keys.Count; i++)
            {
                _serverProxies.GetByFirst(keys[i]).Dispose();
                _serverProxies.Remove(keys[i]);
            }

            int pos = 0;
            _clientSendBuffer.WriteByte(ref pos, (byte)OpCodes.CreateRoom);
            _clientSendBuffer.WriteInt(ref pos, maxServerPlayers);
            _clientSendBuffer.WriteString(ref pos, serverName);
            _clientSendBuffer.WriteBool(ref pos, isPublicServer);
            _clientSendBuffer.WriteString(ref pos, extraServerData);
            // If we have direct connect module, and our local IP isnt null, tell server. Only time local IP is null is on cellular networks, such as IOS and Android.
            _clientSendBuffer.WriteBool(ref pos, _directConnectModule != null ? GetLocalIp() != null ? true : false : false);

            if (_directConnectModule != null && GetLocalIp() != null)
            {
                _clientSendBuffer.WriteString(ref pos, GetLocalIp());
                // Transport port will be NAT port + 1 for the proxy connections.
                _directConnectModule.StartServer(useNATPunch ? _NATIP.Port + 1 : -1);
            }
            else
                _clientSendBuffer.WriteString(ref pos, "0.0.0.0");

            if (useNATPunch)
            {
                _clientSendBuffer.WriteBool(ref pos, true);
                _clientSendBuffer.WriteInt(ref pos, 0);
            }
            else
            {
                _clientSendBuffer.WriteBool(ref pos, false);
                _clientSendBuffer.WriteInt(ref pos, _directConnectModule == null ? 1 : _directConnectModule.SupportsNATPunch() ? _directConnectModule.GetTransportPort() : 1);
            }
#if MIRROR_40_0_OR_NEWER
            clientToServerTransport.ClientSend(new ArraySegment<byte>(_clientSendBuffer, 0, pos), 0);
#else
            clientToServerTransport.ClientSend(0, new ArraySegment<byte>(_clientSendBuffer, 0, pos));
#endif
        }

        public override void ServerStop()
        {
            if (_isServer)
            {
                _isServer = false;
                int pos = 0;
                _clientSendBuffer.WriteByte(ref pos, (byte)OpCodes.LeaveRoom);

#if MIRROR_40_0_OR_NEWER
                clientToServerTransport.ClientSend(new ArraySegment<byte>(_clientSendBuffer, 0, pos), 0);
#else
                clientToServerTransport.ClientSend(0, new ArraySegment<byte>(_clientSendBuffer, 0, pos));
#endif

                if (_directConnectModule != null)
                    _directConnectModule.StopServer();

                var keys = new List<IPEndPoint>(_serverProxies.GetAllKeys());

                for (int i = 0; i < keys.Count; i++)
                {
                    _serverProxies.GetByFirst(keys[i]).Dispose();
                    _serverProxies.Remove(keys[i]);
                }
            }
        }

        public override Uri ServerUri()
        {
            UriBuilder builder = new UriBuilder
            {
                Scheme = "LRM",
                Host = serverId.ToString()
            };

            return builder.Uri;
        }

        public override void Shutdown()
        {
            _isAuthenticated = false;
            _isClient = false;
            _isServer = false;
            _connectedToRelay = false;
            clientToServerTransport.Shutdown();
        }
    }
}