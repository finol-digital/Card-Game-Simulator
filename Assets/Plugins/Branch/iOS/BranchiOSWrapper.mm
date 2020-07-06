#include "BranchiOSWrapper.h"
#include "Branch.h"
#include "BranchConstants.h"
#include "BranchUniversalObject.h"
#import "UnityAppController.h"


static NSString *_branchKey = @"";
static BranchUnityWrapper *_wrapper = [BranchUnityWrapper sharedInstance];


#pragma mark - Private notification class implementation

@implementation BranchUnityWrapper

+ (BranchUnityWrapper *)sharedInstance
{
    return _wrapper;
}

+ (void)initialize {
    if(!_wrapper) {
        _wrapper = [[BranchUnityWrapper alloc] init];
    }
}

- (id)init {
    if (self = [super init]) {
        UnityRegisterAppDelegateListener(self);
    }
    
    return self;
}

- (void)dealloc {
    [[NSNotificationCenter defaultCenter] removeObserver:self];
}

- (void)didFinishLaunching:(NSNotification *)notification {
    self.launchOptions = notification.userInfo;
}

- (void)onOpenURL:(NSNotification *)notification {
    NSURL *openURL = notification.userInfo[@"url"];
    [[Branch getInstance:_branchKey] handleDeepLink:openURL];
}

- (BOOL)continueUserActivity:(NSUserActivity *)userActivity {
    return [[Branch getInstance:_branchKey] continueUserActivity:userActivity];
}

@end


#pragma mark - Converter methods

static NSString *CreateNSString(const char *string) {
    if (string == NULL) {
        return nil;
    }

    return [NSString stringWithUTF8String:string];
}

static NSURL *CreateNSUrl(const char *string) {
    return [NSURL URLWithString:CreateNSString(string)];
}

static NSDate *CreateNSDate(char *strDate) {
    NSString *str = CreateNSString(strDate);
    NSDateFormatter *formatter= [[NSDateFormatter alloc] init];
    formatter.dateFormat = @"yyyy-MM-ddTHH:mm:ssZ";

    return [formatter dateFromString:str];
}

static NSDate *CreateNSDate(NSString *strDate) {
    NSDateFormatter *formatter= [[NSDateFormatter alloc] init];
    formatter.dateFormat = @"yyyy-MM-ddTHH:mm:ssZ";
    
    return [formatter dateFromString:strDate];
}

static NSString *CreateNSStringFromNSDate(NSDate *date) {
    NSDateFormatter *formatter= [[NSDateFormatter alloc] init];
    formatter.dateFormat = @"yyyy-MM-ddTHH:mm:ssZ";
    
    return [formatter stringFromDate:date];
}

static NSDictionary *dictionaryFromJsonString(const char *jsonString) {
    NSData *jsonData = [[NSData alloc] initWithBytes:jsonString length:strlen(jsonString)];
    NSDictionary *dictionary = [NSJSONSerialization JSONObjectWithData:jsonData options:kNilOptions error:nil];
    
    return dictionary;
}

static NSArray *arrayFromJsonString(const char *jsonString) {
    NSData *jsonData = [[NSData alloc] initWithBytes:jsonString length:strlen(jsonString)];
    NSArray *array = [NSJSONSerialization JSONObjectWithData:jsonData options:kNilOptions error:nil];
    
    return array;
}

static const char *jsonCStringFromDictionary(NSDictionary *dictionary) {
    NSData *jsonData = [NSJSONSerialization dataWithJSONObject:dictionary options:kNilOptions error:nil];
    NSString *jsonString = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];

    return [jsonString cStringUsingEncoding:NSUTF8StringEncoding];
}

