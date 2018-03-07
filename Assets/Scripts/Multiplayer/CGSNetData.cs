using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class CGSNetData : NetworkBehaviour
{
    public struct NetDeck {
        public GameObject owner;
        public string[] cardIds;
        public NetDeck(GameObject owner, string[] cardIds) {
            this.owner = owner;
            this.cardIds = cardIds;
        }
    }
    public struct NetScore {
        public GameObject owner;
        public int points;
        public NetScore(GameObject owner, int points) {
            this.owner = owner;
            this.points = points;
        }
    }

    public class SyncListNetDeck : SyncListStruct<NetDeck> {}
    public class SyncListNetScore : SyncListStruct<NetScore> {}

    public SyncListNetDeck decks = new SyncListNetDeck();
    public SyncListNetScore scoreboard = new SyncListNetScore();

    public void RegisterDeck(GameObject owner, List<string> cardIds)
    {
        CmdAddDeck(owner, cardIds.ToArray());
    }

    [Command]
    public void CmdAddDeck(GameObject owner, string[] cardIds)
    {
        decks.Add(new NetDeck(owner, cardIds));
    }

    public void RegisterScore(GameObject owner, int points)
    {
        CmdAddScore(owner, points);
    }

    [Command]
    public void CmdAddScore(GameObject owner, int points)
    {
        scoreboard.Add(new NetScore(owner, points));
    }
}
