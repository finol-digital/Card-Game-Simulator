using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using System.Runtime.InteropServices;

public class Branch : MonoBehaviour {

	public static string sdkVersion = "0.4.11";

    public delegate void BranchCallbackWithParams(Dictionary<string, object> parameters, string error);
    public delegate void BranchCallbackWithUrl(string url, string error);
    public delegate void BranchCallbackWithStatus(bool changed, string error);
    public delegate void BranchCallbackWithList(List<object> list, string error);
	public delegate void BranchCallbackWithBranchUniversalObject(BranchUniversalObject universalObject, BranchLinkProperties linkProperties, string error);

    #region Public methods

    #region InitSession methods

	/**
	 * 
	 */
	public static void getAutoInstance() {
		_getAutoInstance();
	}

    /**
     * Just initialize session.
     */
    public static void initSession() {
		if (_sessionCounter == 0) {
			++_sessionCounter;
			_isFirstSessionInited = true;

			_initSession ();
		}
    }

	/**
     * Just initialize session, specifying whether is should be referrable.
     */
	public static void  initSession(bool isReferrable) {
		if (_sessionCounter == 0) {
			++_sessionCounter;
			_isFirstSessionInited = true;

			_initSessionAsReferrable (isReferrable);
		}
	}

	/**
	 * Initialize session and receive information about how it opened.
	 */
    public static void initSession(BranchCallbackWithParams callback) {
		if (_sessionCounter == 0) {
			++_sessionCounter;
			_isFirstSessionInited = true;
			autoInitCallbackWithParams = callback;

			var callbackId = _getNextCallbackId ();
			_branchCallbacks [callbackId] = callback;
			_initSessionWithCallback (callbackId);
		}
    }

    /**
	 * Initialize session and receive information about how it opened, specifying whether is should be referrable.
	 */
	public static void initSession(bool isReferrable, BranchCallbackWithParams callback) {
		if (_sessionCounter == 0) {
			++_sessionCounter;
			_isFirstSessionInited = true;
			autoInitCallbackWithParams = callback;

			var callbackId = _getNextCallbackId ();
			_branchCallbacks [callbackId] = callback;
			_initSessionAsReferrableWithCallback (isReferrable, callbackId);
		}
	}

	/**
     * Initialize session and receive information about how it opened.
     */
	public static void initSession(BranchCallbackWithBranchUniversalObject callback) {
		if (_sessionCounter == 0) {
			++_sessionCounter;
			_isFirstSessionInited = true;
			autoInitCallbackWithBUO = callback;

			var callbackId = _getNextCallbackId ();
			_branchCallbacks [callbackId] = callback;
			_initSessionWithUniversalObjectCallback (callbackId);
		}
	}

	/**
     * Close session, necessary for some platforms to track when to cut off a Branch session.
     */
	private static void closeSession() {
		#if UNITY_ANDROID || UNITY_EDITOR
		if (_sessionCounter > 0) {
			_sessionCounter--;
		}
		#endif
	}

    #endregion

    #region Session Item methods

	/**
     * Get the BranchUniversalObject from the initial install.
     */
	public static BranchUniversalObject getFirstReferringBranchUniversalObject() {
		string firstReferringParamsString = "";
		
		#if (UNITY_IOS || UNITY_IPHONE) && !UNITY_EDITOR
		IntPtr ptrResult = _getFirstReferringBranchUniversalObject();
		firstReferringParamsString = Marshal.PtrToStringAnsi(ptrResult);
		#else
		firstReferringParamsString = _getFirstReferringBranchUniversalObject();
		#endif

		BranchUniversalObject resultObject = new BranchUniversalObject(firstReferringParamsString);
		return resultObject;
	}

