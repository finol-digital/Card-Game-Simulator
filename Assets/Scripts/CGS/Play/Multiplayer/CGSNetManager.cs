/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;
using UnityEngine.UI;
using Mirror;
using CardGameView;

namespace CGS.Play.Multiplayer
{
    [RequireComponent(typeof(CgsNetDiscovery))]
    [RequireComponent(typeof(CgsNetListServer))]
    public class CgsNetManager : NetworkManager
    {
        public static CgsNetManager Instance => (CgsNetManager) singleton;
        public CgsNetPlayer LocalPlayer { get; set; }
        public CGSNetData Data { get; set; }

        // ReSharper disable once InconsistentNaming
        public CgsNetDiscovery Discovery;

        // ReSharper disable once InconsistentNaming
        public CgsNetListServer ListServer;

        public GameObject cardModelPrefab;
        public PlayMode playController;
        public Text statusText;

        public override void OnServerAddPlayer(NetworkConnection conn)
        {
            base.OnServerAddPlayer(conn);
            Debug.Log("[CgsNet Manager] Server adding player...");
            statusText.text = $"Player {NetworkServer.connections.Count} has joined!";
            if (Data == null)
            {
                Data = Instantiate(spawnPrefabs[0]).GetOrAddComponent<CGSNetData>();
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
            ClientScene.RegisterSpawnHandler(cardModelPrefab.GetComponent<NetworkIdentity>().assetId, SpawnCard,
                UnSpawnCard);
            Debug.Log("[CgsNet Manager] Client connected!");
        }

        public GameObject SpawnCard(Vector3 position, System.Guid assetId)
        {
            GameObject newCard = Instantiate(cardModelPrefab, playController.playAreaContent);
            playController.SetPlayActions(playController.playAreaContent.GetComponent<CardStack>(),
                newCard.GetComponent<CardModel>());
            return newCard;
        }

        public void UnSpawnCard(GameObject spawned)
        {
            Destroy(spawned);
        }
    }
}
