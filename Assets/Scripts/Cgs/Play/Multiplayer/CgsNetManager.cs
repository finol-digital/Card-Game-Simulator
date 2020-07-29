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

        private Guid _cardAssetId;
        private Guid _dieAssetId;

        public override void Start()
        {
            base.Start();

            _cardAssetId = playController.cardModelPrefab.GetComponent<NetworkIdentity>().assetId;
            ClientScene.RegisterSpawnHandler(_cardAssetId, SpawnCard, UnSpawnCard);
            Debug.Log("[CgsNet Manager] Registered card spawn handler.");

            _dieAssetId = playController.diePrefab.GetComponent<NetworkIdentity>().assetId;
            ClientScene.RegisterSpawnHandler(_dieAssetId, SpawnDie, UnSpawnDie);
            Debug.Log("[CgsNet Manager] Registered die spawn handler.");

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

        private GameObject SpawnCard(Vector3 position, Guid assetId)
        {
            Debug.Log("[CgsNet Manager] SpawnCard");
            GameObject newCard =
                Instantiate(playController.cardModelPrefab, playController.playAreaCardStack.transform);
            PlayMode.SetPlayActions(newCard.GetComponent<CardModel>());
            return newCard;
        }

        private static void UnSpawnCard(GameObject spawned)
        {
            Debug.Log("[CgsNet Manager] UnSpawnCard");
            Destroy(spawned);
        }

        private GameObject SpawnDie(Vector3 position, Guid assetId)
        {
            Debug.Log("[CgsNet Manager] SpawnDie");
            Transform target = playController.playAreaCardStack.transform;
            var die = Instantiate(playController.diePrefab, target.parent).GetOrAddComponent<Die>();
            die.transform.SetParent(target);
            return die.gameObject;
        }

        private static void UnSpawnDie(GameObject spawned)
        {
            Debug.Log("[CgsNet Manager] UnSpawnDie");
            Destroy(spawned);
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

        public void Restart()
        {
            // Clear all decks when restarting
            for (var i = 0; i < Data.cardStacks.Count; i++)
                Data.ChangeDeck(i, new string[] { });

            foreach (CardModel cardModel in playController.playAreaCardStack.GetComponentsInChildren<CardModel>())
                NetworkServer.UnSpawn(cardModel.gameObject);
            foreach (CgsNetPlayer player in FindObjectsOfType<CgsNetPlayer>())
                player.TargetRestart(player.connectionToClient);
        }
    }
}
