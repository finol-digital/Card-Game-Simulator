//
//  Clipper.m
//  Clip
//
//  Created by sanuki.wataru on 2015/01/15.
//  Copyright (c) 2015年 sanuki.wataru. All rights reserved.
//

char *MakeStringCpy(const char* string);

void SetText_(const char* c){
    [UIPasteboard generalPasteboard].string = [NSString stringWithCString: c encoding:NSUTF8StringEncoding];
}

char *GetText_(){
    return MakeStringCpy([[UIPasteboard generalPasteboard].string UTF8String]);
}

char *MakeStringCpy(const char* string){
   	if (string == NULL)
        return NULL;
    
    char* res = (char*)malloc(strlen(string) + 1);
    strcpy(res, string);
    return res;
}