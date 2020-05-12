/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;
using Mirror;

namespace Cgs.Play.Multiplayer
{
    public class CgsNetData : NetworkBehaviour
    {
        private const string NetworkWarningMessage = "Warning: Invalid network action detected";

        public readonly struct NetCardStack
        {
            public readonly GameObject Owner;
            public readonly string[] CardIds;
            public NetCardStack(GameObject owner, string[] cardIds)
            {
                Owner = owner;
                CardIds = cardIds;
            }
        }

        public readonly struct NetScore
        {
            public readonly GameObject Owner;
            public readonly int Points;
            public NetScore(GameObject owner, int points)
            {
                Owner = owner;
                Points = points;
            }
        }

        public class SyncListNetCardStack : SyncList<NetCardStack> { }
        public class SyncListNetScore : SyncList<NetScore> { }

        public SyncListNetCardStack cardStacks = new SyncListNetCardStack();
        public SyncListNetScore scores = new SyncListNetScore();

        private void Start()
        {
            CgsNetManager.Instance.Data = this;
        }

        public override void OnStartClient()
        {
            cardStacks.Callback += OnDeckChanged;
            scores.Callback += OnScoreChanged;
        }

        public void RegisterDeck(GameObject owner, string[] cardIds)
        {
            if (NetworkManager.singleton.isNetworkActive && !isServer)
            {
                Debug.LogWarning(NetworkWarningMessage);
                return;
            }

            cardStacks.Add(new NetCardStack(owner, cardIds));
            owner.GetComponent<CgsNetPlayer>().deckIndex = cardStacks.Count - 1;
        }

        public void ChangeDeck(int deckIndex, string[] cardIds)
        {
            if (NetworkManager.singleton.isNetworkActive && !isServer)
            {
                Debug.LogWarning(NetworkWarningMessage);
                return;
            }

            GameObject owner = cardStacks[deckIndex].Owner;
            cardStacks[deckIndex] = new NetCardStack(owner, cardIds);
        }

        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void OnDeckChanged(SyncListNetCardStack.Operation op, int deckIndex, NetCardStack oldDeck, NetCardStack newDeck)
        {
            if (op == SyncList<NetCardStack>.Operation.OP_ADD)
                return;

            CgsNetManager.Instance.LocalPlayer.OnChangeDeck(deckIndex, deckIndex);
        }

        public void RegisterScore(GameObject owner, int points)
        {
            if (NetworkManager.singleton.isNetworkActive && !isServer)
            {
                Debug.LogWarning(NetworkWarningMessage);
                return;
            }

            scores.Add(new NetScore(owner, points));
            owner.GetComponent<CgsNetPlayer>().scoreIndex = scores.Count - 1;
        }

        public void ChangeScore(int scoreIndex, int points)
        {
            if (NetworkManager.singleton.isNetworkActive && !isServer)
            {
                Debug.LogWarning(NetworkWarningMessage);
                return;
            }

            GameObject owner = scores[scoreIndex].Owner;
            scores[scoreIndex] = new NetScore(owner, points);
        }

        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void OnScoreChanged(SyncListNetScore.Operation op, int scoreIndex, NetScore oldScore, NetScore newScore)
        {
            if (op == SyncList<NetScore>.Operation.OP_ADD)
                return;

            CgsNetManager.Instance.LocalPlayer.OnChangeScore(scoreIndex, scoreIndex);
        }

    }
}
