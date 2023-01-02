/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Cgs.CardGameView.Multiplayer;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace Cgs.Play.Multiplayer
{
    public class CgsNetManager : NetworkManager
    {
        public static CgsNetManager Instance => (CgsNetManager) Singleton;

        public bool IsOnline => IsHost || IsConnectedClient;

        public CgsNetPlayer LocalPlayer { get; set; }

        public UnityTransport Transport => (UnityTransport) NetworkConfig.NetworkTransport;

        public string RoomIdIp => "127.0.0.1".Equals(Transport.ConnectionData.Address,
            StringComparison.Ordinal)
            ? RoomId
            : Transport.ConnectionData.Address;

        private string RoomId => // todo: string.IsNullOrEmpty(lrm.serverId) ?
            Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(f => f.AddressFamily == AddressFamily.InterNetwork)
                .ToString();
        // TODO: lrm.serverId;

        public void Restart()
        {
            foreach (var cardStack in PlayController.Instance.playMat.GetComponentsInChildren<CardStack>())
                cardStack.MyNetworkObject.Despawn();
            foreach (var cardModel in PlayController.Instance.playMat.GetComponentsInChildren<CardModel>())
                cardModel.MyNetworkObject.Despawn();
            foreach (var die in PlayController.Instance.playMat.GetComponentsInChildren<Die>())
                die.MyNetworkObject.Despawn();
            foreach (var player in FindObjectsOfType<CgsNetPlayer>())
                player.RestartClientRpc(player.OwnerClientRpcParams);
        }
    }
}