static NSDictionary *dictFromBranchUniversalObject(BranchUniversalObject *universalObject) {
    NSDictionary *universalObjectDict = [NSDictionary dictionary];
    
    if (universalObject) {
        universalObjectDict = @{
            BRANCH_LINK_DATA_KEY_CANONICAL_IDENTIFIER: universalObject.canonicalIdentifier ? universalObject.canonicalIdentifier : @"",
            BRANCH_LINK_DATA_KEY_CANONICAL_URL: universalObject.canonicalUrl ? universalObject.canonicalUrl : @"",
            BRANCH_LINK_DATA_KEY_OG_TITLE: universalObject.title ? universalObject.title : @"",
            BRANCH_LINK_DATA_KEY_OG_DESCRIPTION: universalObject.contentDescription ? universalObject.contentDescription : @"",
            BRANCH_LINK_DATA_KEY_OG_IMAGE_URL: universalObject.imageUrl ? universalObject.imageUrl : @"",
            BRANCH_LINK_DATA_KEY_PUBLICLY_INDEXABLE: universalObject.publiclyIndex ? [[NSNumber numberWithInteger:universalObject.publiclyIndex] stringValue]: @"",
            BRANCH_LINK_DATA_KEY_LOCALLY_INDEXABLE: universalObject.locallyIndex ? [[NSNumber numberWithInteger:universalObject.locallyIndex] stringValue]: @"",
            BRANCH_LINK_DATA_KEY_KEYWORDS: universalObject.keywords ? universalObject.keywords : @"",
            BRANCH_LINK_DATA_KEY_CONTENT_EXPIRATION_DATE: universalObject.expirationDate ? @(1000 * [universalObject.expirationDate timeIntervalSince1970]) : @"",
            @"metadata": universalObject.contentMetadata ? universalObject.contentMetadata.dictionary : @"",
        };
    }

    return universalObjectDict;
}

static NSDictionary *dictFromBranchLinkProperties(BranchLinkProperties *linkProperties) {
    NSDictionary *linkPropertiesDict = [NSDictionary dictionary];
    
    if (linkProperties) {
        linkPropertiesDict = @{
            @"~tags": linkProperties.tags ? linkProperties.tags : @"",
            @"~feature": linkProperties.feature ? linkProperties.feature : @"",
            @"~alias": linkProperties.alias ? linkProperties.alias : @"",
            @"~channel": linkProperties.channel ? linkProperties.channel : @"",
            @"~stage": linkProperties.stage ? linkProperties.stage : @"",
            @"~duration": linkProperties.matchDuration ? [[NSNumber numberWithInteger:linkProperties.matchDuration] stringValue] : @"",
            @"control_params": linkProperties.controlParams ? linkProperties.controlParams : @""
        };
    }
    
    return linkPropertiesDict;
}

