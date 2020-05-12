/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Net.Sockets;
using CardGameView;
using Mirror;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using ClientScene = Mirror.ClientScene;
using NetworkConnection = Mirror.NetworkConnection;
using NetworkIdentity = Mirror.NetworkIdentity;
using NetworkManager = Mirror.NetworkManager;
using NetworkServer = Mirror.NetworkServer;

namespace Cgs.Play.Multiplayer
{
    [RequireComponent(typeof(CgsNetDiscovery))]
    [RequireComponent(typeof(CgsNetListServer))]
    public class CgsNetManager : NetworkManager
    {
        public static string PortForwardingWarningMessage =>
            $"Unable to verify internet connection. Other players may not be able to find this game session. You may need to forward port {((TelepathyTransport) Transport.activeTransport).port}. More info on port forwarding is available at: https://www.howtogeek.com/66214/how-to-forward-ports-on-your-router/";

        public static CgsNetManager Instance => (CgsNetManager) singleton;

        public string GameName { get; set; } = "";

        public CgsNetPlayer LocalPlayer { get; set; }
        public CgsNetData Data { get; set; }

        public CgsNetDiscovery Discovery { get; private set; }
        public CgsNetListServer ListServer { get; private set; }

        public PlayMode playController;
        public Text statusText;

        public override void Start()
        {
            base.Start();
            Discovery = GetComponent<CgsNetDiscovery>();
            ListServer = GetComponent<CgsNetListServer>();
            Debug.Log("[CgsNet Manager] Acquired Discovery and List Server.");
        }

        public override void OnServerAddPlayer(NetworkConnection conn)
        {
            base.OnServerAddPlayer(conn);
            Debug.Log("[CgsNet Manager] Server adding player...");
            statusText.text = $"Player {NetworkServer.connections.Count} has joined!";
            if (Data == null)
            {
                Data = Instantiate(spawnPrefabs[0]).GetOrAddComponent<CgsNetData>();
                NetworkServer.Spawn(Data.gameObject);
            }

            Data.RegisterScore(conn.identity.gameObject, CardGameManager.Current.GameStartPointsCount);
            Debug.Log("[CgsNet Manager] Server added player!");
        }

        public override void OnClientConnect(NetworkConnection connection)
        {
            base.OnClientConnect(connection);
            Debug.Log("[CgsNet Manager] Client connecting...");
            statusText.text = "Connected!";
            Debug.Log("[CgsNet Manager] Client connected!");
        }

        public void CheckForPortForwarding()
        {
            StartCoroutine(CheckIsPortForwarded());
        }

        private static IEnumerator CheckIsPortForwarded()
        {
            string ip;
            using (UnityWebRequest www = UnityWebRequest.Get("https://api.ipify.org"))
            {
                yield return www.SendWebRequest();
                if (www.isNetworkError)
                {
                    ShowPortForwardingWarningMessage();
                    yield break;
                }

                ip = www.downloadHandler.text;
            }

            try
            {
                using (var tcpClient = new TcpClient())
                {
                    IAsyncResult result = tcpClient.BeginConnect(ip,
                        ((TelepathyTransport) Transport.activeTransport).port, null, null);
                    bool success = result.AsyncWaitHandle.WaitOne(100);
                    tcpClient.EndConnect(result);
                    if (!success)
                        ShowPortForwardingWarningMessage();
                }
            }
            catch
            {
                ShowPortForwardingWarningMessage();
            }
        }

        private static void ShowPortForwardingWarningMessage()
        {
            Debug.LogWarning(PortForwardingWarningMessage);
            CardGameManager.Instance.Messenger.Show(PortForwardingWarningMessage);
        }
    }
}
