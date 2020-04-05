/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using UnityEngine;

namespace CGS.Play.Multiplayer
{
    public delegate void OnServerListedDelegate(ServerStatus response);

    public class ServerStatus
    {
        public string ip;
        public string gameName;

        public ServerStatus(string ip, string gameName)
        {
            this.ip = ip;
            this.gameName = gameName;
        }

        public override string ToString()
        {
            return $"{gameName}\n{ip}";
        }
    }

    public class CGSNetListServer : MonoBehaviour
    {
        public const string ListServerIp = "35.232.64.143";
        public const ushort GameServerToListenPort = 8887;
        public const ushort ClientToListenPort = 8888;

        public OnServerListedDelegate OnServerFound;

        private Telepathy.Client _clientToListenConnection = new Telepathy.Client();
        private Telepathy.Client _gameServerToListenConnection = new Telepathy.Client();

        public void StartClient()
        {
            Stop();
            StartCoroutine(TickClient());
        }

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
            ushort gameNameLength = reader.ReadUInt16();
            string gameName = Encoding.UTF8.GetString(reader.ReadBytes(gameNameLength));
            return new ServerStatus(ip, gameName);
        }

        public void StartGameServer()
        {
            Stop();
            StartCoroutine(TickGameServer());
        }

        IEnumerator TickGameServer()
        {
            while (true)
            {
                if (_gameServerToListenConnection.Connected)
                    SendStatus();
                else if (!_gameServerToListenConnection.Connecting)
                {
                    Debug.Log("[List Server] GameServer connecting......");
                    _gameServerToListenConnection.Connect(ListServerIp, GameServerToListenPort);
                }
                yield return new WaitForSeconds(1.0f);
            }
        }

        void SendStatus()
        {
            BinaryWriter writer = new BinaryWriter(new MemoryStream());

            byte[] titleBytes = Encoding.UTF8.GetBytes(CardGameManager.Current.Name);
            writer.Write((ushort)titleBytes.Length);
            writer.Write(titleBytes);
            writer.Flush();

            // list server only allows up to 128 bytes per message
            if (writer.BaseStream.Position <= 128)
            {
//                Debug.Log("[List Server] GameServer sending status......");
                _gameServerToListenConnection.Send(((MemoryStream)writer.BaseStream).ToArray());
            }
            else
                Debug.LogError("[List Server] List Server will reject messages longer than 128 bytes. Please use a shorter title.");
        }

        public void Stop()
        {
            if (_clientToListenConnection.Connected)
                _clientToListenConnection.Disconnect();
            if (_gameServerToListenConnection.Connected)
                _gameServerToListenConnection.Disconnect();
            StopAllCoroutines();
        }

    }
}