static BranchUniversalObject* branchuniversalObjectFormDict(NSDictionary *universalObjectDict) {
    BranchUniversalObject *universalObject = [[BranchUniversalObject alloc] init];
    
    if (universalObjectDict[BRANCH_LINK_DATA_KEY_CANONICAL_IDENTIFIER]) {
        universalObject.canonicalIdentifier = universalObjectDict[BRANCH_LINK_DATA_KEY_CANONICAL_IDENTIFIER];
    }
    if (universalObjectDict[BRANCH_LINK_DATA_KEY_CANONICAL_URL]) {
        universalObject.canonicalUrl = universalObjectDict[BRANCH_LINK_DATA_KEY_CANONICAL_URL];
    }
    if (universalObjectDict[BRANCH_LINK_DATA_KEY_OG_TITLE]) {
        universalObject.title = universalObjectDict[BRANCH_LINK_DATA_KEY_OG_TITLE];
    }
    if (universalObjectDict[BRANCH_LINK_DATA_KEY_OG_DESCRIPTION]) {
        universalObject.contentDescription = universalObjectDict[BRANCH_LINK_DATA_KEY_OG_DESCRIPTION];
    }
    if (universalObjectDict[BRANCH_LINK_DATA_KEY_OG_IMAGE_URL]) {
        universalObject.imageUrl = universalObjectDict[BRANCH_LINK_DATA_KEY_OG_IMAGE_URL];
    }
    
    if (universalObjectDict[BRANCH_LINK_DATA_KEY_PUBLICLY_INDEXABLE]) {
        if (universalObjectDict[BRANCH_LINK_DATA_KEY_PUBLICLY_INDEXABLE] == 0) {
            universalObject.publiclyIndex = BranchContentIndexModePublic;
        }
        else {
            universalObject.publiclyIndex = BranchContentIndexModePrivate;
        }
    }
    
    if (universalObjectDict[BRANCH_LINK_DATA_KEY_LOCALLY_INDEXABLE]) {
        if (universalObjectDict[BRANCH_LINK_DATA_KEY_LOCALLY_INDEXABLE] == 0) {
            universalObject.locallyIndex = BranchContentIndexModePublic;
        }
        else {
            universalObject.locallyIndex = BranchContentIndexModePrivate;
        }
    }
    
    if (universalObjectDict[BRANCH_LINK_DATA_KEY_CONTENT_EXPIRATION_DATE]) {
        universalObject.expirationDate = [NSDate dateWithTimeIntervalSince1970:[universalObjectDict[BRANCH_LINK_DATA_KEY_CONTENT_EXPIRATION_DATE] integerValue]/1000];
    }
    
    if (universalObjectDict[BRANCH_LINK_DATA_KEY_KEYWORDS]) {
        universalObject.keywords = [universalObjectDict[BRANCH_LINK_DATA_KEY_KEYWORDS] copy];
    }
    
    if (universalObjectDict[@"metadata"]) {
        
        NSDictionary *dict = dictionaryFromJsonString([universalObjectDict[@"metadata"] cStringUsingEncoding:NSUTF8StringEncoding]);
        universalObject.contentMetadata = [BranchContentMetadata contentMetadataWithDictionary:dict];
        
        NSMutableDictionary *mutableDict = [dict mutableCopy];
        [mutableDict removeObjectForKey:@"$content_schema"];
        [mutableDict removeObjectForKey:@"$quantity"];
        [mutableDict removeObjectForKey:@"$price"];
        [mutableDict removeObjectForKey:@"$currency"];
        [mutableDict removeObjectForKey:@"$sku"];
        [mutableDict removeObjectForKey:@"$product_name"];
        [mutableDict removeObjectForKey:@"$product_brand"];
        [mutableDict removeObjectForKey:@"$product_category"];
        [mutableDict removeObjectForKey:@"$product_variant"];
        [mutableDict removeObjectForKey:@"$condition"];
        [mutableDict removeObjectForKey:@"$rating_average"];
        [mutableDict removeObjectForKey:@"$rating_count"];
        [mutableDict removeObjectForKey:@"$rating_max"];
        [mutableDict removeObjectForKey:@"$address_street"];
        [mutableDict removeObjectForKey:@"$address_city"];
        [mutableDict removeObjectForKey:@"$address_region"];
        [mutableDict removeObjectForKey:@"$address_country"];
        [mutableDict removeObjectForKey:@"$address_postal_code"];
        [mutableDict removeObjectForKey:@"$latitude"];
        [mutableDict removeObjectForKey:@"$longitude"];
        [mutableDict removeObjectForKey:@"$image_captions"];

        for (NSString *key in mutableDict.keyEnumerator) {
            NSString *value = mutableDict[key];
            universalObject.contentMetadata.customMetadata[key] = value;
        }
    }
    
    return universalObject;
}

