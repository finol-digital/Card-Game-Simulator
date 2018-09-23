using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;

public class BranchAndroidWrapper {
#if UNITY_ANDROID
    
    public static void setBranchKey(String branchKey) {
		_runBlockOnThread(() => {
        	_getBranchClass().CallStatic("setBranchKey", branchKey);
		});
    }
    
	public static void getAutoInstance() {
		_runBlockOnThread(() => {
			_getBranchClass().CallStatic("getAutoInstance");
		});
	}

	#region InitSession methods
    
    public static void initSession() {
		_runBlockOnThread(() => {
        	_getBranchClass().CallStatic("initSession");
		});
    }

	public static void initSessionAsReferrable(bool isReferrable) {
		_runBlockOnThread(() => {
			_getBranchClass().CallStatic("initSession", isReferrable);
		});
	}

	public static void initSessionWithCallback(string callbackId) {
		_runBlockOnThread(() => {
			_getBranchClass().CallStatic("initSession", callbackId);
		});
	}

	public static void initSessionAsReferrableWithCallback(bool isReferrable, string callbackId) {
		_runBlockOnThread(() => {
			_getBranchClass().CallStatic("initSession", callbackId, isReferrable);
		});
	}

	public static void initSessionWithUniversalObjectCallback(string callbackId) {
		_runBlockOnThread(() => {
			_getBranchClass().CallStatic("initSessionWithUniversalObjectCallback", callbackId);
		});
	}
    
	#endregion
    
	#region Session Item methods
    
	public static string getFirstReferringBranchUniversalObject() {
		return _getBranchClass().CallStatic<string>("getFirstReferringBranchUniversalObject");
	}
	
	public static string getFirstReferringBranchLinkProperties() {
		return _getBranchClass().CallStatic<string>("getFirstReferringBranchLinkProperties");
	}
    
	public static string getLatestReferringBranchUniversalObject() {
		return _getBranchClass().CallStatic<string>("getLatestReferringBranchUniversalObject");
	}
	
	public static string getLatestReferringBranchLinkProperties() {
		return _getBranchClass().CallStatic<string>("getLatestReferringBranchLinkProperties");
	}

    public static void resetUserSession() {
		_runBlockOnThread(() => {
        	_getBranchClass().CallStatic("resetUserSession");
		});
    }
    
    public static void setIdentity(string userId) {
		_runBlockOnThread(() => {
        	_getBranchClass().CallStatic("setIdentity", userId);
		});
    }
    
    public static void setIdentityWithCallback(string userId, string callbackId) {
		_runBlockOnThread(() => {
        	_getBranchClass().CallStatic("setIdentity", userId, callbackId);
		});
    }
    
    public static void logout() {
		_runBlockOnThread(() => {
        	_getBranchClass().CallStatic("logout");
		});
    }
    
	#endregion
    
	#region Configuration methods

    public static void setDebug() {
		_runBlockOnThread(() => {
        	_getBranchClass().CallStatic("setDebug");
		});
    }
    
    public static void setRetryInterval(int retryInterval) {
		_runBlockOnThread(() => {
        	_getBranchClass().CallStatic("setRetryInterval", retryInterval);
		});
    }
    
    public static void setMaxRetries(int maxRetries) {
		_runBlockOnThread(() => {
        	_getBranchClass().CallStatic("setMaxRetries", maxRetries);
		});
    }
    
    public static void setNetworkTimeout(int timeout) {
		_runBlockOnThread(() => {
        	_getBranchClass().CallStatic("setNetworkTimeout", timeout);
		});
    }
    
	public static void registerView(string universalObject) {
		_runBlockOnThread(() => {
			_getBranchClass().CallStatic("registerView", universalObject);
		});
	}

	public static void listOnSpotlight(string universalObject) {
		_runBlockOnThread(() => {
			_getBranchClass().CallStatic("listOnSpotlight", universalObject);
		});
	}

	public static void accountForFacebookSDKPreventingAppLaunch() {
		_runBlockOnThread(() => {
		_getBranchClass().CallStatic("accountForFacebookSDKPreventingAppLaunch");
		});
	}

