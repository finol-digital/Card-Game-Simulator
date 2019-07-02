/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;
using Mirror;

using CardGameView;

namespace CGS.Play.Multiplayer
{
    public class CGSNetManager : NetworkManager
    {
        public const string PlayerCountMessage = "Number of connected players: ";
        public const string ConnectionIdMessage = "Connection Id: ";

        public static CGSNetManager Instance => (CGSNetManager)singleton;
        public CGSNetPlayer LocalPlayer { get; set; }
        public CGSNetData Data { get; set; }

        public GameObject cardModelPrefab;
        public PlayMode playController;
        public PointsCounter pointsDisplay;

        public override void OnStartServer()
        {
            base.OnStartServer();
            CardGameManager.Instance.discovery.StartAsHost();
        }

        public override void OnServerAddPlayer(NetworkConnection conn, AddPlayerMessage message)
        {
            base.OnServerAddPlayer(conn, message);
            if (Data == null)
            {
                Data = Instantiate(spawnPrefabs[0]).GetOrAddComponent<CGSNetData>();
                NetworkServer.Spawn(Data.gameObject);
            }
            Data.RegisterScore(conn.playerController.gameObject, CardGameManager.Current.GameStartPointsCount);
            playController.netText.text = PlayerCountMessage + NetworkServer.connections.Count.ToString();
        }

        public override void OnClientConnect(NetworkConnection connection)
        {
            base.OnClientConnect(connection);
            ClientScene.RegisterSpawnHandler(cardModelPrefab.GetComponent<NetworkIdentity>().assetId, SpawnCard, UnSpawnCard);
            playController.netText.text = ConnectionIdMessage + connection.connectionId;
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

        public override void OnStopServer()
        {
            base.OnStopServer();
            if (CardGameManager.Instance.discovery.running)
                CardGameManager.Instance.discovery.StopBroadcast();
            Debug.Log("Server stopped");
        }
    }
}
