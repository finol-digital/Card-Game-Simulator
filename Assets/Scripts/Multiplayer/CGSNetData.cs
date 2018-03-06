using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public struct NetDeck {
    public GameObject owner;
    public List<string> cardIds;
}

public struct NetScore {
    public GameObject owner;
    public int points;
}

public class CGSNetData : NetworkBehaviour
{
    public class SyncListNetDeck : SyncListStruct<NetDeck> {}
    public class SyncListNetScore : SyncListStruct<NetScore> {}
    
    public SyncListNetDeck decks = new SyncListNetDeck();
    public SyncListNetScore scoreboard = new SyncListNetScore();
    
    public void RegisterDeck(GameObject owner, List<string> cardIds)
    {
        CmdAddDeck(owner, cardIds);
    }
    
    [Command]
    public void CmdAddDeck(GameObject owner, List<string> cardIds)
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