	/**
     * Get the BranchLinkProperties from the initial install.
     */
	public static BranchLinkProperties getFirstReferringBranchLinkProperties() {
		string firstReferringParamsString = "";
		
		#if (UNITY_IOS || UNITY_IPHONE) && !UNITY_EDITOR
		IntPtr ptrResult = _getFirstReferringBranchLinkProperties();
		firstReferringParamsString = Marshal.PtrToStringAnsi(ptrResult);
		#else
		firstReferringParamsString = _getFirstReferringBranchLinkProperties();
		#endif
		
		BranchLinkProperties resultObject = new BranchLinkProperties(firstReferringParamsString);
		return resultObject;
	}
		
	/**
     * Get the BranchUniversalObject from the last open.
     */
	public static BranchUniversalObject getLatestReferringBranchUniversalObject() {
		string latestReferringParamsString = "";
		
		#if (UNITY_IOS || UNITY_IPHONE) && !UNITY_EDITOR
		IntPtr ptrResult = _getLatestReferringBranchUniversalObject();
		latestReferringParamsString = Marshal.PtrToStringAnsi(ptrResult);
		#else
		latestReferringParamsString = _getLatestReferringBranchUniversalObject();
		#endif
		
		BranchUniversalObject resultObject = new BranchUniversalObject(latestReferringParamsString);
		return resultObject;
	}

	/**
     * Get the BranchLinkProperties from the initial install.
     */
	public static BranchLinkProperties getLatestReferringBranchLinkProperties() {
		string latestReferringParamsString = "";
		
		#if (UNITY_IOS || UNITY_IPHONE) && !UNITY_EDITOR
		IntPtr ptrResult = _getLatestReferringBranchLinkProperties();
		latestReferringParamsString = Marshal.PtrToStringAnsi(ptrResult);
		#else
		latestReferringParamsString = _getLatestReferringBranchLinkProperties();
		#endif
		
		BranchLinkProperties resultObject = new BranchLinkProperties(latestReferringParamsString);
		return resultObject;
	}

    /**
     * Reset the current session
     */
    public static void resetUserSession() {
        _resetUserSession();
    }

    /**
     * Specifiy an identity for the current session
     */
    public static void setIdentity(string userId) {
        _setIdentity(userId);
    }

    /**
     * Specifiy an identity for the current session and receive information about the set.
     */
    public static void setIdentity(string userId, BranchCallbackWithParams callback) {
        var callbackId = _getNextCallbackId();

        _branchCallbacks[callbackId] = callback;

        _setIdentityWithCallback(userId, callbackId);
    }

    /**
     * Clear current session
     */
    public static void logout() {
        _logout();
    }

    #endregion

    #region Configuration methods

    /**
     * Puts Branch into debug mode, causing it to log all requests, and more importantly, not reference the hardware ID of the phone so you can register installs after just uninstalling/reinstalling the app.
     *IMPORTANT! You need to change manifest or to enable Debug mode for Android. Methos setDebug works for iOS only.
     *
     * Make sure to remove setDebug before releasing.
     */
    public static void setDebug() {
		_setDebug();
    }

    /**
     * How many seconds between retries
     */
    public static void setRetryInterval(int retryInterval) {
        _setRetryInterval(retryInterval);
    }

    /**
     * How many retries before giving up
     */
    public static void setMaxRetries(int maxRetries) {
        _setMaxRetries(maxRetries);
    }

    /**
     * How long before deeming a request as timed out
     */
    public static void setNetworkTimeout(int timeout) {
        _setNetworkTimeout(timeout);
    }

	public static void registerView(BranchUniversalObject universalObject) {
		_registerView(universalObject.ToJsonString());
	}

	public static void listOnSpotlight(BranchUniversalObject universalObject) {
		_listOnSpotlight(universalObject.ToJsonString());
	}

	public static void accountForFacebookSDKPreventingAppLaunch() {
		_accountForFacebookSDKPreventingAppLaunch();
	}

	public static void setRequestMetadata(string key, string val) {

		if (!string.IsNullOrEmpty (key) && !string.IsNullOrEmpty (val)) {
			_setRequestMetadata (key, val);
		}
	}

	public static void setTrackingDisabled(bool value) {
		_setTrackingDisabled(value);
	}

    #endregion

    #region User Action methods

