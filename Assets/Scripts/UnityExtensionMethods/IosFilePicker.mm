/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

#import <UIKit/UIKit.h>
#import <UniformTypeIdentifiers/UniformTypeIdentifiers.h>

extern "C" UIViewController *UnityGetGLViewController(void);

typedef void (*CgsFolderPickedCallback)(int status, const char *value);
typedef void (*CgsImageSourceCallback)(int choice);

static const int CgsPickCancelled = 0;
static const int CgsPickSucceeded = 1;
static const int CgsPickFailed = 2;

@interface CgsFolderPickerDelegate : NSObject <UIDocumentPickerDelegate>
@property (nonatomic, copy) NSString *destination;
@property (nonatomic, assign) CgsFolderPickedCallback callback;
@end

// UIDocumentPickerViewController keeps only a weak reference to its delegate.
static CgsFolderPickerDelegate *CgsActiveFolderPicker;

static void CgsFinishFolderPick(CgsFolderPickedCallback callback, int status, NSString *value)
{
    dispatch_async(dispatch_get_main_queue(), ^{
        CgsActiveFolderPicker = nil;
        callback(status, value.UTF8String);
    });
}

// Copies the chosen folder into the app's cache while its security-scoped access lasts,
// so the rest of CGS can read it with ordinary file APIs.
static void CgsCopyPickedFolder(NSURL *source, NSString *destination, CgsFolderPickedCallback callback)
{
    dispatch_async(dispatch_get_global_queue(QOS_CLASS_USER_INITIATED, 0), ^{
        BOOL isAccessing = [source startAccessingSecurityScopedResource];
        NSString *target = [destination stringByAppendingPathComponent:source.lastPathComponent];
        __block BOOL didCopy = NO;
        __block NSError *copyError = nil;
        NSError *coordinationError = nil;
        [[[NSFileCoordinator alloc] initWithFilePresenter:nil] coordinateReadingItemAtURL:source
            options:NSFileCoordinatorReadingWithoutChanges error:&coordinationError byAccessor:^(NSURL *readUrl) {
                NSFileManager *fileManager = NSFileManager.defaultManager;
                didCopy = [fileManager createDirectoryAtPath:destination withIntermediateDirectories:YES
                                                  attributes:nil error:&copyError]
                          && [fileManager copyItemAtURL:readUrl toURL:[NSURL fileURLWithPath:target isDirectory:YES]
                                                  error:&copyError];
            }];
        if (isAccessing)
            [source stopAccessingSecurityScopedResource];

        NSError *error = copyError ?: coordinationError;
        CgsFinishFolderPick(callback, didCopy ? CgsPickSucceeded : CgsPickFailed,
                            didCopy ? target : (error.localizedDescription ?: @"The folder could not be copied."));
    });
}

@implementation CgsFolderPickerDelegate

- (void)documentPicker:(UIDocumentPickerViewController *)controller didPickDocumentsAtURLs:(NSArray<NSURL *> *)urls
{
    NSURL *source = urls.firstObject;
    if (source == nil)
        CgsFinishFolderPick(self.callback, CgsPickCancelled, nil);
    else
        CgsCopyPickedFolder(source, self.destination, self.callback);
}

- (void)documentPickerWasCancelled:(UIDocumentPickerViewController *)controller
{
    CgsFinishFolderPick(self.callback, CgsPickCancelled, nil);
}

@end

extern "C" int CgsPickFolder(const char *destination, CgsFolderPickedCallback callback)
{
    if (CgsActiveFolderPicker != nil)
        return 0;

    CgsFolderPickerDelegate *pickerDelegate = [CgsFolderPickerDelegate new];
    pickerDelegate.destination = [NSString stringWithUTF8String:destination];
    pickerDelegate.callback = callback;

    UIDocumentPickerViewController *picker =
        [[UIDocumentPickerViewController alloc] initForOpeningContentTypes:@[UTTypeFolder]];
    picker.allowsMultipleSelection = NO;
    picker.delegate = pickerDelegate;
    CgsActiveFolderPicker = pickerDelegate;
    [UnityGetGLViewController() presentViewController:picker animated:YES completion:nil];
    return 1;
}

extern "C" void CgsChooseImageSource(const char *title, const char *photos, const char *files, const char *cancel,
                                     CgsImageSourceCallback callback)
{
    UIAlertController *alert = [UIAlertController alertControllerWithTitle:[NSString stringWithUTF8String:title]
                                                                   message:nil
                                                            preferredStyle:UIAlertControllerStyleActionSheet];
    [alert addAction:[UIAlertAction actionWithTitle:[NSString stringWithUTF8String:photos]
                                              style:UIAlertActionStyleDefault
                                            handler:^(UIAlertAction *action) { callback(1); }]];
    [alert addAction:[UIAlertAction actionWithTitle:[NSString stringWithUTF8String:files]
                                              style:UIAlertActionStyleDefault
                                            handler:^(UIAlertAction *action) { callback(2); }]];
    [alert addAction:[UIAlertAction actionWithTitle:[NSString stringWithUTF8String:cancel]
                                              style:UIAlertActionStyleCancel
                                            handler:^(UIAlertAction *action) { callback(0); }]];

    // iPad shows action sheets as popovers, which need an anchor.
    UIViewController *root = UnityGetGLViewController();
    UIPopoverPresentationController *popover = alert.popoverPresentationController;
    if (popover != nil)
    {
        popover.sourceView = root.view;
        popover.sourceRect = CGRectMake(CGRectGetMidX(root.view.bounds), CGRectGetMidY(root.view.bounds), 0, 0);
        popover.permittedArrowDirections = 0;
    }
    [root presentViewController:alert animated:YES completion:nil];
}
