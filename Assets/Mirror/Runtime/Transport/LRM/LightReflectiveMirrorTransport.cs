using Mirror;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace LightReflectiveMirror
{
    [DefaultExecutionOrder(1001)]
    public class LightReflectiveMirrorTransport : Transport
    {
        [Header("Connection Variables")]
        public Transport clientToServerTransport;
        public string serverIP = "34.67.125.123";
        public ushort endpointServerPort = 8080;
        public float heartBeatInterval = 3;
        public bool connectOnAwake = true;
        public string authenticationKey = "Secret Auth Key";
        public UnityEvent diconnectedFromRelay;
        [Header("NAT Punchthrough")]
        [Help("NAT Punchthrough will require the Direct Connect module attached.")]
        public bool useNATPunch = true;
        public ushort NATPunchtroughPort = 7776;
        [Header("Server Hosting Data")]
        public string serverName = "My awesome server!";
        public string extraServerData = "Map 1";
        public int maxServerPlayers = 10;
        public bool isPublicServer = true;
        [Header("Server List")]
        public UnityEvent serverListUpdated;
        public List<RelayServerInfo> relayServerList { private set; get; } = new List<RelayServerInfo>();
        [Header("Server Information")]
        public int serverId = -1;

        private LRMDirectConnectModule _directConnectModule;

        private byte[] _clientSendBuffer;
        private bool _connectedToRelay = false;
        private bool _isClient = false;
        private bool _isServer = false;
        private bool _directConnected = false;
        private bool _isAuthenticated = false;
        private int _currentMemberId;
        private bool _callbacksInitialized = false;
        private int _cachedHostID;
        private UdpClient _NATPuncher;
        private IPEndPoint _NATIP;
        private IPEndPoint _relayPuncherIP;
        private byte[] _punchData = new byte[1] { 1 };
        private IPEndPoint _directConnectEndpoint;
        private SocketProxy _clientProxy;
        private BiDictionary<IPEndPoint, SocketProxy> _serverProxies = new BiDictionary<IPEndPoint, SocketProxy>();
        private BiDictionary<int, int> _connectedRelayClients = new BiDictionary<int, int>();
        private BiDictionary<int, int> _connectedDirectClients = new BiDictionary<int, int>();

        public override bool ClientConnected() => _isClient;
        private void OnConnectedToRelay() => _connectedToRelay = true;
        public bool IsAuthenticated() => _isAuthenticated;
        public override bool ServerActive() => _isServer;
        public override bool Available() => _connectedToRelay;
        public override void ClientConnect(Uri uri) => ClientConnect(uri.Host);
        public override int GetMaxPacketSize(int channelId = 0) => clientToServerTransport.GetMaxPacketSize(channelId);

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

        void RecvData(IAsyncResult result)
        {
            IPEndPoint newClientEP = new IPEndPoint(IPAddress.Any, 0);
            var data = _NATPuncher.EndReceive(result, ref newClientEP);

            if (!newClientEP.Address.Equals(_relayPuncherIP.Address))
            {
                if (_isServer)
                {
                    if(_serverProxies.TryGetByFirst(newClientEP, out SocketProxy foundProxy))
                    {
                        if (data.Length > 2)
                            foundProxy.RelayData(data, data.Length);
                    }
                    else
                    {
                        _serverProxies.Add(newClientEP, new SocketProxy(_NATIP.Port + 1, newClientEP));
                        _serverProxies.GetByFirst(newClientEP).dataReceived += ServerProcessProxyData;
                    }
                }

                if (_isClient)
                {
                    if(_clientProxy == null)
                    {
                        _clientProxy = new SocketProxy(_NATIP.Port - 1);
                        _clientProxy.dataReceived += ClientProcessProxyData;
                    }
                    else
                    {
                        _clientProxy.ClientRelayData(data, data.Length);
                    }
                }
            }

            _NATPuncher.BeginReceive(new AsyncCallback(RecvData), _NATPuncher);
        }

        void ServerProcessProxyData(IPEndPoint remoteEndpoint, byte[] data)
        {
            _NATPuncher.Send(data, data.Length, remoteEndpoint);
        }

        void ClientProcessProxyData(IPEndPoint _, byte[] data)
        {
            _NATPuncher.Send(data, data.Length, _directConnectEndpoint);
        }

        public override void ServerLateUpdate()
        {
            if (_directConnectModule != null)
                _directConnectModule.directConnectTransport.ServerLateUpdate();
        }

        private void Awake()
        {
            if (clientToServerTransport is LightReflectiveMirrorTransport)
                throw new Exception("Haha real funny... Use a different transport.");

            _directConnectModule = GetComponent<LRMDirectConnectModule>();

            if(_directConnectModule != null)
            {
                if (useNATPunch && !_directConnectModule.SupportsNATPunch())
                {
                    Debug.LogWarning("LRM | NATPunch is turned on but the transport used does not support it. It will be disabled.");
                    useNATPunch = false;
                }
            }

            SetupCallbacks();

            if (connectOnAwake)
                ConnectToRelay();

            InvokeRepeating(nameof(SendHeartbeat), heartBeatInterval, heartBeatInterval);
        }

        private void SetupCallbacks()
        {
            if (_callbacksInitialized)
                return;

            _callbacksInitialized = true;
            clientToServerTransport.OnClientConnected = OnConnectedToRelay;
            clientToServerTransport.OnClientDataReceived = DataReceived;
            clientToServerTransport.OnClientDisconnected = Disconnected;
        }

        void Disconnected()
        {
            _connectedToRelay = false;
            _isAuthenticated = false;
            diconnectedFromRelay?.Invoke();
        }

        public void ConnectToRelay()
        {
            if (!_connectedToRelay)
            {
                _clientSendBuffer = new byte[clientToServerTransport.GetMaxPacketSize()];

                string relayIP = serverIP;
                if (!IPAddress.TryParse(relayIP, out _))
                    relayIP = Dns.GetHostEntry(serverIP).AddressList[0].ToString();

                clientToServerTransport.ClientConnect(relayIP);
            }
            else
            {
                Debug.Log("Already connected to relay!");
            }
        }

        void SendHeartbeat()
        {
            if (_connectedToRelay)
            {
                int pos = 0;
                _clientSendBuffer.WriteByte(ref pos, 200);
                clientToServerTransport.ClientSend(0, new ArraySegment<byte>(_clientSendBuffer, 0, pos));

                if (_NATPuncher != null)
                    _NATPuncher.Send(new byte[] { 0 }, 1, _relayPuncherIP);

                var keys = new List<IPEndPoint>(_serverProxies.GetAllKeys());

                for (int i = 0; i < keys.Count; i++)
                {
                    if (DateTime.Now.Subtract(_serverProxies.GetByFirst(keys[i]).lastInteractionTime).TotalSeconds > 10)
                    {
                        _serverProxies.GetByFirst(keys[i]).Dispose();
                        _serverProxies.Remove(keys[i]);
                    }
                }
            }
        }

        public void RequestServerList()
        {
            if (_isAuthenticated && _connectedToRelay)
                StartCoroutine(GetServerList());
            else
                Debug.Log("You must be connected to Relay to request server list!");
        }

        IEnumerator NATPunch(IPEndPoint remoteAddress)
        {
            for (int i = 0; i < 10; i++)
            {
                _NATPuncher.Send(_punchData, 1, remoteAddress);
                yield return new WaitForSeconds(0.25f);
            }
        }

        void DataReceived(ArraySegment<byte> segmentData, int channel)
        {
            try
            {
                var data = segmentData.Array;
                int pos = segmentData.Offset;

                OpCodes opcode = (OpCodes)data.ReadByte(ref pos);

                switch (opcode)
                {
                    case OpCodes.Authenticated:
                        _isAuthenticated = true;
                        break;
                    case OpCodes.AuthenticationRequest:
                        SendAuthKey();
                        break;
                    case OpCodes.GetData:
                        var recvData = data.ReadBytes(ref pos);

                        if (_isServer)
                        {
                            if(_connectedRelayClients.TryGetByFirst(data.ReadInt(ref pos), out int clientID))
                                OnServerDataReceived?.Invoke(clientID, new ArraySegment<byte>(recvData), channel);
                        }

                        if (_isClient)
                            OnClientDataReceived?.Invoke(new ArraySegment<byte>(recvData), channel);
                        break;
                    case OpCodes.ServerLeft:
                        if (_isClient)
                        {
                            _isClient = false;
                            OnClientDisconnected?.Invoke();
                        }
                        break;
                    case OpCodes.PlayerDisconnected:
                        if (_isServer)
                        {
                            int user = data.ReadInt(ref pos);
                            if (_connectedRelayClients.TryGetByFirst(user, out int clientID))
                            {
                                OnServerDisconnected?.Invoke(clientID);
                                _connectedRelayClients.Remove(user);
                            }
                        }
                        break;
                    case OpCodes.RoomCreated:
                        serverId = data.ReadInt(ref pos);
                        break;
                    case OpCodes.ServerJoined:
                        int clientId = data.ReadInt(ref pos);
                        if (_isClient)
                        {
                            OnClientConnected?.Invoke();
                        }
                        if (_isServer)
                        {
                            _connectedRelayClients.Add(clientId, _currentMemberId);
                            OnServerConnected?.Invoke(_currentMemberId);
                            _currentMemberId++;
                        }
                        break;
                    case OpCodes.DirectConnectIP:
                        var ip = data.ReadString(ref pos);
                        int port = data.ReadInt(ref pos);
                        bool attemptNatPunch = data.ReadBool(ref pos);

                        _directConnectEndpoint = new IPEndPoint(IPAddress.Parse(ip), port);

                        if (useNATPunch && attemptNatPunch)
                        {
                            StartCoroutine(NATPunch(_directConnectEndpoint));
                        }

                        if (!_isServer)
                        {
                            if (_clientProxy == null && useNATPunch && attemptNatPunch)
                            {
                                _clientProxy = new SocketProxy(_NATIP.Port - 1);
                                _clientProxy.dataReceived += ClientProcessProxyData;
                            }

                            if (useNATPunch && attemptNatPunch)
                                _directConnectModule.JoinServer("127.0.0.1", _NATIP.Port - 1);
                            else
                                _directConnectModule.JoinServer(ip, port);
                        }

                        break;
                    case OpCodes.RequestNATConnection:
                        if (GetLocalIp() != null && _directConnectModule != null)
                        {
                            _NATPuncher = new UdpClient { ExclusiveAddressUse = false };
                            _NATIP = new IPEndPoint(IPAddress.Parse(GetLocalIp()), UnityEngine.Random.Range(16000, 17000));
                            _NATPuncher.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                            _NATPuncher.Client.Bind(_NATIP);

                            IPAddress relayAddr;

                            if (!IPAddress.TryParse(serverIP, out relayAddr))
                                relayAddr = Dns.GetHostEntry(serverIP).AddressList[0];

                            _relayPuncherIP = new IPEndPoint(relayAddr, NATPunchtroughPort);

                            byte[] initalData = new byte[150];
                            int sendPos = 0;

                            initalData.WriteBool(ref sendPos, true);
                            initalData.WriteString(ref sendPos, data.ReadString(ref pos));

                            // Send 3 to lower chance of it being dropped or corrupted when received on server.
                            _NATPuncher.Send(initalData, sendPos,_relayPuncherIP);
                            _NATPuncher.Send(initalData, sendPos,_relayPuncherIP);
                            _NATPuncher.Send(initalData, sendPos, _relayPuncherIP);
                            _NATPuncher.BeginReceive(new AsyncCallback(RecvData), _NATPuncher);
                        }
                        break;
                }
            }
            catch(Exception e) { print(e); }
        }

        IEnumerator GetServerList()
        {
            string uri = $"http://{serverIP}:{endpointServerPort}/api/compressed/servers";

            using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
            {
                // Request and wait for the desired page.
                yield return webRequest.SendWebRequest();
                var result = webRequest.downloadHandler.text;

#if UNITY_2020_1_OR_NEWER
                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogWarning("LRM | Network Error while retreiving the server list!");
                        break;

                    case UnityWebRequest.Result.Success:
                        relayServerList?.Clear();
                        relayServerList = JsonConvert.DeserializeObject<List<RelayServerInfo>>(result.Decompress());
                        serverListUpdated?.Invoke();
                        break;
                }
#else
                if (webRequest.isNetworkError || webRequest.isHttpError)
                {
                    Debug.LogWarning("LRM | Network Error while retreiving the server list!");
                }
                else
                {
                    relayServerList?.Clear();
                    relayServerList = JsonConvert.DeserializeObject<List<RelayServerInfo>>(result.Decompress());
                    serverListUpdated?.Invoke();
                }
#endif
            }
        }

        public void UpdateRoomInfo(string newServerName = null, string newServerData = null, bool? newServerIsPublic = null, int? newPlayerCap = null)
        {
            if (_isServer)
            {
                int pos = 0;

                _clientSendBuffer.WriteByte(ref pos, (byte)OpCodes.UpdateRoomData);

                if (!string.IsNullOrEmpty(newServerName))
                {
                    _clientSendBuffer.WriteBool(ref pos, true);
                    _clientSendBuffer.WriteString(ref pos, newServerName);
                }
                else
                    _clientSendBuffer.WriteBool(ref pos, false);

                if (!string.IsNullOrEmpty(newServerData))
                {
                    _clientSendBuffer.WriteBool(ref pos, true);
                    _clientSendBuffer.WriteString(ref pos, newServerData);
                }
                else
                    _clientSendBuffer.WriteBool(ref pos, false);

                if (newServerIsPublic != null)
                {
                    _clientSendBuffer.WriteBool(ref pos, true);
                    _clientSendBuffer.WriteBool(ref pos, newServerIsPublic.Value);
                }
                else
                    _clientSendBuffer.WriteBool(ref pos, false);

                if (newPlayerCap != null)
                {
                    _clientSendBuffer.WriteBool(ref pos, true);
                    _clientSendBuffer.WriteInt(ref pos, newPlayerCap.Value);
                }
                else
                    _clientSendBuffer.WriteBool(ref pos, false);

                clientToServerTransport.ClientSend(0, new ArraySegment<byte>(_clientSendBuffer, 0, pos));
            }
        }

        void SendAuthKey()
        {
            int pos = 0;
            _clientSendBuffer.WriteByte(ref pos, (byte)OpCodes.AuthenticationResponse);
            _clientSendBuffer.WriteString(ref pos, authenticationKey);
            clientToServerTransport.ClientSend(0, new ArraySegment<byte>(_clientSendBuffer, 0, pos));
        }

        public override void ClientConnect(string address)
        {
            if (!Available() || !int.TryParse(address, out _cachedHostID))
            {
                Debug.Log("Not connected to relay or invalid server id!");
                OnClientDisconnected?.Invoke();
                return;
            }

            if (_isClient || _isServer)
                throw new Exception("Cannot connect while hosting/already connected!");

            int pos = 0;
            _directConnected = false;
            _clientSendBuffer.WriteByte(ref pos, (byte)OpCodes.JoinServer);
            _clientSendBuffer.WriteInt(ref pos, _cachedHostID);
            _clientSendBuffer.WriteBool(ref pos, _directConnectModule != null);

            if (GetLocalIp() == null)
                _clientSendBuffer.WriteString(ref pos, "0.0.0.0");
            else
                _clientSendBuffer.WriteString(ref pos, GetLocalIp());

            _isClient = true;

            clientToServerTransport.ClientSend(0, new System.ArraySegment<byte>(_clientSendBuffer, 0, pos));
        }

        public override void ClientDisconnect()
        {
            _isClient = false;

            int pos = 0;
            _clientSendBuffer.WriteByte(ref pos, (byte)OpCodes.LeaveRoom);

            clientToServerTransport.ClientSend(0, new ArraySegment<byte>(_clientSendBuffer, 0, pos));

            if (_directConnectModule != null)
                _directConnectModule.ClientDisconnect();
        }

        public override void ClientSend(int channelId, ArraySegment<byte> segment)
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

                clientToServerTransport.ClientSend(channelId, new ArraySegment<byte>(_clientSendBuffer, 0, pos));
            }
        }

        public override bool ServerDisconnect(int connectionId)
        {
            if (_connectedRelayClients.TryGetBySecond(connectionId, out int relayId))
            {
                int pos = 0;
                _clientSendBuffer.WriteByte(ref pos, (byte)OpCodes.KickPlayer);
                _clientSendBuffer.WriteInt(ref pos, relayId);
                return true;
            }

            if(_connectedDirectClients.TryGetBySecond(connectionId, out int directId))
                return _directConnectModule.KickClient(directId);

            return false;
        }

        public override void ServerSend(int connectionId, int channelId, ArraySegment<byte> segment)
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

                clientToServerTransport.ClientSend(channelId, new ArraySegment<byte>(_clientSendBuffer, 0, pos));
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

            for(int i = 0; i < keys.Count; i++)
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

            clientToServerTransport.ClientSend(0, new ArraySegment<byte>(_clientSendBuffer, 0, pos));
        }

        public override void ServerStop()
        {
            if (_isServer)
            {
                _isServer = false;
                int pos = 0;
                _clientSendBuffer.WriteByte(ref pos, (byte)OpCodes.LeaveRoom);

                clientToServerTransport.ClientSend(0, new ArraySegment<byte>(_clientSendBuffer, 0, pos));

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

        public enum OpCodes
        {
            Default = 0, RequestID = 1, JoinServer = 2, SendData = 3, GetID = 4, ServerJoined = 5, GetData = 6, CreateRoom = 7, ServerLeft = 8, PlayerDisconnected = 9, RoomCreated = 10,
            LeaveRoom = 11, KickPlayer = 12, AuthenticationRequest = 13, AuthenticationResponse = 14, Authenticated = 17, UpdateRoomData = 18, ServerConnectionData = 19, RequestNATConnection = 20,
            DirectConnectIP = 21
        }

        private static string GetLocalIp()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }

            return null;
        }

        #region Direct Connect Module
        public void DirectAddClient(int clientID)
        {
            if (!_isServer)
                return;

            _connectedDirectClients.Add(clientID, _currentMemberId);
            OnServerConnected?.Invoke(_currentMemberId);
            _currentMemberId++;
        }

        public void DirectRemoveClient(int clientID)
        {
            if (!_isServer)
                return;

            OnServerDisconnected?.Invoke(_connectedDirectClients.GetByFirst(clientID));
            _connectedDirectClients.Remove(clientID);
        }

        public void DirectReceiveData(ArraySegment<byte> data, int channel, int clientID = -1)
        {
            if (_isServer)
                OnServerDataReceived?.Invoke(_connectedDirectClients.GetByFirst(clientID), data, channel);

            if (_isClient)
                OnClientDataReceived?.Invoke(data, channel);
        }

        public void DirectClientConnected()
        {
            _directConnected = true;
            OnClientConnected?.Invoke();
        }

        public void DirectDisconnected()
        {
            if (_directConnected)
            {
                _isClient = false;
                _directConnected = false;
                OnClientDisconnected?.Invoke();
            }
            else
            {
                int pos = 0;
                _directConnected = false;
                _clientSendBuffer.WriteByte(ref pos, (byte)OpCodes.JoinServer);
                _clientSendBuffer.WriteInt(ref pos, _cachedHostID);
                _clientSendBuffer.WriteBool(ref pos, false); // Direct failed, use relay

                _isClient = true;

                clientToServerTransport.ClientSend(0, new System.ArraySegment<byte>(_clientSendBuffer, 0, pos));
            }

            if (_clientProxy != null)
            {
                _clientProxy.Dispose();
                _clientProxy = null;
            }
        }
        #endregion
    }

    [Serializable]
    public struct RelayServerInfo
    {
        public string serverName;
        public int currentPlayers;
        public int maxPlayers;
        public int serverId;
        public string serverData;

        public override string ToString()
        {
            return $"{serverName}\n{serverId} - {currentPlayers}/{maxPlayers}";
        }
    }
}