    /**
     * Mark a custom action completed
     */
    public static void userCompletedAction(string action) {
        _userCompletedAction(action);
    }

    /**
     * Mark a custom action completed with additional custom fields
     */
    public static void userCompletedAction(string action, Dictionary<string, object> state) {
		_userCompletedActionWithState(action, BranchThirdParty_MiniJSON.Json.Serialize(state));
    }

	#endregion

	#region Send Evene methods

	/**
	 * Send event
	 **/
	public static void sendEvent(BranchEvent branchEvent) {
		_sendEvent(branchEvent.ToJsonString());

	}

    #endregion

    #region Credit methods

    /**
     * Load reward information. Callback indicates whether these values have changed.
     */
    public static void loadRewards(BranchCallbackWithStatus callback) {
        var callbackId = _getNextCallbackId();

        _branchCallbacks[callbackId] = callback;

        _loadRewardsWithCallback(callbackId);
    }

    /**
     * Get total credit count
     */
    public static int getCredits() {
        return _getCredits();
    }

    /**
     * Get credit count for a specified bucket
     */
    public static int getCredits(string bucket) {
        return _getCreditsForBucket(bucket);
    }

    /**
     * Redeem reward for a specified amount of credits
     */
    public static void redeemRewards(int count) {
        _redeemRewards(count);
    }

    /**
     * Redeem reward for a specified amount of credits and a certain bucket
     */
    public static void redeemRewards(int count, string bucket) {
        _redeemRewardsForBucket(count, bucket);
    }

    /**
     * Get Credit Transaction History items in a list
     */
    public static void getCreditHistory(BranchCallbackWithList callback) {
        var callbackId = _getNextCallbackId();

        _branchCallbacks[callbackId] = callback;

        _getCreditHistoryWithCallback(callbackId);
    }

    /**
     * Get Credit Transaction History items in a list for a specified bucket
     */
    public static void getCreditHistory(string bucket, BranchCallbackWithList callback) {
        var callbackId = _getNextCallbackId();

        _branchCallbacks[callbackId] = callback;

        _getCreditHistoryForBucketWithCallback(bucket, callbackId);
    }

    /**
     * Get Credit Transaction History items in a list starting at a specified transaction id, and continuing for a specified number of items, either descending or ascending (0, 1)
     */
    public static void getCreditHistory(string creditTransactionId, int length, int order, BranchCallbackWithList callback) {
        var callbackId = _getNextCallbackId();

        _branchCallbacks[callbackId] = callback;

        _getCreditHistoryForTransactionWithLengthOrderAndCallback(creditTransactionId, length, order, callbackId);
    }


    /**
     * Get Credit Transaction History items in a list for a specified bucket starting at a specified transaction id, and continuing for a specified number of items, either descending or ascending (0, 1)
     */
    public static void getCreditHistory(string bucket, string creditTransactionId, int length, int order, BranchCallbackWithList callback) {
        var callbackId = _getNextCallbackId();
        
        _branchCallbacks[callbackId] = callback;
        
        _getCreditHistoryForBucketWithTransactionLengthOrderAndCallback(bucket, creditTransactionId, length, order, callbackId);
    }

    #endregion

	#region Share Link methods

	public static void shareLink(BranchUniversalObject universalObject, BranchLinkProperties linkProperties, string message, BranchCallbackWithParams callback) {
		var callbackId = _getNextCallbackId();
		
		_branchCallbacks[callbackId] = callback;
		
		_shareLinkWithLinkProperties(universalObject.ToJsonString(), linkProperties.ToJsonString(), message, callbackId);
	}

	#endregion
	
	#region Short URL Generation methods

	/**
     * Get a short url given a BranchUniversalObject, BranchLinkProperties
     */
	public static void getShortURL(BranchUniversalObject universalObject, BranchLinkProperties linkProperties, BranchCallbackWithUrl callback) {
		var callbackId = _getNextCallbackId();
		
		_branchCallbacks[callbackId] = callback;
		
		_getShortURLWithBranchUniversalObjectAndCallback(universalObject.ToJsonString(), linkProperties.ToJsonString(), callbackId);
	}

