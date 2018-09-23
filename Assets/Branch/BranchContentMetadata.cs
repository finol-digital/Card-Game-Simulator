using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BranchContentMetadata {

	private Dictionary<string, object> metadata = new Dictionary<string, object>();

	#region Properties

     // Schema for the qualifying content item. Please see {@link BranchContentSchema}
	public BranchContentSchema contentSchema {
		get {
			if (metadata.ContainsKey ("$content_schema"))
				return (BranchContentSchema)Enum.Parse (typeof(BranchContentSchema), metadata ["$content_schema"].ToString ());
			else
				return BranchContentSchema.NONE;
		}
		set {
			AddData ("$content_schema", value.ToString());
		}
	}

    // Quantity of the thing associated with the qualifying content item
	public float quantity {
		get {
			if (metadata.ContainsKey ("$quantity"))
				return (float)metadata ["$quantity"];
			else
				return 0.0f;
		}
		set {
			AddData ("$quantity", value);
		}
	}

    // Any price associated with the qualifying content item
	public float price {
		get {
			if (metadata.ContainsKey ("$price"))
				return (float)metadata ["$price"];
			else
				return 0.0f;
		}
		set {
			AddData ("$price", value);
		}
	}

    // Currency type associated with the price
	public BranchCurrencyType currencyType {
		get {
			if (metadata.ContainsKey ("$currency"))
				return (BranchCurrencyType)Enum.Parse (typeof(BranchCurrencyType), metadata ["$currency"].ToString ());
			else
				return BranchCurrencyType.NONE;
		}
		set {
			AddData ("$currency", value.ToString());
		}
	}

    // Holds any associated store keeping unit
	public string sku {
		get {
			if (metadata.ContainsKey ("$sku"))
				return metadata ["$sku"].ToString ();
			else
				return "";
		}
		set {
			AddData ("$sku", value.ToString());
		}
	}

    // Name of any product specified by this metadata
	public string productName {
		get {
			if (metadata.ContainsKey ("$product_name"))
				return metadata ["$product_name"].ToString ();
			else
				return "";
		}
		set {
			AddData ("$product_name", value.ToString());
		}
	}

    // Any brand name associated with this metadata
	public string productBrand {
		get {
			if (metadata.ContainsKey ("$product_brand"))
				return metadata ["$product_brand"].ToString ();
			else
				return "";
		}
		set {
			AddData ("$product_brand", value.ToString());
		}
	}

    // Category of product if this metadata is for a product
	// Value should be one of the enumeration from BranchProductCategory
	public BranchProductCategory productCategory {
		get {
			if (metadata.ContainsKey ("$product_category"))
				return metadata ["$product_category"].ToString ().parse();
			else
				return BranchProductCategory.NONE;
		}
		set {
			AddData ("$product_category", value.toString());
		}
	}

    // Condition of the product item. Value is one of the enum constants from BranchCondition
	public BranchCondition condition {
		get {
			if (metadata.ContainsKey ("$condition"))
				return (BranchCondition)Enum.Parse (typeof(BranchCondition), metadata ["$condition"].ToString ());
			else
				return BranchCondition.NONE;
		}
		set {
			AddData ("$condition", value.ToString());
		}
	}

    // Variant of product if this metadata is for a product
	public string productVariant {
		get {
			if (metadata.ContainsKey ("$product_variant"))
				return metadata ["$product_variant"].ToString ();
			else
				return "";
		}
		set {
			AddData ("$product_variant", value.ToString());
		}
	}

    // Average rating for the qualifying content item
	public float ratingAverage {
		get {
			if (metadata.ContainsKey ("$rating_average"))
				return (float)metadata ["$rating_average"];
			else
				return 0.0f;
		}
		set {
			AddData ("$rating_average", value);
		}
	}

    // Total number of ratings for the qualifying content item
	public int ratingCount {
		get {
			if (metadata.ContainsKey ("$rating_count"))
				return (int)metadata ["$rating_count"];
			else
				return 0;
		}
		set {
			AddData ("$rating_count", value);
		}
	}

    // Maximum ratings for the qualifying content item
	public float ratingMax {
		get {
			if (metadata.ContainsKey ("$rating_max"))
				return (float)metadata ["$rating_max"];
			else
				return 0.0f;
		}
		set {
			AddData ("$rating_max", value);
		}
	}

    // Street address associated with the qualifying content item
	public string addressStreet {
		get {
			if (metadata.ContainsKey ("$address_street"))
				return metadata ["$address_street"].ToString ();
			else
				return "";
		}
		set {
			AddData ("$address_street", value.ToString());
		}
	}

    // City name associated with the qualifying content item
	public string addressCity {
		get {
			if (metadata.ContainsKey ("$address_city"))
				return metadata ["$address_city"].ToString ();
			else
				return "";
		}
		set {
			AddData ("$address_city", value.ToString());
		}
	}

    // Region or province name associated with the qualifying content item
	public string addressRegion {
		get {
			if (metadata.ContainsKey ("$address_region"))
				return metadata ["$address_region"].ToString ();
			else
				return "";
		}
		set {
			AddData ("$address_region", value.ToString());
		}
	}

    // Country name associated with the qualifying content item
	public string addressCountry {
		get {
			if (metadata.ContainsKey ("$address_country"))
				return metadata ["$address_country"].ToString ();
			else
				return "";
		}
		set {
			AddData ("$address_country", value.ToString());
		}
	}

    // Postal code associated with the qualifying content item
	public string addressPostalCode {
		get {
			if (metadata.ContainsKey ("$address_postal_code"))
				return metadata ["$address_postal_code"].ToString ();
			else
				return "";
		}
		set {
			AddData ("$address_postal_code", value.ToString());
		}
	}

    // Latitude value  associated with the qualifying content item
	public float latitude {
		get {
			if (metadata.ContainsKey ("$latitude"))
				return (float)metadata ["$latitude"];
			else
				return 0.0f;
		}
		set {
			AddData ("$latitude", value);
		}
	}

	// Longitude value  associated with the qualifying content item
	public float longitude {
		get {
			if (metadata.ContainsKey ("$longitude"))
				return (float)metadata ["$longitude"];
			else
				return 0.0f;
		}
		set {
			AddData ("$longitude", value);
		}
	}


	// Image captions associated with the qualifying content item
	private List<string> imageCaptions = new List<string>();

	public void AddImageCaption(string imageCaption) {
		imageCaptions.Add (imageCaption);
	}

	public List<string> GetImageCaptions() {
		return imageCaptions;
	}

	// Custom metadata associated with the qualifying content item
	private Dictionary<string, string> customMetadata = new Dictionary<string, string>();

	public void AddCustomMetadata(string key, string value) {
		if (!customMetadata.ContainsKey(key)) {
			customMetadata.Add (key, value);
		}
		else{
			customMetadata [key] = value;
		}
	}

	public Dictionary<string, string> GetCustomMetadata() {
		return customMetadata;
	}

	#endregion

	public void setAddress(string street, string city, string region, string country, string postalCode) {
		this.addressStreet = street;
		this.addressCity = city;
		this.addressRegion = region;
		this.addressCountry = country;
		this.addressPostalCode = postalCode;
	}

	public void setLocation(float latitude, float longitude) {
		this.latitude = latitude;
		this.longitude = longitude;
	}

	public void setRating(float averageRating, float maximumRating, int ratingCount) {
		this.ratingAverage = averageRating;
		this.ratingMax = maximumRating;
		this.ratingCount = ratingCount;
	}

	private void AddData(string key, object value) {
		if (!metadata.ContainsKey(key)) {
			metadata.Add (key, value);
		}
		else{
			metadata [key] = value;
		}
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

		if (data.ContainsKey("$content_schema") && data["$content_schema"] != null) {
			contentSchema = (BranchContentSchema)Enum.Parse (typeof(BranchContentSchema), data["$content_schema"].ToString());
			data.Remove ("$content_schema");
		}
		if (data.ContainsKey("$quantity") && data["$quantity"] != null) {
			quantity = Convert.ToSingle(data["$quantity"]);
			data.Remove ("$quantity");
		}
		if (data.ContainsKey("$price") && data["$price"] != null) {
			price = Convert.ToSingle(data["$price"]);
			data.Remove ("$price");
		}
		if (data.ContainsKey("$currency") && data["$currency"] != null) {
			currencyType = (BranchCurrencyType)Enum.Parse (typeof(BranchCurrencyType), data["$currency"].ToString());
			data.Remove ("$currency");
		}
		if (data.ContainsKey("$sku") && data["$sku"] != null) {
			sku = data["$sku"].ToString();
			data.Remove ("$sku");
		}
		if (data.ContainsKey("$product_name") && data["$product_name"] != null) {
			productName = data["$product_name"].ToString();
			data.Remove ("$product_name");
		}
		if (data.ContainsKey("$product_brand") && data["$product_brand"] != null) {
			productBrand = data["$product_brand"].ToString();
			data.Remove ("$product_brand");
		}
		if (data.ContainsKey("$product_category") && data["$product_category"] != null) {
			productCategory = data["$product_category"].ToString().parse();
			data.Remove ("$product_category");
		}
		if (data.ContainsKey("$condition") && data["$condition"] != null) {
			condition = (BranchCondition)Enum.Parse (typeof(BranchCondition), data["$condition"].ToString());
			data.Remove ("$condition");
		}
		if (data.ContainsKey("$product_variant") && data["$product_variant"] != null) {
			productVariant = data["$product_variant"].ToString();
			data.Remove ("$product_variant");
		}
		if (data.ContainsKey("$rating_average") && data["$rating_average"] != null) {
			ratingAverage = Convert.ToSingle(data["$rating_average"]);
			data.Remove ("$rating_average");
		}
		if (data.ContainsKey("$rating_count") && data["$rating_count"] != null) {
			ratingCount = Convert.ToInt32(data["$rating_count"]);
			data.Remove ("$rating_count");
		}
		if (data.ContainsKey("$rating_max") && data["$rating_max"] != null) {
			ratingMax = Convert.ToSingle(data["$rating_max"]);
			data.Remove ("$rating_max");
		}
		if (data.ContainsKey("$address_street") && data["$address_street"] != null) {
			addressStreet = data["$address_street"].ToString();
			data.Remove ("$address_street");
		}
		if (data.ContainsKey("$address_city") && data["$address_city"] != null) {
			addressCity = data["$address_city"].ToString();
			data.Remove ("$address_city");
		}
		if (data.ContainsKey("$address_region") && data["$address_region"] != null) {
			addressRegion = data["$address_region"].ToString();
			data.Remove ("$address_region");
		}
		if (data.ContainsKey("$address_country") && data["$address_country"] != null) {
			addressCountry = data["$address_country"].ToString();
			data.Remove ("$address_country");
		}
		if (data.ContainsKey("$address_postal_code") && data["$address_postal_code"] != null) {
			addressPostalCode = data["$address_postal_code"].ToString();
			data.Remove ("$address_postal_code");
		}
		if (data.ContainsKey("$latitude") && data["$latitude"] != null) {
			latitude = Convert.ToSingle(data["$latitude"]);
			data.Remove ("$latitude");
		}
		if (data.ContainsKey("$longitude") && data["$longitude"] != null) {
			longitude = Convert.ToSingle(data["$longitude"]);
			data.Remove ("$longitude");
		}

		if (data.ContainsKey ("$image_captions")) {
			if (data ["$image_captions"] != null) {
				List<object> imageCaptionsTemp = data ["$image_captions"] as List<object>;

				if (imageCaptionsTemp != null) {
					foreach (object obj in imageCaptionsTemp) {
						imageCaptions.Add (obj.ToString ());
					}
				}

				data.Remove ("$image_captions");
			}
		}

		foreach (string key in data.Keys) {
			customMetadata.Add (key, data [key].ToString ());
		}
	}


	public string ToJsonString() {
		var data = new Dictionary<string, object>(metadata);

		if (imageCaptions.Count > 0) {
			data.Add ("$image_captions", imageCaptions);
		}

		if (customMetadata.Count > 0) {
			foreach (string key in customMetadata.Keys) {
				data.Add (key, customMetadata[key]);
			}
		}

		return BranchThirdParty_MiniJSON.Json.Serialize(data);
	}
}