	public static void setRequestMetadata(string key, string val) {
		_runBlockOnThread(() => {
		_getBranchClass().CallStatic("setRequestMetadata", key, val);
		});
	}

	public static void setTrackingDisabled(bool value) {
	    _runBlockOnThread(() => {
	    _getBranchClass().CallStatic("setTrackingDisabled", value);
        });
	}

	#endregion
    
	#region User Action methods
    
    public static void userCompletedAction(string action) {
		_runBlockOnThread(() => {
        	_getBranchClass().CallStatic("userCompletedAction", action);
		});
    }
    
    public static void userCompletedActionWithState(string action, string stateDict) {
		_runBlockOnThread(() => {
        	_getBranchClass().CallStatic("userCompletedAction", action, stateDict);
		});
    }
    
	public static void sendEvent(string eventName) {
		_runBlockOnThread(() => {
			_getBranchClass().CallStatic("sendEvent", eventName);
		});
	}

	#endregion
    
	#region Credit methods
    
    public static void loadRewardsWithCallback(string callbackId) {
		_runBlockOnThread(() => {
        	_getBranchClass().CallStatic("loadRewards", callbackId);
		});
    }
    
    public static int getCredits() {
        return _getBranchClass().CallStatic<int>("getCredits");
    }
    
    public static void redeemRewards(int count) {
		_runBlockOnThread(() => {
        	_getBranchClass().CallStatic("redeemRewards", count);
		});
    }
    
    public static int getCreditsForBucket(string bucket) {
        return _getBranchClass().CallStatic<int>("getCreditsForBucket", bucket);
    }
    
    public static void redeemRewardsForBucket(int count, string bucket) {
		_runBlockOnThread(() => {
        	_getBranchClass().CallStatic("redeemRewards", bucket, count);
		});
    }
    
    public static void getCreditHistoryWithCallback(string callbackId) {
		_runBlockOnThread(() => {
        	_getBranchClass().CallStatic("getCreditHistory", callbackId);
		});
    }
    
    public static void getCreditHistoryForBucketWithCallback(string bucket, string callbackId) {
		_runBlockOnThread(() => {
        	_getBranchClass().CallStatic("getCreditHistory", bucket, callbackId);
		});
    }
    
    public static void getCreditHistoryForTransactionWithLengthOrderAndCallback(string creditTransactionId, int length, int order, string callbackId) {
		_runBlockOnThread(() => {
        	_getBranchClass().CallStatic("getCreditHistory", creditTransactionId, length, order, callbackId);
		});
    }
    
    public static void getCreditHistoryForBucketWithTransactionLengthOrderAndCallback(string bucket, string creditTransactionId, int length, int order, string callbackId) {
		_runBlockOnThread(() => {
        	_getBranchClass().CallStatic("getCreditHistory", bucket, creditTransactionId, length, order, callbackId);
		});
    }
    
	#endregion

	#region Share Link methods

	public static void shareLinkWithLinkProperties(string universalObject, string linkProperties, string message, string callbackId) {
		_runBlockOnThread(() => {
			_getBranchClass().CallStatic("shareLinkWithLinkProperties", universalObject, linkProperties, message, callbackId);
		});
	}

	#endregion
    
	#region Short URL Generation methods
    
	public static void getShortURLWithBranchUniversalObjectAndCallback(string universalObject, string linkProperties, string callbackId) {
		_runBlockOnThread(() => {
			_getBranchClass().CallStatic("getShortURLWithBranchUniversalObject", universalObject, linkProperties, callbackId);
		});
	}

	#endregion
    
	#region Utility methods
    
	private static AndroidJavaClass _getBranchClass() {
		if (_branchClass == null) {
			_branchClass = new AndroidJavaClass("io/branch/unity/BranchUnityWrapper");
		}

		return _branchClass;
	}
	
	private static void _runBlockOnThread(Action runBlock) {
		var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
		var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

		activity.Call("runOnUiThread", new AndroidJavaRunnable(runBlock));
	}
    
	#endregion
    
    private static AndroidJavaClass _branchClass;
    
#endif
}
