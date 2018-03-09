using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class CGSNetData : NetworkBehaviour
{
    public const string NetworkWarningMessage = "Warning: Invalid network action detected";

    public struct NetScore {
        public GameObject owner;
        public int points;
        public NetScore(GameObject owner, int points) {
            this.owner = owner;
            this.points = points;
        }
    }
    public struct NetDeck {
        public GameObject owner;
        public string[] cardIds;
        public NetDeck(GameObject owner, string[] cardIds) {
            this.owner = owner;
            this.cardIds = cardIds;
        }
    }

    public class SyncListNetScore : SyncListStruct<NetScore> { }
    public class SyncListNetDeck : SyncListStruct<NetDeck> {}

    public SyncListNetScore scoreboard = new SyncListNetScore();
    public SyncListNetDeck decks = new SyncListNetDeck();

    void Start()
    {
        CGSNetManager.Instance.Data = this;
    }

    public void RegisterScore(GameObject owner, int points)
    {
        if (NetworkManager.singleton.isNetworkActive && !isServer) {
            Debug.LogWarning(NetworkWarningMessage);
            return;
        }

        scoreboard.Add(new NetScore(owner, points));
        owner.GetComponent<CGSNetPlayer>().scoreIndex = scoreboard.Count - 1;
        RpcOnChangeScore(scoreboard.Count - 1);
    }

    public void ChangeScore(int scoreIndex, int points)
    {
        if (NetworkManager.singleton.isNetworkActive && !isServer) {
            Debug.LogWarning(NetworkWarningMessage);
            return;
        }

        GameObject owner = scoreboard[scoreIndex].owner;
        scoreboard[scoreIndex] = new NetScore(owner, points);
        RpcOnChangeScore(scoreIndex);
    }

    [ClientRpc]
    public void RpcOnChangeScore(int scoreIndex)
    {
        if (CGSNetManager.Instance.LocalPlayer.scoreIndex == scoreIndex)
            CGSNetManager.Instance.pointsDisplay.UpdateText();
    }

    public void RegisterDeck(GameObject owner, List<string> cardIds)
    {
        if (NetworkManager.singleton.isNetworkActive && !isServer) {
            Debug.LogWarning(NetworkWarningMessage);
            return;
        }

        decks.Add(new NetDeck(owner, cardIds.ToArray()));
    }
}
