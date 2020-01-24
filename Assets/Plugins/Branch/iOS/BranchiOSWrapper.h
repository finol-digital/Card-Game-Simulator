#import "AppDelegateListener.h"

#pragma mark - Private notification class interface

typedef void (^callbackWithShareCompletion) (NSString *activityType, BOOL completed);

@interface BranchUnityWrapper : NSObject<AppDelegateListener>
@property (strong, nonatomic) NSDictionary *launchOptions;

+ (BranchUnityWrapper *)sharedInstance;
- (BOOL)continueUserActivity:(NSUserActivity *)userActivity;
@end


#pragma mark - Unity plugin methods

extern "C" {
    #pragma mark - Key methods

    void _setBranchKey(char *branchKey);

    #pragma mark - InitSession methods

    void _initSession();
    void _initSessionAsReferrable(BOOL isReferrable);
    void _initSessionWithCallback(char *callbackId);
    void _initSessionAsReferrableWithCallback(BOOL isReferrable, char *callbackId);
    void _initSessionWithUniversalObjectCallback(char *callbackId);

    #pragma mark - Session Item methods

    const char *_getFirstReferringBranchUniversalObject();
    const char *_getFirstReferringBranchLinkProperties();
    const char *_getLatestReferringBranchUniversalObject();
    const char *_getLatestReferringBranchLinkProperties();
    void _resetUserSession();
    void _setIdentity(char *userId);
    void _setIdentityWithCallback(char *userId, char *callbackId);
    void _logout();

    # pragma mark - Configuration methods

    void _setDebug();
    void _setRetryInterval(int retryInterval);
    void _setMaxRetries(int maxRetries);
    void _setNetworkTimeout(int timeout);
    void _registerView(char *universalObjectJson);
    void _listOnSpotlight(char *universalObjectJson);
    void _accountForFacebookSDKPreventingAppLaunch();
    void _setRequestMetadata(char *key, char *value);
    void _setTrackingDisabled(BOOL value);
    void _delayInitToCheckForSearchAds();
    
    #pragma mark - User Action methods

    void _userCompletedAction(char *action);
    void _userCompletedActionWithState(char *action, char *stateDict);

    #pragma mark - Send event methods
    
    void _sendEvent(char *eventJson);
    
    #pragma mark - Credit methods

    void _loadRewardsWithCallback(char *callbackId);
    int _getCredits();
    void _redeemRewards(int count);
    int _getCreditsForBucket(char *bucket);
    void _redeemRewardsForBucket(int count, char *bucket);

    void _getCreditHistoryWithCallback(char *callbackId);
    void _getCreditHistoryForBucketWithCallback(char *bucket, char *callbackId);
    void _getCreditHistoryForTransactionWithLengthOrderAndCallback(char *creditTransactionId, int length, int order, char *callbackId);
    void _getCreditHistoryForBucketWithTransactionLengthOrderAndCallback(char *bucket, char *creditTransactionId, int length, int order, char *callbackId);

    #pragma mark - Short URL Generation methods

    void _getShortURLWithBranchUniversalObjectAndCallback(char *universalObjectJson, char *linkPropertiesJson, char *callbackId);

    #pragma mark - Share Link methods
    
    void _shareLinkWithLinkProperties(char *universalObjectJson, char *linkPropertiesJson, char *message, char *callbackId);
}