    #endregion

    #endregion

	#region Singleton

    public void Awake() {
		var olderBranch = FindObjectOfType<Branch>();

		if (olderBranch != null && olderBranch != this) {
			// someone's already here!
			Destroy(gameObject);
			return;
		}

        name = "Branch";
        DontDestroyOnLoad(gameObject);

		if (BranchData.Instance.testMode) {
        	_setBranchKey(BranchData.Instance.testBranchKey);
		}
		else {
			_setBranchKey(BranchData.Instance.liveBranchKey);
		}
    }

	void OnApplicationPause(bool pauseStatus) {
		if (!_isFirstSessionInited)
			return;

		if (!pauseStatus) {
			if (autoInitCallbackWithParams != null) {
				initSession(autoInitCallbackWithParams);
			}
			else if (autoInitCallbackWithBUO != null) {
				initSession(autoInitCallbackWithBUO);
			}
			else {
				initSession();
			}
		}
		else {
			closeSession();
		}
	}

	#endregion

	#region Private methods

	#region Platform Loading Methods

#if (UNITY_IOS || UNITY_IPHONE) && !UNITY_EDITOR
    
    [DllImport ("__Internal")]
    private static extern void _setBranchKey(string branchKey);
    
	private static void _getAutoInstance() {}

    [DllImport ("__Internal")]
    private static extern void _initSession();

	[DllImport ("__Internal")]
	private static extern void _initSessionAsReferrable(bool isReferrable);

	[DllImport ("__Internal")]
	private static extern void _initSessionWithCallback(string callbackId);

	[DllImport ("__Internal")]
	private static extern void _initSessionAsReferrableWithCallback(bool isReferrable, string callbackId);

	[DllImport ("__Internal")]
	private static extern void _initSessionWithUniversalObjectCallback(string callbackId);
        
	[DllImport ("__Internal")]
	private static extern IntPtr _getFirstReferringBranchUniversalObject();

	[DllImport ("__Internal")]
	private static extern IntPtr _getFirstReferringBranchLinkProperties();

	[DllImport ("__Internal")]
	private static extern IntPtr _getLatestReferringBranchUniversalObject();
    
	[DllImport ("__Internal")]
	private static extern IntPtr _getLatestReferringBranchLinkProperties();

    [DllImport ("__Internal")]
    private static extern void _resetUserSession();
    
    [DllImport ("__Internal")]
    private static extern void _setIdentity(string userId);
    
    [DllImport ("__Internal")]
    private static extern void _setIdentityWithCallback(string userId, string callbackId);
    
    [DllImport ("__Internal")]
    private static extern void _logout();
    
    [DllImport ("__Internal")]
    private static extern void _setDebug();

    [DllImport ("__Internal")]
    private static extern void _setRetryInterval(int retryInterval);
    
    [DllImport ("__Internal")]
    private static extern void _setMaxRetries(int maxRetries);
    
    [DllImport ("__Internal")]
    private static extern void _setNetworkTimeout(int timeout);
    
	[DllImport ("__Internal")]
	private static extern void _registerView(string universalObject);

	[DllImport ("__Internal")]
	private static extern void _listOnSpotlight(string universalObject);

	[DllImport ("__Internal")]
	private static extern void _accountForFacebookSDKPreventingAppLaunch();

	[DllImport ("__Internal")]
	private static extern void _setRequestMetadata(string key, string val);

	[DllImport ("__Internal")]
	private static extern void _setTrackingDisabled(bool value);

    [DllImport ("__Internal")]
    private static extern void _userCompletedAction(string action);
    
    [DllImport ("__Internal")]
    private static extern void _userCompletedActionWithState(string action, string stateDict);
    
	[DllImport ("__Internal")]
	private static extern void _sendEvent(string eventName);

    [DllImport ("__Internal")]
    private static extern void _loadRewardsWithCallback(string callbackId);
    
    [DllImport ("__Internal")]
    private static extern int _getCredits();
    
