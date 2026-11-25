//
//  UniversalSDKNativeInterface.m
//  UniversalSDKUnityBridge
//
//  Created by coolishbee on 2022/11/09.
//

#import <Foundation/Foundation.h>
#import "UniversalSDKWrapper.h"

#define UNIVERSAL_SDK_EXTERNC extern "C"

@protocol RainbowGoogleSignIn
+ (id<RainbowGoogleSignIn>)sharedInstance;
- (BOOL)handleURL:(NSURL *)url;
- (void)signOut;
@end

static id<RainbowGoogleSignIn> GoogleSignInInstance()
{
    Class googleSignIn = NSClassFromString(@"GIDSignIn");
    return [(Class<RainbowGoogleSignIn>)googleSignIn sharedInstance];
}

// MARK: - Helpers
NSString* UniversalSDKMakeNSString(const char* string)
{
    if(string) {
        return [NSString stringWithUTF8String:string];
    }else{
        return [NSString stringWithUTF8String:""];
    }
}

char* UniversalSDKMakeCString(NSString *str)
{
    const char* string = [str UTF8String];
    if(string == NULL) {
        return NULL;
    }
    
    char *buffer = (char*)malloc(strlen(string)+1);
    strcpy(buffer, string);
    return buffer;
}

UNIVERSAL_SDK_EXTERNC void universal_sdk_UnitySendMessage(const char *name,
                                                          const char *method,
                                                          NSString *params)
{
    UnitySendMessage(name, method, UniversalSDKMakeCString(params));
}

// MARK: - Extern APIs
UNIVERSAL_SDK_EXTERNC void universal_sdk_setup();
void universal_sdk_setup()
{
    [[UniversalSDKWrapper sharedInstance] setupSDK];
}

UNIVERSAL_SDK_EXTERNC int universal_sdk_has_google_configuration()
{
    NSString *clientId = [[NSBundle mainBundle] objectForInfoDictionaryKey:@"GIDClientID"];
    return clientId.length > 0;
}

UNIVERSAL_SDK_EXTERNC void universal_sdk_handle_google_url(const char *url)
{
    NSURL *callback = [NSURL URLWithString:UniversalSDKMakeNSString(url)];
    if (![callback.scheme hasPrefix:@"com.googleusercontent.apps."])
        return;

    dispatch_async(dispatch_get_main_queue(), ^{
        [GoogleSignInInstance() handleURL:callback];
    });
}

UNIVERSAL_SDK_EXTERNC void universal_sdk_login(const char* identifier,
                                               int loginType);
void universal_sdk_login(const char* identifier,
                         int loginType)
{
    NSString *nsIdentifier = UniversalSDKMakeNSString(identifier);
    [[UniversalSDKWrapper sharedInstance] login:nsIdentifier
                                           type:loginType];
}

UNIVERSAL_SDK_EXTERNC void universal_sdk_logout();
void universal_sdk_logout()
{
    dispatch_async(dispatch_get_main_queue(), ^{
        [GoogleSignInInstance() signOut];
    });
}

UNIVERSAL_SDK_EXTERNC void universal_sdk_openSafariView(const char* url);
void universal_sdk_openSafariView(const char* url)
{
    NSString *nsUrl = UniversalSDKMakeNSString(url);
    [[UniversalSDKWrapper sharedInstance] openSafariView:nsUrl];
}
