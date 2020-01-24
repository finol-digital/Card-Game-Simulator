using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BranchCurrencyType {
	AED, AFN, ALL, AMD, ANG, AOA, ARS, AUD, AWG, AZN, BAM, BBD,
	BDT, BGN, BHD, BIF, BMD, BND, BOB, BOV, BRL, BSD, BTN, BWP,
	BYN, BYR, BZD, CAD, CDF, CHE, CHF, CHW, CLF, CLP, CNY, COP,
	COU, CRC, CUC, CUP, CVE, CZK, DJF, DKK, DOP, DZD, EGP, ERN,
	ETB, EUR, FJD, FKP, GBP, GEL, GHS, GIP, GMD, GNF, GTQ, GYD,
	HKD, HNL, HRK, HTG, HUF, IDR, ILS, INR, IQD, IRR, ISK, JMD,
	JOD, JPY, KES, KGS, KHR, KMF, KPW, KRW, KWD, KYD, KZT, LAK,
	LBP, LKR, LRD, LSL, LYD, MAD, MDL, MGA, MKD, MMK, MNT, MOP,
	MRO, MUR, MVR, MWK, MXN, MXV, MYR, MZN, NAD, NGN, NIO, NOK,
	NPR, NZD, OMR, PAB, PEN, PGK, PHP, PKR, PLN, PYG, QAR, RON,
	RSD, RUB, RWF, SAR, SBD, SCR, SDG, SEK, SGD, SHP, SLL, SOS,
	SRD, SSP, STD, SYP, SZL, THB, TJS, TMT, TND, TOP, TRY, TTD,
	TWD, TZS, UAH, UGX, USD, USN, UYI, UYU, UZS, VEF, VND, VUV,
	WST, XAF, XAG, XAU, XBA, XBB, XBC, XBD, XCD, XDR, XFU, XOF,
	XPD, XPF, XPT, XSU, XTS, XUA, XXX, YER, ZAR, ZMW, NONE
}

public enum BranchEventType {
	// Commerce events
	ADD_TO_CART,
	ADD_TO_WISHLIST,
	VIEW_CART,
	INITIATE_PURCHASE,
	ADD_PAYMENT_INFO,
	PURCHASE,
	SPEND_CREDITS,
    SUBSCRIBE,
    START_TRIAL,
    CLICK_AD,
    VIEW_AD,

    // Content events
    SEARCH,
	VIEW_ITEM,
	VIEW_ITEMS,
	RATE,
	SHARE,

	// User lifecycle events
	COMPLETE_REGISTRATION,
	COMPLETE_TUTORIAL,
	ACHIEVE_LEVEL,
	UNLOCK_ACHIEVEMENT,
    INVITE,
    LOGIN,
    RESERVE
}

public enum BranchContentSchema {
	COMMERCE_AUCTION,
	COMMERCE_BUSINESS,
	COMMERCE_OTHER,
	COMMERCE_PRODUCT,
	COMMERCE_RESTAURANT,
	COMMERCE_SERVICE,
	COMMERCE_TRAVEL_FLIGHT,
	COMMERCE_TRAVEL_HOTEL,
	COMMERCE_TRAVEL_OTHER,
	GAME_STATE,
	MEDIA_IMAGE,
	MEDIA_MIXED,
	MEDIA_MUSIC,
	MEDIA_OTHER,
	MEDIA_VIDEO,
	OTHER,
	TEXT_ARTICLE,
	TEXT_BLOG,
	TEXT_OTHER,
	TEXT_RECIPE,
	TEXT_REVIEW,
	TEXT_SEARCH_RESULTS,
	TEXT_STORY,
	TEXT_TECHNICAL_DOC,
	NONE
}

public enum BranchProductCategory {
	ANIMALS_AND_PET_SUPPLIES,
	APPAREL_AND_ACCESSORIES,
	ARTS_AND_ENTERTAINMENT,
	BABY_AND_TODDLER,
	BUSINESS_AND_INDUSTRIAL,
	CAMERAS_AND_OPTICS,
	ELECTRONICS,
	FOOD_BEVERAGES_AND_TOBACCO,
	FURNITURE,
	HARDWARE,
	HEALTH_AND_BEAUTY,
	HOME_AND_GARDEN,
	LUGGAGE_AND_BAGS,
	MATURE,
	MEDIA,
	OFFICE_SUPPLIES,
	RELIGIOUS_AND_CEREMONIAL,
	SOFTWARE,
	SPORTING_GOODS,
	TOYS_AND_GAMES,
	VEHICLES_AND_PARTS,
	NONE
}

public enum BranchCondition {
	OTHER, NEW, GOOD, FAIR, POOR, USED, REFURBISHED, EXCELLENT, NONE
}

