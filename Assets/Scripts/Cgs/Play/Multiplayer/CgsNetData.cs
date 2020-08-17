/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Mirror;
using UnityEngine;

namespace Cgs.Play.Multiplayer
{
    public class CgsNetData : NetworkBehaviour
    {
        private const string NetworkWarningMessage = "Warning: Invalid network action detected";

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

        public class SyncListNetScore : SyncList<NetScore>
        {
        }

        public readonly SyncListNetScore Scores = new SyncListNetScore();

        private void Start()
        {
            CgsNetManager.Instance.Data = this;
        }

        public override void OnStartClient()
        {
            Scores.Callback += OnScoreChanged;
        }

        public void RegisterScore(GameObject owner, int points)
        {
            if (NetworkManager.singleton.isNetworkActive && !isServer)
            {
                Debug.LogWarning(NetworkWarningMessage);
                return;
            }

            Scores.Add(new NetScore(owner, points));
            owner.GetComponent<CgsNetPlayer>().scoreIndex = Scores.Count - 1;
        }

        public void ChangeScore(int scoreIndex, int points)
        {
            if (NetworkManager.singleton.isNetworkActive && !isServer)
            {
                Debug.LogWarning(NetworkWarningMessage);
                return;
            }

            GameObject owner = Scores[scoreIndex].Owner;
            Scores[scoreIndex] = new NetScore(owner, points);
        }

        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void OnScoreChanged(SyncList<NetScore>.Operation op, int scoreIndex, NetScore oldScore,
            NetScore newScore)
        {
            if (op == SyncList<NetScore>.Operation.OP_ADD)
                return;

            CgsNetManager.Instance.LocalPlayer.OnChangeScore(scoreIndex, scoreIndex);
        }
    }
}
