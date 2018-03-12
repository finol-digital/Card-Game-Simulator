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
    public struct NetCardStack {
        public GameObject owner;
        public string[] cardIds;
        public NetCardStack(GameObject owner, string[] cardIds) {
            this.owner = owner;
            this.cardIds = cardIds;
        }
    }

    public class SyncListNetScore : SyncListStruct<NetScore> { }
    public class SyncListNetCardStack : SyncListStruct<NetCardStack> {}

    public SyncListNetScore scores = new SyncListNetScore();
    public SyncListNetCardStack cardStacks = new SyncListNetCardStack();

    void Start()
    {
        CGSNetManager.Instance.Data = this;
    }

    public override void OnStartClient()
    {
        scores.Callback = OnScoreChanged;
        cardStacks.Callback = OnDeckChanged;
    }

    public void RegisterScore(GameObject owner, int points)
    {
        if (NetworkManager.singleton.isNetworkActive && !isServer) {
            Debug.LogWarning(NetworkWarningMessage);
            return;
        }

        scores.Add(new NetScore(owner, points));
        owner.GetComponent<CGSNetPlayer>().scoreIndex = scores.Count - 1;
    }

    public void ChangeScore(int scoreIndex, int points)
    {
        if (NetworkManager.singleton.isNetworkActive && !isServer) {
            Debug.LogWarning(NetworkWarningMessage);
            return;
        }

        GameObject owner = scores[scoreIndex].owner;
        scores[scoreIndex] = new NetScore(owner, points);
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

        cardStacks.Add(new NetCardStack(owner, cardIds));
        owner.GetComponent<CGSNetPlayer>().deckIndex = cardStacks.Count - 1;
    }

    public void ChangeDeck(int deckIndex, string[] cardIds)
    {
        if (NetworkManager.singleton.isNetworkActive && !isServer) {
            Debug.LogWarning(NetworkWarningMessage);
            return;
        }

        GameObject owner = cardStacks[deckIndex].owner;
        cardStacks[deckIndex] = new NetCardStack(owner, cardIds);
    }

    private void OnDeckChanged(SyncListNetCardStack.Operation op, int deckIndex)
    {
        if (op == SyncList<NetCardStack>.Operation.OP_ADD)
            return;

        CGSNetManager.Instance.LocalPlayer.OnChangeDeck(deckIndex);
    }
}