    [DllImport ("__Internal")]
    private static extern void _redeemRewards(int count);
    
    [DllImport ("__Internal")]
    private static extern int _getCreditsForBucket(string bucket);
    
    [DllImport ("__Internal")]
    private static extern void _redeemRewardsForBucket(int count, string bucket);
    
    [DllImport ("__Internal")]
    private static extern void _getCreditHistoryWithCallback(string callbackId);
    
    [DllImport ("__Internal")]
    private static extern void _getCreditHistoryForBucketWithCallback(string bucket, string callbackId);
    
    [DllImport ("__Internal")]
    private static extern void _getCreditHistoryForTransactionWithLengthOrderAndCallback(string creditTransactionId, int length, int order, string callbackId);
    
    [DllImport ("__Internal")]
    private static extern void _getCreditHistoryForBucketWithTransactionLengthOrderAndCallback(string bucket, string creditTransactionId, int length, int order, string callbackId);
    
	[DllImport ("__Internal")]
	private static extern void _getShortURLWithBranchUniversalObjectAndCallback(string universalObject, string linkProperties, string callbackId);

	[DllImport ("__Internal")]
	private static extern void _shareLinkWithLinkProperties(string universalObject, string linkProperties, string message, string callbackId);
	    
#elif UNITY_ANDROID && !UNITY_EDITOR

    private static void _setBranchKey(string branchKey) {
        BranchAndroidWrapper.setBranchKey(branchKey);
    }

	private static void _getAutoInstance() {
		BranchAndroidWrapper.getAutoInstance();
	}

    private static void _initSession() {
        BranchAndroidWrapper.initSession();
    }

	private static void _initSessionAsReferrable(bool isReferrable) {
		BranchAndroidWrapper.initSessionAsReferrable(isReferrable);
	}

	private static void _initSessionWithCallback(string callbackId) {
		BranchAndroidWrapper.initSessionWithCallback(callbackId);
	}

	private static void _initSessionAsReferrableWithCallback(bool isReferrable, string callbackId) {
		BranchAndroidWrapper.initSessionAsReferrableWithCallback(isReferrable, callbackId);
	}

	private static void _initSessionWithUniversalObjectCallback(string callbackId) {
		BranchAndroidWrapper.initSessionWithUniversalObjectCallback(callbackId);
	}

	private static string _getFirstReferringBranchUniversalObject() {
		return BranchAndroidWrapper.getFirstReferringBranchUniversalObject();
	}

	private static string _getFirstReferringBranchLinkProperties() {
		return BranchAndroidWrapper.getFirstReferringBranchLinkProperties();
	}

	private static string _getLatestReferringBranchUniversalObject() {
		return BranchAndroidWrapper.getLatestReferringBranchUniversalObject();
	}

	private static string _getLatestReferringBranchLinkProperties() {
		return BranchAndroidWrapper.getLatestReferringBranchLinkProperties();
	}

    private static void _resetUserSession() {
        BranchAndroidWrapper.resetUserSession();
    }
    
    private static void _setIdentity(string userId) {
        BranchAndroidWrapper.setIdentity(userId);
    }
    
    private static void _setIdentityWithCallback(string userId, string callbackId) {
        BranchAndroidWrapper.setIdentityWithCallback(userId, callbackId);
    }
    
    private static void _logout() {
        BranchAndroidWrapper.logout();
    }

    private static void _setDebug() {
        BranchAndroidWrapper.setDebug();
    }
    
    private static void _setRetryInterval(int retryInterval) {
        BranchAndroidWrapper.setRetryInterval(retryInterval);
    }
    
    private static void _setMaxRetries(int maxRetries) {
        BranchAndroidWrapper.setMaxRetries(maxRetries);
    }
    
    private static void _setNetworkTimeout(int timeout) {
        BranchAndroidWrapper.setNetworkTimeout(timeout);
    }
    
	private static void _registerView(string universalObject) {
		BranchAndroidWrapper.registerView(universalObject);
	}

