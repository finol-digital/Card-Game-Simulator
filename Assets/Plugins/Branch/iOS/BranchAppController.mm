#import <UIKit/UIKit.h>
#import "UnityAppController.h"
#import "UI/UnityView.h"
#import "UI/UnityViewControllerBase.h"
#import "BranchiOSWrapper.h"

@interface BranchAppController : UnityAppController
{
}
@end

@implementation BranchAppController

- (BOOL)application:(UIApplication *)application continueUserActivity:(NSUserActivity *)userActivity restorationHandler:(void (^)(NSArray *))restorationHandler {
    BOOL handledByBranch = [[BranchUnityWrapper sharedInstance] continueUserActivity:userActivity];
    return handledByBranch;
}

@end

IMPL_APP_CONTROLLER_SUBCLASS(BranchAppController)

