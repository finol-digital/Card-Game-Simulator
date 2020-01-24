using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BranchEvent {

	private Dictionary<string, object> data = new Dictionary<string, object>();
	private Dictionary<string, string> customData = new Dictionary<string, string>();
	private List<string> contentItems = new List<string>();

	// Create custom BranchEvent
	public BranchEvent(string customEventName) {
		AddData("event_name", customEventName);
	}

	// Create standard BranchEvent
	public BranchEvent(BranchEventType branchEventType) {
		AddData("event_name", branchEventType.ToString ());
	}

    // Set the alias
    public void SetAlias(string alias) {
        AddData("customer_event_alias", alias);
    }

	// Set the transaction id associated with this event if there in any
	public void SetTransactionID(string transactionID) {
		AddData ("transaction_id", transactionID);
	}

	// Set any affiliation for this transaction event
	public void SetAffiliation(string affiliation) {
		AddData ("affiliation", affiliation);
	}

	// Set any coupons associated with this transaction event
	public void SetCoupon(string coupon) {
		AddData ("coupon", coupon);
	}

	// Set the currency related with this transaction event
	public void SetCurrency(BranchCurrencyType currency) {
		AddData ("currency", currency.ToString());
	}

	// Set the tax value  related with this transaction event
	public void SetTax(float tax) {
		AddData ("tax", tax);
	}

	// Set the revenue value  related with this transaction event
	public void SetRevenue(float revenue) {
		AddData ("revenue", revenue);
	}

	// Set description for this transaction event
	public void SetDescription(string description) {
		AddData ("description", description);
	}

	// Set the shipping value  related with this transaction event
	public void SetShipping(float shipping) {
		AddData ("shipping", shipping);
	}

	// Set any search query associated with the event
	public void SetSearchQuery(string searchQuery) {
		AddData ("search_query", searchQuery);
	}

	// Adds a custom data property associated with this Branch Event
	public void AddCustomData(string key, string value) {
		if (!customData.ContainsKey(key)) {
			customData.Add (key, value);
		}
		else{
			customData [key] = value;
		}
	}

	// Use this method to add any BranchUniversalObject associated with this event
	public void AddContentItem(BranchUniversalObject contentItem) {
		contentItems.Add (contentItem.ToJsonString ());
	}

	public string ToJsonString() {
		var dataToSend = new Dictionary<string, object>(data);

		if (customData.Count > 0) {
			dataToSend.Add ("custom_data", customData);
		}

		if (contentItems.Count > 0) {
			dataToSend.Add ("content_items", contentItems);
		}

		return BranchThirdParty_MiniJSON.Json.Serialize(dataToSend);
	}

	private void AddData(string key, object value) {
		if (!data.ContainsKey(key)) {
			data.Add (key, value);
		}
		else{
			data [key] = value;
		}
	}
}