static BranchLinkProperties *branchLinkPropertiesFormDict(NSDictionary *linkPropertiesDict) {
    BranchLinkProperties *linkProperties = [[BranchLinkProperties alloc] init];
    
    if (linkPropertiesDict[@"~tags"]) {
        linkProperties.tags = linkPropertiesDict[@"~tags"];
    }
    if (linkPropertiesDict[@"~feature"]) {
        linkProperties.feature = linkPropertiesDict[@"~feature"];
    }
    if (linkPropertiesDict[@"~alias"]) {
        linkProperties.alias = linkPropertiesDict[@"~alias"];
    }
    if (linkPropertiesDict[@"~channel"]) {
        linkProperties.channel = linkPropertiesDict[@"~channel"];
    }
    if (linkPropertiesDict[@"~stage"]) {
        linkProperties.stage = linkPropertiesDict[@"~stage"];
    }
    if (linkPropertiesDict[@"~duration"]) {
        linkProperties.matchDuration = [linkPropertiesDict[@"~duration"] intValue];
    }
    if (linkPropertiesDict[@"control_params"]) {
        linkProperties.controlParams = [linkPropertiesDict[@"control_params"] copy];
    }
    
    return linkProperties;
}

#pragma mark - Callbacks

static callbackWithParams callbackWithParamsForCallbackId(char *callbackId) {
    NSString *callbackString = CreateNSString(callbackId);

    return ^(NSDictionary *params, NSError *error) {
        id errorDictItem = error ? [error description] : [NSNull null];
        id paramsDictItem = params ?: [NSNull null];
        NSDictionary *callbackDict = @{ @"callbackId": callbackString, @"params": paramsDictItem, @"error": errorDictItem };
        
        UnitySendMessage("Branch", "_asyncCallbackWithParams", jsonCStringFromDictionary(callbackDict));
    };
}

static callbackWithStatus callbackWithStatusForCallbackId(char *callbackId) {
    NSString *callbackString = CreateNSString(callbackId);

    return ^(BOOL status, NSError *error) {
        id errorDictItem = error ? [error description] : [NSNull null];
        NSDictionary *callbackDict = @{ @"callbackId": callbackString, @"status": @(status), @"error": errorDictItem };
        
        UnitySendMessage("Branch", "_asyncCallbackWithStatus", jsonCStringFromDictionary(callbackDict));
    };
}

static callbackWithList callbackWithListForCallbackId(char *callbackId) {
    NSString *callbackString = CreateNSString(callbackId);

    return ^(NSArray *list, NSError *error) {
        id errorDictItem = error ? [error description] : [NSNull null];
        NSDictionary *callbackDict = @{ @"callbackId": callbackString, @"list": list, @"error": errorDictItem };
        
        UnitySendMessage("Branch", "_asyncCallbackWithList", jsonCStringFromDictionary(callbackDict));
    };
}

static callbackWithUrl callbackWithUrlForCallbackId(char *callbackId) {
    NSString *callbackString = CreateNSString(callbackId);

    return ^(NSString *url, NSError *error) {
        id errorDictItem = error ? [error description] : [NSNull null];
        NSDictionary *callbackDict = @{ @"callbackId": callbackString, @"url": url, @"error": errorDictItem };
        
        UnitySendMessage("Branch", "_asyncCallbackWithUrl", jsonCStringFromDictionary(callbackDict));
    };
}

static callbackWithBranchUniversalObject callbackWithBranchUniversalObjectForCallbackId(char *callbackId) {
    NSString *callbackString = CreateNSString(callbackId);
    
    return ^(BranchUniversalObject *universalObject, BranchLinkProperties *linkProperties, NSError *error) {
        id errorDictItem = error ? [error description] : [NSNull null];
        
        NSDictionary *params = @{@"universalObject": dictFromBranchUniversalObject(universalObject), @"linkProperties": dictFromBranchLinkProperties(linkProperties)};
        NSDictionary *callbackDict = @{ @"callbackId": callbackString, @"params": params, @"error": errorDictItem };
        
        UnitySendMessage("Branch", "_asyncCallbackWithBranchUniversalObject", jsonCStringFromDictionary(callbackDict));
    };
}

