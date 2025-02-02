#import <UnityFramework/UnityFramework-Swift.h>

 #ifdef __cplusplus
 extern "C" {
 #endif
    void pickDirWithSecurityScope() {
        [OtaNativeDirPickerManager.getInstance pickDirWithSecurityScopeWithController: UnityGetGLViewController()];
    }
 
    char* getSecurityScopeURL() {
        return strdup(OtaNativeDirPickerManager.getInstance.getSecurityScopeURL.UTF8String);
    }
 
    int getSecurityScopeBookmark(int** dataPtr) {
        NSData *data = [OtaNativeDirPickerManager.getInstance getSecurityScopeBookmark];
        if (data == nil) {
            return 0;
        }
        NSLog(@"%@", [NSString stringWithFormat:@"%lu", static_cast<unsigned long>(data.length)]);
        unsigned char* cdata = (unsigned char*)malloc(data.length);
        [data getBytes:cdata length:data.length];
        *dataPtr = (int*) cdata;
        return (int) data.length;
    }
 
    void stopAccessingSecurityScopedResource() {
            [OtaNativeDirPickerManager.getInstance stopAccessingSecurityScopedResource];
    }
 #ifdef __cplusplus
 }
 #endif
    
