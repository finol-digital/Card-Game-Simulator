/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Cgs.CardGameView.Multiplayer;
using LightReflectiveMirror;
using Mirror;
using UnityEngine;
using UnityExtensionMethods;

namespace Cgs.Play.Multiplayer
{
    [RequireComponent(typeof(CgsNetDiscovery))]
    public class CgsNetManager : NetworkManager
    {
        public static CgsNetManager Instance => (CgsNetManager) singleton;

        public string RoomName
        {
            get => lrm.serverName ?? CardGameManager.Current.Name;
            set => lrm.serverName = value;
        }

        public string RoomIdIp => "localhost".Equals(networkAddress, StringComparison.Ordinal)
            ? RoomId
            : networkAddress;

        private string RoomId => string.IsNullOrEmpty(lrm.serverId)
            ? Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(f => f.AddressFamily == AddressFamily.InterNetwork)
                .ToString()
            : lrm.serverId;

        public static int ActiveConnectionCount => NetworkServer.connections.Count(con => con.Value.isReady);

        public CgsNetPlayer LocalPlayer { get; set; }

        public CgsNetDiscovery Discovery { get; private set; }

        public LRMDirectConnectModule lanConnector;

        public LightReflectiveMirrorTransport lrm;

        public PlayController playController;

        public override void Start()
        {
            base.Start();

            var cardStackAssetId = playController.cardStackPrefab.GetComponent<NetworkIdentity>().assetId;
            NetworkClient.RegisterSpawnHandler(cardStackAssetId, SpawnStack, UnSpawn);

            var cardAssetId = playController.cardModelPrefab.GetComponent<NetworkIdentity>().assetId;
            NetworkClient.RegisterSpawnHandler(cardAssetId, SpawnCard, UnSpawnCard);

            var dieAssetId = playController.diePrefab.GetComponent<NetworkIdentity>().assetId;
            NetworkClient.RegisterSpawnHandler(dieAssetId, SpawnDie, UnSpawn);

            Discovery = GetComponent<CgsNetDiscovery>();
            Debug.Log("[CgsNet Manager] Acquired NetworkDiscovery.");
        }

        public override void OnServerAddPlayer(NetworkConnection conn)
        {
            base.OnServerAddPlayer(conn);
            Debug.Log("[CgsNet Manager] Server adding player...");
        }

        public override void OnClientConnect(NetworkConnection conn)
        {
            base.OnClientConnect(conn);
            Debug.Log("[CgsNet Manager] Client connected!");
        }

        private GameObject SpawnStack(Vector3 position, Guid assetId)
        {
            Debug.Log("[CgsNet Manager] SpawnStack");
            var target = playController.playMat.transform;
            var cardStack = Instantiate(playController.cardStackPrefab, target.parent).GetComponent<CardStack>();
            cardStack.transform.SetParent(target);
            return cardStack.gameObject;
        }

        private GameObject SpawnCard(Vector3 position, Guid assetId)
        {
            Debug.Log("[CgsNet Manager] SpawnCard");
            var newCardGameObject = Instantiate(playController.cardModelPrefab, playController.playMat.transform);
            PlayController.SetPlayActions(newCardGameObject.GetComponent<CardModel>());
            return newCardGameObject;
        }

        private GameObject SpawnDie(Vector3 position, Guid assetId)
        {
            Debug.Log("[CgsNet Manager] SpawnDie");
            var target = playController.playMat.transform;
            var die = Instantiate(playController.diePrefab, target.parent).GetOrAddComponent<Die>();
            die.transform.SetParent(target);
            return die.gameObject;
        }

        private static void UnSpawn(GameObject spawned)
        {
            Debug.Log("[CgsNet Manager] UnSpawn");
            Destroy(spawned);
        }

        private static void UnSpawnCard(GameObject spawnedCard)
        {
            Debug.Log("[CgsNet Manager] UnSpawnCard");
            var cardModel = spawnedCard.GetComponent<CardModel>();
            if (!cardModel.PointerDragOffsets.Any())
                Destroy(spawnedCard);
            else
                Debug.Log("[CgsNet Manager] Ignore UnSpawn - Active Card");
        }

        public void Restart()
        {
            foreach (var cardStack in playController.playMat.GetComponentsInChildren<CardStack>())
                NetworkServer.UnSpawn(cardStack.gameObject);
            foreach (var cardModel in playController.playMat.GetComponentsInChildren<CardModel>())
                NetworkServer.UnSpawn(cardModel.gameObject);
            foreach (var die in playController.playMat.GetComponentsInChildren<Die>())
                NetworkServer.UnSpawn(die.gameObject);
            foreach (var player in FindObjectsOfType<CgsNetPlayer>())
                player.TargetRestart();
        }

        private void OnDisable()
        {
            NetworkClient.ClearSpawners();
        }
    }
}