	private static void _listOnSpotlight(string universalObject) {
		BranchAndroidWrapper.listOnSpotlight(universalObject);
	}

	private static void _accountForFacebookSDKPreventingAppLaunch() {
		BranchAndroidWrapper.accountForFacebookSDKPreventingAppLaunch();
	}

	private static void _setRequestMetadata(string key, string val) {
		BranchAndroidWrapper.setRequestMetadata(key, val);
	}

	private static void _setTrackingDisabled(bool value) {
	    BranchAndroidWrapper.setTrackingDisabled(value);
    }

    private static void _userCompletedAction(string action) {
        BranchAndroidWrapper.userCompletedAction(action);
    }
    
    private static void _userCompletedActionWithState(string action, string stateDict) {
        BranchAndroidWrapper.userCompletedActionWithState(action, stateDict);
    }
    
	private static void _sendEvent(string eventName) {
		BranchAndroidWrapper.sendEvent(eventName);
	}

    private static void _loadRewardsWithCallback(string callbackId) {
        BranchAndroidWrapper.loadRewardsWithCallback(callbackId);
    }
    
    private static int _getCredits() {
        return BranchAndroidWrapper.getCredits();
    }

    private static void _redeemRewards(int count) {
        BranchAndroidWrapper.redeemRewards(count);
    }
    
    private static int _getCreditsForBucket(string bucket) {
        return BranchAndroidWrapper.getCreditsForBucket(bucket);
    }

    private static void _redeemRewardsForBucket(int count, string bucket) {
        BranchAndroidWrapper.redeemRewardsForBucket(count, bucket);
    }

    private static void _getCreditHistoryWithCallback(string callbackId) {
        BranchAndroidWrapper.getCreditHistoryWithCallback(callbackId);
    }
    
    private static void _getCreditHistoryForBucketWithCallback(string bucket, string callbackId) {
        BranchAndroidWrapper.getCreditHistoryForBucketWithCallback(bucket, callbackId);
    }

    private static void _getCreditHistoryForTransactionWithLengthOrderAndCallback(string creditTransactionId, int length, int order, string callbackId) {
        BranchAndroidWrapper.getCreditHistoryForTransactionWithLengthOrderAndCallback(creditTransactionId, length, order, callbackId);
    }
    
    private static void _getCreditHistoryForBucketWithTransactionLengthOrderAndCallback(string bucket, string creditTransactionId, int length, int order, string callbackId) {
        BranchAndroidWrapper.getCreditHistoryForBucketWithTransactionLengthOrderAndCallback(bucket, creditTransactionId, length, order, callbackId);
    }

	private static void _shareLinkWithLinkProperties(string universalObject, string linkProperties, string message, string callbackId) {
		BranchAndroidWrapper.shareLinkWithLinkProperties(universalObject, linkProperties, message, callbackId);
	}
	    
	private static void _getShortURLWithBranchUniversalObjectAndCallback(string universalObject, string linkProperties, string callbackId) {
		BranchAndroidWrapper.getShortURLWithBranchUniversalObjectAndCallback(universalObject, linkProperties, callbackId);
	}

#else

	private static void _setBranchKey(string branchKey) { }
    
	private static void _getAutoInstance() { }

    private static void _initSession() {
        Debug.Log("Branch is not implemented on this platform");
    }
    
	private static void _initSessionAsReferrable(bool isReferrable) {
		Debug.Log("Branch is not implemented on this platform");
	}

	private static void _initSessionWithCallback(string callbackId) {
		callNotImplementedCallbackForParamCallback(callbackId);
	}

	private static void _initSessionAsReferrableWithCallback(bool isReferrable, string callbackId) {
		callNotImplementedCallbackForParamCallback(callbackId);
	}

	private static void _initSessionWithUniversalObjectCallback(string callbackId) {
		callNotImplementedCallbackForBUOCallback(callbackId);
	}

	private static string _getFirstReferringBranchUniversalObject() {
		return "{}";
	}

	private static string _getFirstReferringBranchLinkProperties() {
		return "{}";
	}

