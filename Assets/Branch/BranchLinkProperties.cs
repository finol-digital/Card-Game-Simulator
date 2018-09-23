using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class BranchLinkProperties {

	public List<String> tags;
	public string feature;
	public string alias;
	public string channel;
	public string stage;
	public int matchDuration;
	public Dictionary<String, String> controlParams;


	public BranchLinkProperties() {
		tags =  new List<String>();
		feature = "";
		alias = "";
		channel = "";
		stage = "";
		matchDuration = 0;
		controlParams = new Dictionary<String, String>();
	}

	public BranchLinkProperties(string json) {
		tags =  new List<String>();
		feature = "";
		alias = "";
		channel = "";
		stage = "";
		matchDuration = 0;
		controlParams = new Dictionary<String, String>();

		loadFromJson(json);
	}

	public BranchLinkProperties(Dictionary<string, object> data) {
		tags =  new List<String>();
		feature = "";
		alias = "";
		channel = "";
		stage = "";
		matchDuration = 0;
		controlParams = new Dictionary<String, String>();
		
		loadFromDictionary(data);
	}

	public void loadFromJson(string json) {
		if (string.IsNullOrEmpty(json))
			return;

		var data = BranchThirdParty_MiniJSON.Json.Deserialize(json) as Dictionary<string, object>;
		loadFromDictionary(data);
	}

	public void loadFromDictionary(Dictionary<string, object> data) {
		if (data == null)
			return;

		if (data.ContainsKey("~tags") && data["~tags"] != null) {
			List<object> tempList = data["~tags"] as List<object>;

			if (tempList != null) {
				foreach(object obj in tempList) {
					if (obj != null) {
						tags.Add (obj.ToString ());
					}
				}
			}
		}
		if (data.ContainsKey("~feature") && data["~feature"] != null) {
			feature = data["~feature"].ToString();
		}
		if (data.ContainsKey("~alias") && data["~alias"] != null) {
			alias = data["~alias"].ToString();
		}
		if (data.ContainsKey("~channel") && data["~channel"] != null) {
			channel = data["~channel"].ToString();
		}
		if (data.ContainsKey("~stage") && data["~stage"] != null) {
			stage = data["~stage"].ToString();
		}
		if (data.ContainsKey("~duration")) {
			if (!string.IsNullOrEmpty(data["~duration"].ToString())) {
				matchDuration = Convert.ToInt32(data["~duration"].ToString());
			}
		}
		if (data.ContainsKey("control_params")) {
			if (data["control_params"] != null) {
				Dictionary<string, object> paramsTemp = data["control_params"] as Dictionary<string, object>;

				if (paramsTemp != null) {
					foreach(string key in paramsTemp.Keys) {
						if (paramsTemp [key] != null) {
							controlParams.Add (key, paramsTemp [key].ToString ());
						}
					}
				}
			}
		}
	}

	public string ToJsonString() {
		var data = new Dictionary<string, object>();
		
		data.Add("~tags", tags);
		data.Add("~feature", feature);
		data.Add("~alias", alias);
		data.Add("~channel", channel);
		data.Add("~stage", stage);
		data.Add("~duration", matchDuration.ToString());
		data.Add("control_params", controlParams);
		
		return BranchThirdParty_MiniJSON.Json.Serialize(data);
	}
}