static callbackWithShareCompletion callbackWithShareCompletionForCallbackId(char *callbackId) {
    NSString *callbackString = CreateNSString(callbackId);
    
    return ^(NSString *activityType, BOOL completed) {
        id errorDictItem = [NSNull null];
        
        NSDictionary *params;
        if( activityType != nil) {
            params = @{@"sharedLink": @"", @"sharedChannel": activityType};
        } else {
            params = @{@"sharedLink": @"", @"sharedChannel": @""};
        }
        
        NSDictionary *callbackDict = @{ @"callbackId": callbackString, @"params": params, @"error": errorDictItem };
        
        UnitySendMessage("Branch", "_asyncCallbackWithParams", jsonCStringFromDictionary(callbackDict));
    };
}


#pragma mark - Key methods

void _setBranchKey(char *branchKey, char* branchSDKVersion) {
    _branchKey = CreateNSString(branchKey);
    [[Branch getInstance:_branchKey] registerPluginName:@"Unity" version:CreateNSString(branchSDKVersion)];
}

#pragma mark - InitSession methods

void _initSession() {
    [[Branch getInstance:_branchKey] initSessionWithLaunchOptions:_wrapper.launchOptions];
}

void _initSessionWithCallback(char *callbackId) {
    [[Branch getInstance:_branchKey] initSessionWithLaunchOptions:_wrapper.launchOptions andRegisterDeepLinkHandler:callbackWithParamsForCallbackId(callbackId)];
}

void _initSessionAsReferrable(BOOL isReferrable) {
    [[Branch getInstance:_branchKey] initSessionWithLaunchOptions:_wrapper.launchOptions isReferrable:isReferrable];
}

void _initSessionAsReferrableWithCallback(BOOL isReferrable, char *callbackId) {
    [[Branch getInstance:_branchKey] initSessionWithLaunchOptions:_wrapper.launchOptions isReferrable:isReferrable andRegisterDeepLinkHandler:callbackWithParamsForCallbackId(callbackId)];
}

void _initSessionWithUniversalObjectCallback(char *callbackId) {
    [[Branch getInstance:_branchKey] initSessionWithLaunchOptions:_wrapper.launchOptions andRegisterDeepLinkHandlerUsingBranchUniversalObject:callbackWithBranchUniversalObjectForCallbackId(callbackId)];
}

#pragma mark - Session Item methods

const char *_getFirstReferringBranchUniversalObject() {
    BranchUniversalObject* universalObject = [[Branch getInstance:_branchKey] getFirstReferringBranchUniversalObject];
    return jsonCStringFromDictionary(dictFromBranchUniversalObject(universalObject));
}

const char *_getFirstReferringBranchLinkProperties() {
    BranchLinkProperties *linkProperties = [[Branch getInstance:_branchKey] getFirstReferringBranchLinkProperties];
    return jsonCStringFromDictionary(dictFromBranchLinkProperties(linkProperties));
}

const char *_getLatestReferringBranchUniversalObject() {
    BranchUniversalObject *universalObject = [[Branch getInstance:_branchKey]getLatestReferringBranchUniversalObject];
    return jsonCStringFromDictionary(dictFromBranchUniversalObject(universalObject));
}

const char *_getLatestReferringBranchLinkProperties() {
    BranchLinkProperties *linkProperties = [[Branch getInstance:_branchKey] getLatestReferringBranchLinkProperties];
    return jsonCStringFromDictionary(dictFromBranchLinkProperties(linkProperties));
}

void _resetUserSession() {
    [[Branch getInstance:_branchKey] resetUserSession];
}

void _setIdentity(char *userId) {
    [[Branch getInstance:_branchKey] setIdentity:CreateNSString(userId)];
}

void _setIdentityWithCallback(char *userId, char *callbackId) {
    [[Branch getInstance:_branchKey] setIdentity:CreateNSString(userId) withCallback:callbackWithParamsForCallbackId(callbackId)];
}

void _logout() {
    [[Branch getInstance:_branchKey] logout];
}