	private static string _getLatestReferringBranchUniversalObject() {
		return "{}";
	}

	private static string _getLatestReferringBranchLinkProperties() {
		return "{}";
	}
    
    private static void _resetUserSession() { }
    
    private static void _setIdentity(string userId) { }
    
    private static void _setIdentityWithCallback(string userId, string callbackId) {
        callNotImplementedCallbackForParamCallback(callbackId);
    }
    
    private static void _logout() { }

	private static void _setDebug() { }
    
    private static void _setRetryInterval(int retryInterval) { }
    
    private static void _setMaxRetries(int maxRetries) { }
    
    private static void _setNetworkTimeout(int timeout) { }

	private static void _registerView(string universalObject) { }

	private static void _listOnSpotlight(string universalObject) { }

	private static void _accountForFacebookSDKPreventingAppLaunch() { }

	private static void _setRequestMetadata(string key, string val) { }

	private static void _setTrackingDisabled(bool value) { }
    
    private static void _userCompletedAction(string action) { }
    
    private static void _userCompletedActionWithState(string action, string stateDict) { }

	private static void _sendEvent(string eventName) { }
    
    private static void _loadRewardsWithCallback(string callbackId) {
        callNotImplementedCallbackForStatusCallback(callbackId);
    }
    
    private static int _getCredits() {
        return 0;
    }
    
    private static void _redeemRewards(int count) { }
    
    private static int _getCreditsForBucket(string bucket) {
        return 0;
    }
    
    private static void _redeemRewardsForBucket(int count, string bucket) { }
    
    private static void _getCreditHistoryWithCallback(string callbackId) {
        callNotImplementedCallbackForListCallback(callbackId);
    }
    
    private static void _getCreditHistoryForBucketWithCallback(string bucket, string callbackId) {
        callNotImplementedCallbackForListCallback(callbackId);
    }
    
    private static void _getCreditHistoryForTransactionWithLengthOrderAndCallback(string creditTransactionId, int length, int order, string callbackId) {
        callNotImplementedCallbackForListCallback(callbackId);
    }
    
    private static void _getCreditHistoryForBucketWithTransactionLengthOrderAndCallback(string bucket, string creditTransactionId, int length, int order, string callbackId) {
        callNotImplementedCallbackForListCallback(callbackId);
    }
    
	private static void _shareLinkWithLinkProperties(string universalObject, string linkProperties, string message,string callbackId) {
		callNotImplementedCallbackForUrlCallback(callbackId);
	}

	private static void _getShortURLWithBranchUniversalObjectAndCallback(string universalObject, string linkProperties, string callbackId) {
		callNotImplementedCallbackForUrlCallback(callbackId);
	}
		
    
    private static void callNotImplementedCallbackForParamCallback(string callbackId) {
        var callback = _branchCallbacks[callbackId] as BranchCallbackWithParams;
        callback(null, "Not implemented on this platform");
    }
    
    private static void callNotImplementedCallbackForUrlCallback(string callbackId) {
        var callback = _branchCallbacks[callbackId] as BranchCallbackWithUrl;
        callback(null, "Not implemented on this platform");
    }
    
    private static void callNotImplementedCallbackForListCallback(string callbackId) {
        var callback = _branchCallbacks[callbackId] as BranchCallbackWithList;
        callback(null, "Not implemented on this platform");
    }
    
    private static void callNotImplementedCallbackForStatusCallback(string callbackId) {
        var callback = _branchCallbacks[callbackId] as BranchCallbackWithStatus;
        callback(false, "Not implemented on this platform");
    }

	private static void callNotImplementedCallbackForBUOCallback(string callbackId) {
		var callback = _branchCallbacks[callbackId] as BranchCallbackWithBranchUniversalObject;
		callback(null, null, "Not implemented on this platform");
	}

    #endif
    
    #endregion

    #region Callback management

