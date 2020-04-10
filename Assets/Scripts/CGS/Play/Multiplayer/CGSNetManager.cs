/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;
using Mirror;

using CardGameView;

namespace CGS.Play.Multiplayer
{
    [RequireComponent(typeof(CGSNetDiscovery))]
    [RequireComponent(typeof(CGSNetListServer))]
    public class CGSNetManager : NetworkManager
    {
        public static CGSNetManager Instance => (CGSNetManager)singleton;
        public CGSNetPlayer LocalPlayer { get; set; }
        public CGSNetData Data { get; set; }

        public CGSNetDiscovery Discovery;
        public CGSNetListServer ListServer;

        public GameObject cardModelPrefab;
        public PlayMode playController;

        public override void OnServerAddPlayer(NetworkConnection conn)
        {
            base.OnServerAddPlayer(conn);
            Debug.Log("CGSNetManager OnServerAddPlayer...");
            if (Data == null)
            {
                Data = Instantiate(spawnPrefabs[0]).GetOrAddComponent<CGSNetData>();
                NetworkServer.Spawn(Data.gameObject);
            }
            Data.RegisterScore(conn.identity.gameObject, CardGameManager.Current.GameStartPointsCount);
            Debug.Log("CGSNetManager OnServerAddPlayer!");
        }

        public override void OnClientConnect(NetworkConnection connection)
        {
            base.OnClientConnect(connection);
            Debug.Log("CGSNetManager OnClientConnect...");
            ClientScene.RegisterSpawnHandler(cardModelPrefab.GetComponent<NetworkIdentity>().assetId, SpawnCard, UnSpawnCard);
            Debug.Log("CGSNetManager OnClientConnect!");
        }

        public GameObject SpawnCard(Vector3 position, System.Guid assetId)
        {
            GameObject newCardGO = Instantiate(cardModelPrefab, playController.playAreaContent);
            playController.SetPlayActions(playController.playAreaContent.GetComponent<CardStack>(), newCardGO.GetComponent<CardModel>());
            return newCardGO;
        }

        public void UnSpawnCard(GameObject spawned)
        {
            Destroy(spawned);
        }
    }
}