# pragma mark - Configuation methods

void _setDebug() {
    [[Branch getInstance:_branchKey] setDebug];
}

void _setRetryInterval(int retryInterval) {
    [[Branch getInstance:_branchKey] setRetryInterval:retryInterval];
}

void _setMaxRetries(int maxRetries) {
    [[Branch getInstance:_branchKey] setMaxRetries:maxRetries];
}

void _setNetworkTimeout(int timeout) {
    [[Branch getInstance:_branchKey] setNetworkTimeout:timeout];
}

void _registerView(char *universalObjectJson) {
    NSDictionary *universalObjectDict = dictionaryFromJsonString(universalObjectJson);
    BranchUniversalObject *obj = branchuniversalObjectFormDict(universalObjectDict);
    
    BranchEvent* event = [[BranchEvent alloc] initWithName:BranchStandardEventViewItem];
    [event.contentItems arrayByAddingObject:obj];
    [event logEvent];
}

void _listOnSpotlight(char *universalObjectJson) {
    NSDictionary *universalObjectDict = dictionaryFromJsonString(universalObjectJson);
    BranchUniversalObject *obj = branchuniversalObjectFormDict(universalObjectDict);
    
    [obj listOnSpotlight];
}

void _accountForFacebookSDKPreventingAppLaunch() {
    [[Branch getInstance:_branchKey] accountForFacebookSDKPreventingAppLaunch];
}

void _setRequestMetadata(char *key, char *value) {
    [[Branch getInstance:_branchKey] setRequestMetadataKey:CreateNSString(key) value:CreateNSString(value)];
}

void _setTrackingDisabled(BOOL value) {
    [Branch setTrackingDisabled:value];
}

void _delayInitToCheckForSearchAds() {
    [[Branch getInstance:_branchKey] delayInitToCheckForSearchAds];
}


#pragma mark - User Action methods

void _userCompletedAction(char *action) {
    [[Branch getInstance:_branchKey] userCompletedAction:CreateNSString(action)];
}

void _userCompletedActionWithState(char *action, char *stateDict) {
    [[Branch getInstance:_branchKey] userCompletedAction:CreateNSString(action) withState:dictionaryFromJsonString(stateDict)];
}

#pragma mark - Send event methods

void _sendEvent(char *eventJson) {
    NSDictionary *eventDict = dictionaryFromJsonString(eventJson);
    if (eventDict == nil) {
        return;
    }
    
    BranchEvent *event = nil;
    
    if (eventDict[@"event_name"]) {
        event = [[BranchEvent alloc] initWithName:eventDict[@"event_name"]];
    }
    else {
        return;
    }
    
    if (eventDict[@"transaction_id"]) {
        event.transactionID = eventDict[@"transaction_id"];
    }
    if (eventDict[@"customer_event_alias"]) {
        event.alias = eventDict[@"customer_event_alias"];
    }
    if (eventDict[@"affiliation"]) {
        event.affiliation = eventDict[@"affiliation"];
    }
    if (eventDict[@"coupon"]) {
        event.coupon = eventDict[@"coupon"];
    }
    if (eventDict[@"currency"]) {
        event.currency = eventDict[@"currency"];
    }
    if (eventDict[@"tax"]) {
        event.tax = eventDict[@"tax"];
    }
    if (eventDict[@"revenue"]) {
        event.revenue = eventDict[@"revenue"];
    }
    if (eventDict[@"description"]) {
        event.eventDescription = eventDict[@"description"];
    }
    if (eventDict[@"shipping"]) {
        event.shipping = eventDict[@"shipping"];
    }
    if (eventDict[@"search_query"]) {
        event.searchQuery = eventDict[@"search_query"];
    }
    if (eventDict[@"custom_data"]) {
        event.customData = [eventDict[@"custom_data"] copy];
    }
    if (eventDict[@"content_items"]) {
        NSArray *array = [eventDict[@"content_items"] copy];
        NSMutableArray *buoArray = [[NSMutableArray alloc] init];
        
        for (NSString* buoJson in array) {
            [buoArray addObject:branchuniversalObjectFormDict(dictionaryFromJsonString([buoJson cStringUsingEncoding:NSUTF8StringEncoding]))];
        }
        
        [event setContentItems:buoArray];
    }
    
    [event logEvent];
}

