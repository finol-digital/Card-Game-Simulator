/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections;
using System.IO;
using System.Net;
using System.Text;
using Mirror;
using UnityEngine;

namespace Cgs.Play.Multiplayer
{
    public delegate void OnServerListedDelegate(ServerStatus response);

    public class ServerStatus
    {
        public readonly string Ip;

// ReSharper disable MemberCanBePrivate.Global
        public readonly string GameName;
        public readonly ushort Players;
        public readonly ushort Capacity;

        public ServerStatus(string ip, string gameName, ushort players, ushort capacity)
        {
            Ip = ip;
            GameName = gameName;
            Players = players;
            Capacity = capacity;
        }

        public override string ToString()
        {
            return $"{GameName}\n{Ip} - {Players}/{Capacity}";
        }
    }

    public class CgsNetListServer : MonoBehaviour
    {
        private const string ListServerIp = "35.232.64.143";
        private const ushort GameServerToListenPort = 8887;
        private const ushort ClientToListenPort = 8888;

        public OnServerListedDelegate OnServerFound { get; set; }

        // TODO: private readonly Client _gameServerToListenConnection = new Client();
        // TODO: private readonly Client _clientToListenConnection = new Client();

        public void StartGameServer()
        {
            Stop();
            Debug.Log("[List Server] Starting game server...");
            StartCoroutine(TickGameServer());
        }

        private IEnumerator TickGameServer()
        {
            while (true)
            {
                /* // TODO:
                if (_gameServerToListenConnection.Connected)
                {
//                    Debug.Log("[CgsNet List Server] GameServer connected and sending status...");
                    SendStatus();
                }
                else if (!_gameServerToListenConnection.Connecting)
                {
                    Debug.Log("[CgsNet List Server] GameServer connecting...");
                    _gameServerToListenConnection.Connect(ListServerIp, GameServerToListenPort);
                }
                else
                {
                    Debug.LogError("[CgsNet List Server] GameServer is ticking but not connecting.");
                    yield break;
                }
*/
                yield return new WaitForSeconds(1.0f);
            }
        }

        private void SendStatus()
        {
            var writer = new BinaryWriter(new MemoryStream());

            // create message
            writer.Write((ushort) NetworkServer.connections.Count);
            writer.Write((ushort) NetworkManager.singleton.maxConnections);
            byte[] gameNameBytes = Encoding.UTF8.GetBytes(CgsNetManager.Instance.GameName);
            writer.Write((ushort) gameNameBytes.Length);
            writer.Write(gameNameBytes);
            writer.Flush();

            // list server only allows up to 128 bytes per message
            if (writer.BaseStream.Position <= 128)
            {
                // TODO: if (!_gameServerToListenConnection.Send(((MemoryStream) writer.BaseStream).ToArray()))
                // TODO:     Debug.LogError("[CgsNet List Server] GameServer failed to send status!");
            }
            else
                Debug.LogError(
                    "[CgsNet List Server] List Server will reject messages longer than 128 bytes. Game Name is too long.");
        }

        public void StartClient()
        {
            Stop();
            Debug.Log("[CgsNet List Server] Starting client...");
            StartCoroutine(TickClient());
        }

        private IEnumerator TickClient()
        {
            while (true)
            {
                /*// TODO:
                if (_clientToListenConnection.Connected)
                {
                    // receive latest game server info
                    while (_clientToListenConnection.GetNextMessage(out Message message))
                    {
                        // connected?
                        if (message.eventType == EventType.Connected)
                            Debug.Log("[CgsNet List Server] Client connected!");
                        // data message?
                        else if (message.eventType == EventType.Data)
                            OnServerFound(ParseMessage(message.data));
                        // disconnected?
                        else if (message.eventType == EventType.Disconnected)
                            Debug.Log("[CgsNet List Server] Client disconnected.");
                    }
                }
                else if (!_clientToListenConnection.Connecting)
                {
                    Debug.Log("[CgsNet List Server] Client connecting...");
                    _clientToListenConnection.Connect(ListServerIp, ClientToListenPort);
                }
                else
                {
                    Debug.LogError("[CgsNet List Server] Client is ticking but not connecting.");
                    yield break;
                }
*/
                yield return new WaitForSeconds(1.0f);
            }
        }

        private static ServerStatus ParseMessage(byte[] bytes)
        {
            // note: we don't use ReadString here because the list server
            //       doesn't know C#'s '7-bit-length + utf8' encoding for strings
            var reader = new BinaryReader(new MemoryStream(bytes, false), Encoding.UTF8);
            byte ipBytesLength = reader.ReadByte();
            byte[] ipBytes = reader.ReadBytes(ipBytesLength);
            var ip = new IPAddress(ipBytes).ToString();
            ushort players = reader.ReadUInt16();
            ushort capacity = reader.ReadUInt16();
            ushort gameNameLength = reader.ReadUInt16();
            string gameName = Encoding.UTF8.GetString(reader.ReadBytes(gameNameLength));
            return new ServerStatus(ip, gameName, players, capacity);
        }

        public void Stop()
        {
            Debug.Log("[CgsNet List Server] Stopping...");
            // TODO: if (_clientToListenConnection.Connected)
            // TODO:     _clientToListenConnection.Disconnect();
            // TODO: if (_gameServerToListenConnection.Connected)
            // TODO:     _gameServerToListenConnection.Disconnect();
            StopAllCoroutines();
        }

        private void OnApplicationQuit()
        {
            Stop();
        }
    }
}
