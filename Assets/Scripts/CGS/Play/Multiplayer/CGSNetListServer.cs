/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using UnityEngine;
using Mirror;

namespace CGS.Play.Multiplayer
{
    public delegate void OnServerListedDelegate(ServerStatus response);

    public class ServerStatus
    {
        public string ip;
        public string gameName;
        public ushort players;
        public ushort capacity;

        public ServerStatus(string ip, string gameName, ushort players, ushort capacity)
        {
            this.ip = ip;
            this.gameName = gameName;
            this.players = players;
            this.capacity = capacity;
        }

        public override string ToString()
        {
            return $"{gameName}\n{ip} - {players}/{capacity}";
        }
    }

    public class CGSNetListServer : MonoBehaviour
    {
        public const string ListServerIp = "35.232.64.143";
        public const ushort GameServerToListenPort = 8887;
        public const ushort ClientToListenPort = 8888;

        public OnServerListedDelegate OnServerFound;

        private Telepathy.Client _gameServerToListenConnection = new Telepathy.Client();
        private Telepathy.Client _clientToListenConnection = new Telepathy.Client();

        public void StartGameServer()
        {
            Stop();
            Debug.Log("[List Server] Starting game server...");
//            InvokeRepeating(nameof(TickGameServer), 0, 1);
            StartCoroutine(TickGameServer());
        }

//        void TickGameServer()
        IEnumerator TickGameServer()
        {
            while (true)
            {
                if (_gameServerToListenConnection.Connected)
                {
//                    Debug.Log("[List Server] GameServer connected...");
                    SendStatus();
                }
                else if (!_gameServerToListenConnection.Connecting)
                {
                    Debug.Log("[List Server] GameServer connecting...");
                    _gameServerToListenConnection.Connect(ListServerIp, GameServerToListenPort);
                }
                else
                    Debug.LogError("[List Server] GameServer is ticking but not connecting.");
                yield return new WaitForSeconds(1.0f);
            }
        }

        void SendStatus()
        {
            BinaryWriter writer = new BinaryWriter(new MemoryStream());

            // create message
            writer.Write((ushort)NetworkServer.connections.Count);
            writer.Write((ushort)NetworkManager.singleton.maxConnections);
            byte[] gameNameBytes = Encoding.UTF8.GetBytes(CardGameManager.Current.Name);
            writer.Write((ushort)gameNameBytes.Length);
            writer.Write(gameNameBytes);
            writer.Flush();

            // list server only allows up to 128 bytes per message
            if (writer.BaseStream.Position <= 128)
            {
//                Debug.Log("[List Server] GameServer sending status......");
                if(!_gameServerToListenConnection.Send(((MemoryStream)writer.BaseStream).ToArray()))
                    Debug.LogError("[List Server] GameServer failed to send status!");
            }
            else
                Debug.LogError("[List Server] List Server will reject messages longer than 128 bytes. Game Name is too long.");
        }

        public void StartClient()
        {
            Stop();
            Debug.Log("[List Server] Starting client...");
//            InvokeRepeating(nameof(TickClient), 0, 1);
            StartCoroutine(TickClient());
        }

//        void TickClient()
        IEnumerator TickClient()
        {
            while (true)
            {
                if (_clientToListenConnection.Connected)
                {
                    // receive latest game server info
                    while (_clientToListenConnection.GetNextMessage(out Telepathy.Message message))
                    {
                        // connected?
                        if (message.eventType == Telepathy.EventType.Connected)
                            Debug.Log("[List Server] Client connected!");
                        // data message?
                        else if (message.eventType == Telepathy.EventType.Data)
                            OnServerFound(ParseMessage(message.data));
                        // disconnected?
                        else if (message.eventType == Telepathy.EventType.Disconnected)
                            Debug.Log("[List Server] Client disconnected.");
                    }
                }
                else if (!_clientToListenConnection.Connecting)
                {
                    Debug.Log("[List Server] Client connecting...");
                    _clientToListenConnection.Connect(ListServerIp, ClientToListenPort);
                }
                else
                    Debug.LogError("[List Server] Client is ticking but not connecting.");
                yield return new WaitForSeconds(1.0f);
            }
        }

        ServerStatus ParseMessage(byte[] bytes)
        {
            // note: we don't use ReadString here because the list server
            //       doesn't know C#'s '7-bit-length + utf8' encoding for strings
            BinaryReader reader = new BinaryReader(new MemoryStream(bytes, false), Encoding.UTF8);
            byte ipBytesLength = reader.ReadByte();
            byte[] ipBytes = reader.ReadBytes(ipBytesLength);
            string ip = new IPAddress(ipBytes).ToString();
            ushort players = reader.ReadUInt16();
            ushort capacity = reader.ReadUInt16();
            ushort gameNameLength = reader.ReadUInt16();
            string gameName = Encoding.UTF8.GetString(reader.ReadBytes(gameNameLength));
            return new ServerStatus(ip, gameName, players, capacity);
        }

        public void Stop()
        {
            Debug.Log("[List Server] Stopping...");
            if (_clientToListenConnection.Connected)
                _clientToListenConnection.Disconnect();
            if (_gameServerToListenConnection.Connected)
                _gameServerToListenConnection.Disconnect();
            StopAllCoroutines();
        }

        void OnApplicationQuit()
        {
            Stop();
        }

    }
}
