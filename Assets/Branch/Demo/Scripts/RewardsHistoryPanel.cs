using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class RewardsHistoryPanel : MonoBehaviour {

	public ScrollRect scroll;
	public GameObject contentContainer;
	public GameObject itemPrefab;
	private List<RewardTransactionItem> items = new List<RewardTransactionItem>();


	void OnEnable() {
		Branch.getCreditHistory( (historyList, error) =>  {

			if (error != null) {
				Debug.LogError("Branch.getCreditHistory failed: " + error);
				AddItem("error");

			} else if (historyList != null && historyList.Count > 0) {
				List<string> resList = new List<string>();
				Parse(historyList, resList);

				foreach(string str in resList) {
					Debug.Log(str);
					AddItem(str);
				}

				scroll.verticalNormalizedPosition = 1.0f;
			} else {
				AddItem("empty");
			}
		});
	}

	void OnDisable() {
		foreach(RewardTransactionItem obj in items)
			Destroy(obj.gameObject);

		items.Clear();
	}

	void AddItem(string text) {
		GameObject item = Instantiate(itemPrefab) as GameObject;
		
		if (item != null) {
			item.GetComponent<RewardTransactionItem>().label.text = text;
			item.transform.SetParent(contentContainer.transform, false);
			items.Add(item.GetComponent<RewardTransactionItem>());
			item.SetActive(true);
		}
	}

	#region Parse list of object

	// historyList is list of
	// Dictionary<string,object>
	// {
	//   transaction : Dictionary<string,object>
	//   {
	//     type   : int
	//     id     : string
	//     bucket : string
	//     amount : int
	//     date   : string
	//   }
	//
	//   event : Dictionary<string,object>
	//   {
	//     name   : string (optional)
	//
	//     metadata : Dictionary<string,object>
	//     {
	//       ip : string (optional)
	//     }
	//   }
	//
	//   referrer : string (optional)
	//   referree : string (optional)
	
	void Parse(List<object> list, List<string> resList) {
		string res = "";
		
		foreach(object dict in list) {
			if (dict != null && dict.GetType() == typeof(Dictionary<string, object>)) {
				res = ParseTransaction(dict as Dictionary<string, object>);
				res += "   ";
				res += ParseEvent(dict as Dictionary<string, object>);
				res += "   ";
				res += ParseReferrer(dict as Dictionary<string, object>);
				res += "   ";
				res += ParseReferree(dict as Dictionary<string, object>);
				
				resList.Add(res);
			}
		}
	}
	
	string ParseTransaction(Dictionary<string, object> dict) {
		string strRes = "";
		
		if (dict.ContainsKey("transaction")) {
			Dictionary<string, object> transactionDict = dict["transaction"] as Dictionary<string, object>;
			
			if (transactionDict != null) {
				if (transactionDict.ContainsKey("type") && transactionDict["type"] != null) {
					strRes += "type = " + transactionDict["type"].ToString();
				}
				
				if (transactionDict.ContainsKey("id") && transactionDict["id"] != null) {
					strRes += "  id = " + transactionDict["id"].ToString();
				}

				if (transactionDict.ContainsKey("bucket") && transactionDict["bucket"] != null) {
					strRes += "  bucket = " + transactionDict["bucket"].ToString();
				}

				if (transactionDict.ContainsKey("amount") && transactionDict["amount"] != null) {
					strRes += "  amount = " + transactionDict["amount"].ToString();
				}

				if (transactionDict.ContainsKey("date") && transactionDict["date"] != null) {
					strRes += "  date = " + transactionDict["date"].ToString();
				}
			}
		}
		
		return strRes;
	}
	
	string ParseEvent(Dictionary<string, object> dict) {
		string strRes = "";
		
		if (dict.ContainsKey("event")) {
			Dictionary<string, object> transactionDict = dict["event"] as Dictionary<string, object>;
			
			if (transactionDict != null) {
				if (transactionDict.ContainsKey("name") && transactionDict["name"] != null) {
					strRes += "  name = " + transactionDict["name"].ToString();
				}
				
				if (transactionDict.ContainsKey("metadata") && transactionDict["metadata"] != null) {
					Dictionary<string, object> dictMetadata = transactionDict["metadata"] as Dictionary<string, object>;
					
					if (dictMetadata != null && dictMetadata.ContainsKey("ip") && dictMetadata["ip"] != null) {
						strRes += "  ip = " + dictMetadata["ip"].ToString();
					}
				}
			}
		}
		
		return strRes;
	}
	
	string ParseReferrer(Dictionary<string, object> dict) {
		string strRes = "";
		
		if (dict != null && dict.ContainsKey("referrer") && dict["referrer"] != null ) {
			strRes += "  referrer = " + dict["referrer"].ToString();
		}
		
		return strRes;
	}
	
	string ParseReferree(Dictionary<string, object> dict) {
		string strRes = "";
		
		if (dict != null && dict.ContainsKey("referree") && dict["referree"] != null ) {
			strRes += "  referree = " + dict["referree"].ToString();
		}
		
		return strRes;
	}

	#endregion
}