    public void _asyncCallbackWithParams(string callbackDictString) {
		var callbackDict = BranchThirdParty_MiniJSON.Json.Deserialize(callbackDictString) as Dictionary<string, object>;
        var callbackId = callbackDict["callbackId"] as string;
        Dictionary<string, object> parameters = callbackDict.ContainsKey("params") ? callbackDict["params"] as Dictionary<string, object> : null;
        string error = callbackDict.ContainsKey("error") ? callbackDict["error"] as string : null;
        
		var callback = _branchCallbacks[callbackId] as BranchCallbackWithParams;
		if (callback != null) {
			callback(parameters, error);
		}
    }

    public void _asyncCallbackWithStatus(string callbackDictString) {
		var callbackDict = BranchThirdParty_MiniJSON.Json.Deserialize(callbackDictString) as Dictionary<string, object>;
        var callbackId = callbackDict["callbackId"] as string;
        bool status = callbackDict.ContainsKey("status") ? (callbackDict["status"] as bool?).Value : false;
        string error = callbackDict.ContainsKey("error") ? callbackDict["error"] as string : null;

        var callback = _branchCallbacks[callbackId] as BranchCallbackWithStatus;
		if (callback != null) {
        	callback(status, error);
		}
    }

    public void _asyncCallbackWithList(string callbackDictString) {
		var callbackDict = BranchThirdParty_MiniJSON.Json.Deserialize(callbackDictString) as Dictionary<string, object>;
        var callbackId = callbackDict["callbackId"] as string;
		List<object> list = callbackDict.ContainsKey("list") ? callbackDict["list"] as List<object> : null;
        string error = callbackDict.ContainsKey("error") ? callbackDict["error"] as string : null;

        var callback = _branchCallbacks[callbackId] as BranchCallbackWithList;
		if (callback != null) {
        	callback(list, error);
		}
    }

    public void _asyncCallbackWithUrl(string callbackDictString) {
		var callbackDict = BranchThirdParty_MiniJSON.Json.Deserialize(callbackDictString) as Dictionary<string, object>;
        var callbackId = callbackDict["callbackId"] as string;
        string url = callbackDict.ContainsKey("url") ? callbackDict["url"] as string : null;
        string error = callbackDict.ContainsKey("error") ? callbackDict["error"] as string : null;

        var callback = _branchCallbacks[callbackId] as BranchCallbackWithUrl;
		if (callback != null) {
        	callback(url, error);
		}
    }

	public void _asyncCallbackWithBranchUniversalObject(string callbackDictString) {

		Debug.Log ("callbackDictString: \n\n" + callbackDictString + "\n\n");

		var callbackDict = BranchThirdParty_MiniJSON.Json.Deserialize(callbackDictString) as Dictionary<string, object>;
		var callbackId = callbackDict["callbackId"] as string;
		var paramsDict = callbackDict.ContainsKey("params") ? callbackDict["params"] as Dictionary<string, object> : null;
		var universalObject = paramsDict != null && paramsDict.ContainsKey("universalObject") ? paramsDict["universalObject"] as Dictionary<string, object> : null;
		var linkProperties = paramsDict != null && paramsDict.ContainsKey("linkProperties") ? paramsDict["linkProperties"] as Dictionary<string, object> : null;
		string error = callbackDict.ContainsKey("error") ? callbackDict["error"] as string : null;

		var callback = _branchCallbacks[callbackId] as BranchCallbackWithBranchUniversalObject;
		if (callback != null) {
			callback(new BranchUniversalObject(universalObject), new BranchLinkProperties(linkProperties), error);
		}
	}

	public void _DebugLog(string val) {
		Debug.Log(val);
	}

    private static string _getNextCallbackId() {
        return "BranchCallbackId" + (++_nextCallbackId);
    }

    #endregion

    #endregion

    private static int _nextCallbackId = 0;
    private static Dictionary<string, object> _branchCallbacks = new Dictionary<string, object>();

	private static int _sessionCounter = 0;
	private static bool _isFirstSessionInited = false;
	private static BranchCallbackWithParams autoInitCallbackWithParams = null;
	private static BranchCallbackWithBranchUniversalObject autoInitCallbackWithBUO = null;
}