#pragma mark - Credit methods

void _loadRewardsWithCallback(char *callbackId) {
    [[Branch getInstance:_branchKey] loadRewardsWithCallback:callbackWithStatusForCallbackId(callbackId)];
}

int _getCredits() {
    return (int)[[Branch getInstance:_branchKey] getCredits];
}

void _redeemRewards(int count) {
    [[Branch getInstance:_branchKey] redeemRewards:count];
}

int _getCreditsForBucket(char *bucket) {
    return (int)[[Branch getInstance:_branchKey] getCreditsForBucket:CreateNSString(bucket)];
}

void _redeemRewardsForBucket(int count, char *bucket) {
    [[Branch getInstance:_branchKey] redeemRewards:count forBucket:CreateNSString(bucket)];
}

void _getCreditHistoryWithCallback(char *callbackId) {
    [[Branch getInstance:_branchKey] getCreditHistoryWithCallback:callbackWithListForCallbackId(callbackId)];
}

void _getCreditHistoryForBucketWithCallback(char *bucket, char *callbackId) {
    [[Branch getInstance:_branchKey] getCreditHistoryForBucket:CreateNSString(bucket) andCallback:callbackWithListForCallbackId(callbackId)];
}

void _getCreditHistoryForTransactionWithLengthOrderAndCallback(char *creditTransactionId, int length, int order, char *callbackId) {
    [[Branch getInstance:_branchKey] getCreditHistoryAfter:CreateNSString(creditTransactionId) number:length order:(BranchCreditHistoryOrder)order andCallback:callbackWithListForCallbackId(callbackId)];
}

void _getCreditHistoryForBucketWithTransactionLengthOrderAndCallback(char *bucket, char *creditTransactionId, int length, int order, char *callbackId) {
    [[Branch getInstance:_branchKey] getCreditHistoryForBucket:CreateNSString(bucket) after:CreateNSString(creditTransactionId) number:length order:(BranchCreditHistoryOrder)order andCallback:callbackWithListForCallbackId(callbackId)];
}

#pragma mark - Short URL Generation methods

void _getShortURLWithBranchUniversalObjectAndCallback(char *universalObjectJson, char *linkPropertiesJson, char *callbackId) {
    NSDictionary *universalObjectDict = dictionaryFromJsonString(universalObjectJson);
    NSDictionary *linkPropertiesDict = dictionaryFromJsonString(linkPropertiesJson);
    
    BranchUniversalObject *obj = branchuniversalObjectFormDict(universalObjectDict);
    BranchLinkProperties *prop = branchLinkPropertiesFormDict(linkPropertiesDict);
    
    [obj getShortUrlWithLinkProperties:prop andCallback:callbackWithUrlForCallbackId(callbackId)];
}

#pragma mark - Share Link methods

void _shareLinkWithLinkProperties(char *universalObjectJson, char *linkPropertiesJson, char *message, char *callbackId) {
    NSDictionary *universalObjectDict = dictionaryFromJsonString(universalObjectJson);
    NSDictionary *linkPropertiesDict = dictionaryFromJsonString(linkPropertiesJson);
    
    BranchUniversalObject *obj = branchuniversalObjectFormDict(universalObjectDict);
    BranchLinkProperties *prop = branchLinkPropertiesFormDict(linkPropertiesDict);
    
    [obj showShareSheetWithLinkProperties:prop andShareText:CreateNSString(message) fromViewController:nil completion:callbackWithShareCompletionForCallbackId(callbackId)];
}
