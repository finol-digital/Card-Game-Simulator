/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

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
// TODO
        }

        public void StartGameServer()
        {
            Stop();
// TODO
        }

        public void Stop()
        {
            if (_clientToListenConnection.Connected)
                _clientToListenConnection.Disconnect();
            if (_gameServerToListenConnection.Connected)
                _gameServerToListenConnection.Disconnect();
        }

    }
}
