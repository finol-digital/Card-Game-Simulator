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
    public SyncListNetDeck cardStacks = new SyncListNetDeck();

    void Start()
    {
        CGSNetManager.Instance.Data = this;
    }

    public override void OnStartClient()
    {
        scoreboard.Callback = OnScoreChanged;
        cardStacks.Callback = OnDeckChanged;
    }

    public void RegisterScore(GameObject owner, int points)
    {
        if (NetworkManager.singleton.isNetworkActive && !isServer) {
            Debug.LogWarning(NetworkWarningMessage);
            return;
        }

        scoreboard.Add(new NetScore(owner, points));
        owner.GetComponent<CGSNetPlayer>().scoreIndex = scoreboard.Count - 1;
    }

    public void ChangeScore(int scoreIndex, int points)
    {
        if (NetworkManager.singleton.isNetworkActive && !isServer) {
            Debug.LogWarning(NetworkWarningMessage);
            return;
        }

        GameObject owner = scoreboard[scoreIndex].owner;
        scoreboard[scoreIndex] = new NetScore(owner, points);
    }

    private void OnScoreChanged(SyncListNetScore.Operation op, int scoreIndex)
    {
        if (op == SyncList<NetScore>.Operation.OP_ADD)
            return;

        CGSNetManager.Instance.LocalPlayer.OnChangeScore(scoreIndex);
    }

    public void RegisterDeck(GameObject owner, string[] cardIds)
    {
        if (NetworkManager.singleton.isNetworkActive && !isServer) {
            Debug.LogWarning(NetworkWarningMessage);
            return;
        }

        cardStacks.Add(new NetDeck(owner, cardIds));
        owner.GetComponent<CGSNetPlayer>().deckIndex = cardStacks.Count - 1;
    }

    public void ChangeDeck(int deckIndex, string[] cardIds)
    {
        if (NetworkManager.singleton.isNetworkActive && !isServer) {
            Debug.LogWarning(NetworkWarningMessage);
            return;
        }

        GameObject owner = cardStacks[deckIndex].owner;
        cardStacks[deckIndex] = new NetDeck(owner, cardIds);
    }

    private void OnDeckChanged(SyncListNetDeck.Operation op, int deckIndex)
    {
        if (op == SyncList<NetDeck>.Operation.OP_ADD)
            return;

        CGSNetManager.Instance.LocalPlayer.OnChangeDeck(deckIndex);
    }
}