public static class BranchEnumExtensions
{        
	public static string toString(this BranchProductCategory category)
	{
		switch(category) {
		case BranchProductCategory.ANIMALS_AND_PET_SUPPLIES:
			return "Animals & Pet Supplies";
		case BranchProductCategory.APPAREL_AND_ACCESSORIES:
			return "Apparel & Accessories";
		case BranchProductCategory.ARTS_AND_ENTERTAINMENT:
			return "Arts & Entertainment";
		case BranchProductCategory.BABY_AND_TODDLER:
			return "Baby & Toddler";
		case BranchProductCategory.BUSINESS_AND_INDUSTRIAL:
			return "Business & Industrial";
		case BranchProductCategory.CAMERAS_AND_OPTICS:
			return "Cameras & Optics";
		case BranchProductCategory.ELECTRONICS:
			return "Electronics";
		case BranchProductCategory.FOOD_BEVERAGES_AND_TOBACCO:
			return "Food, Beverages & Tobacco";
		case BranchProductCategory.FURNITURE:
			return "Furniture";
		case BranchProductCategory.HARDWARE:
			return "Hardware";
		case BranchProductCategory.HEALTH_AND_BEAUTY:
			return "Health & Beauty";
		case BranchProductCategory.HOME_AND_GARDEN:
			return "Home & Garden";
		case BranchProductCategory.LUGGAGE_AND_BAGS:
			return "Luggage & Bags";
		case BranchProductCategory.MATURE:
			return "Mature";
		case BranchProductCategory.MEDIA:
			return "Media";
		case BranchProductCategory.OFFICE_SUPPLIES:
			return "Office Supplies";
		case BranchProductCategory.RELIGIOUS_AND_CEREMONIAL:
			return "Religious & Ceremonial";
		case BranchProductCategory.SOFTWARE:
			return "Software";
		case BranchProductCategory.SPORTING_GOODS:
			return "Sporting Goods";
		case BranchProductCategory.TOYS_AND_GAMES:
			return "Toys & Games";
		case BranchProductCategory.VEHICLES_AND_PARTS:
			return "Vehicles & Parts";
		}

		return "";
	}

	public static BranchProductCategory parse(this string category)
	{
		if (category.Equals("Animals & Pet Supplies")) {
			return BranchProductCategory.ANIMALS_AND_PET_SUPPLIES;
		}
		if (category.Equals("Apparel & Accessories")) {
			return BranchProductCategory.APPAREL_AND_ACCESSORIES;
		}
		if (category.Equals("Arts & Entertainment")) {
			return BranchProductCategory.ARTS_AND_ENTERTAINMENT;
		}
		if (category.Equals("Baby & Toddler")) {
			return BranchProductCategory.BABY_AND_TODDLER;
		}
		if (category.Equals("Business & Industrial")) {
			return BranchProductCategory.BUSINESS_AND_INDUSTRIAL;
		}
		if (category.Equals("Cameras & Optics")) {
			return BranchProductCategory.CAMERAS_AND_OPTICS;
		}
		if (category.Equals("Electronics")) {
			return BranchProductCategory.ELECTRONICS;
		}
		if (category.Equals("Food, Beverages & Tobacco")) {
			return BranchProductCategory.FOOD_BEVERAGES_AND_TOBACCO;
		}
		if (category.Equals("Furniture")) {
			return BranchProductCategory.FURNITURE;
		}
		if (category.Equals("Hardware")) {
			return BranchProductCategory.HARDWARE;
		}
		if (category.Equals("Health & Beauty")) {
			return BranchProductCategory.HEALTH_AND_BEAUTY;
		}
		if (category.Equals("Home & Garden")) {
			return BranchProductCategory.HOME_AND_GARDEN;
		}
		if (category.Equals("Luggage & Bags")) {
			return BranchProductCategory.LUGGAGE_AND_BAGS;
		}
		if (category.Equals("Mature")) {
			return BranchProductCategory.MATURE;
		}
		if (category.Equals("Media")) {
			return BranchProductCategory.MEDIA;
		}
		if (category.Equals("Office Supplies")) {
			return BranchProductCategory.OFFICE_SUPPLIES;
		}
		if (category.Equals("Religious & Ceremonial")) {
			return BranchProductCategory.RELIGIOUS_AND_CEREMONIAL;
		}
		if (category.Equals("Software")) {
			return BranchProductCategory.SOFTWARE;
		}
		if (category.Equals("Sporting Goods")) {
			return BranchProductCategory.SPORTING_GOODS;
		}
		if (category.Equals("Toys & Games")) {
			return BranchProductCategory.TOYS_AND_GAMES;
		}
		if (category.Equals("Vehicles & Parts")) {
			return BranchProductCategory.VEHICLES_AND_PARTS;
		}

		return BranchProductCategory.NONE;
	}
}