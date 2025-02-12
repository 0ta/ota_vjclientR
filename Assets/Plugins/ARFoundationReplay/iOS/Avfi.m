#import <AVFoundation/AVFoundation.h>
#import <CoreMedia/CMMetadata.h>

#if TARGET_OS_IOS
#import <UIKit/UIKit.h>
#endif

#define kMETADATA_ID_RAW @"mdta/com.github.asus4.avfi.raw"
#define kTIMESCALE 240

// Internal objects
static AVAssetWriter* _writer;
static AVAssetWriterInput* _writerVideoInput;
static AVAssetWriterInputPixelBufferAdaptor* _pixelBufferAdaptor;
static AVAssetWriterInput* _writerMetadataInput;
static AVAssetWriterInputMetadataAdaptor* _metadataAdaptor;
static double _frameCount;

extern void Avfi_StartRecordingUsingBookmark(const char* fileName, const Byte *bookmark, int length, int width, int height)
{
    NSLog(@"Using Bookmark");
    if (_writer)
    {
        NSLog(@"Recording has already been initiated.");
        return;
    }
    NSData* bookmarkData = [NSData dataWithBytes:bookmark length:length];
    
    NSError* err;
    
    NSURL* filePathURL = [[NSURL alloc] initByResolvingBookmarkData:bookmarkData options: NSURLBookmarkResolutionWithoutImplicitStartAccessing relativeToURL:nil bookmarkDataIsStale:false error:&err];
    filePathURL = [filePathURL URLByAppendingPathComponent:[NSString stringWithCString: fileName encoding:NSUTF8StringEncoding] isDirectory:false];

    if (err)
    {
        NSLog(@"Failed to Bookmark NSURL (%@)", err);
        return;
    }
    _writer =
      [[AVAssetWriter alloc] initWithURL: filePathURL
                                fileType: AVFileTypeQuickTimeMovie
                                   error: &err];
    _writer.movieTimeScale = kTIMESCALE;

    if (err)
    {
        NSLog(@"Failed to initialize AVAssetWriter (%@)", err);
        return;
    }

    // Asset writer input setup
    NSDictionary* colorPropertySettings =
    @{
        AVVideoColorPrimariesKey: AVVideoColorPrimaries_ITU_R_709_2,
        AVVideoYCbCrMatrixKey: AVVideoTransferFunction_ITU_R_709_2,
        AVVideoTransferFunctionKey: AVVideoYCbCrMatrix_ITU_R_709_2,
    };
    NSDictionary* settings =
    @{
        AVVideoCodecKey: AVVideoCodecTypeH264,
        AVVideoWidthKey: @(width),
        AVVideoHeightKey: @(height),
        AVVideoColorPropertiesKey: colorPropertySettings,
    };

    _writerVideoInput = [AVAssetWriterInput assetWriterInputWithMediaType: AVMediaTypeVideo
                                                      outputSettings: settings];
    _writerVideoInput.expectsMediaDataInRealTime = true;
    _writerVideoInput.mediaTimeScale = kTIMESCALE;

    [_writer addInput:_writerVideoInput];

    // Pixel buffer adaptor setup
    NSDictionary* attribs = @{
        (NSString*)kCVPixelBufferPixelFormatTypeKey: @(kCVPixelFormatType_32BGRA),
        (NSString*)kCVPixelBufferWidthKey: @(width),
        (NSString*)kCVPixelBufferHeightKey: @(height),
    };

    _pixelBufferAdaptor = [AVAssetWriterInputPixelBufferAdaptor assetWriterInputPixelBufferAdaptorWithAssetWriterInput: _writerVideoInput
                                                                                      sourcePixelBufferAttributes: attribs];
    
    // Metadata adaptor setup
    CMFormatDescriptionRef metadataFormatDescription = NULL;
    NSArray *specs = @[
       @{(__bridge NSString *)kCMMetadataFormatDescriptionMetadataSpecificationKey_Identifier : kMETADATA_ID_RAW,
         (__bridge NSString *)kCMMetadataFormatDescriptionMetadataSpecificationKey_DataType : (__bridge NSString *)kCMMetadataBaseDataType_RawData},
    ];
    OSStatus metadataStatus = CMMetadataFormatDescriptionCreateWithMetadataSpecifications(kCFAllocatorDefault, kCMMetadataFormatType_Boxed, (__bridge CFArrayRef)specs, &metadataFormatDescription);
    if(metadataStatus) {
        NSLog(@"CMMetadataFormatDescriptionCreateWithMetadataSpecifications failed with error %d", (int)metadataStatus);
    }
    _writerMetadataInput = [AVAssetWriterInput assetWriterInputWithMediaType:AVMediaTypeMetadata
                                                              outputSettings:nil
                                                            sourceFormatHint:metadataFormatDescription];
    _metadataAdaptor = [AVAssetWriterInputMetadataAdaptor assetWriterInputMetadataAdaptorWithAssetWriterInput: _writerMetadataInput];
    _writerMetadataInput.expectsMediaDataInRealTime = YES;
    
    [_writerMetadataInput addTrackAssociationWithTrackOfInput:_writerVideoInput type:AVTrackAssociationTypeMetadataReferent];
    [_writer addInput:_writerMetadataInput];
    
    // Recording start
    if (![_writer startWriting])
    {
        NSLog(@"Failed to start (%ld: %@)", _writer.status, _writer.error);
        return;
    }

    [_writer startSessionAtSourceTime:kCMTimeZero];
    _frameCount = 0;
}

extern void Avfi_StartRecording(const char* filePath, int width, int height)
{
    if (_writer)
    {
        NSLog(@"Recording has already been initiated.");
        return;
    }

    // Asset writer setup
    NSURL* filePathURL =
      [NSURL fileURLWithPath:[NSString stringWithUTF8String:filePath]];

    NSError* err;
    _writer =
      [[AVAssetWriter alloc] initWithURL: filePathURL
                                fileType: AVFileTypeQuickTimeMovie
                                   error: &err];
    _writer.movieTimeScale = kTIMESCALE;

    if (err)
    {
        NSLog(@"Failed to initialize AVAssetWriter (%@)", err);
        return;
    }

    // Asset writer input setup
    NSDictionary* colorPropertySettings =
    @{
        AVVideoColorPrimariesKey: AVVideoColorPrimaries_ITU_R_709_2,
        AVVideoYCbCrMatrixKey: AVVideoTransferFunction_ITU_R_709_2,
        AVVideoTransferFunctionKey: AVVideoYCbCrMatrix_ITU_R_709_2,
    };
    NSDictionary* settings =
    @{
        AVVideoCodecKey: AVVideoCodecTypeH264,
        AVVideoWidthKey: @(width),
        AVVideoHeightKey: @(height),
        AVVideoColorPropertiesKey: colorPropertySettings,
    };

    _writerVideoInput = [AVAssetWriterInput assetWriterInputWithMediaType: AVMediaTypeVideo
                                                      outputSettings: settings];
    _writerVideoInput.expectsMediaDataInRealTime = true;
    _writerVideoInput.mediaTimeScale = kTIMESCALE;

    [_writer addInput:_writerVideoInput];

    // Pixel buffer adaptor setup
    NSDictionary* attribs = @{
        (NSString*)kCVPixelBufferPixelFormatTypeKey: @(kCVPixelFormatType_32BGRA),
        (NSString*)kCVPixelBufferWidthKey: @(width),
        (NSString*)kCVPixelBufferHeightKey: @(height),
    };

    _pixelBufferAdaptor = [AVAssetWriterInputPixelBufferAdaptor assetWriterInputPixelBufferAdaptorWithAssetWriterInput: _writerVideoInput
                                                                                      sourcePixelBufferAttributes: attribs];
    
    // Metadata adaptor setup
    CMFormatDescriptionRef metadataFormatDescription = NULL;
    NSArray *specs = @[
       @{(__bridge NSString *)kCMMetadataFormatDescriptionMetadataSpecificationKey_Identifier : kMETADATA_ID_RAW,
         (__bridge NSString *)kCMMetadataFormatDescriptionMetadataSpecificationKey_DataType : (__bridge NSString *)kCMMetadataBaseDataType_RawData},
    ];
    OSStatus metadataStatus = CMMetadataFormatDescriptionCreateWithMetadataSpecifications(kCFAllocatorDefault, kCMMetadataFormatType_Boxed, (__bridge CFArrayRef)specs, &metadataFormatDescription);
    if(metadataStatus) {
        NSLog(@"CMMetadataFormatDescriptionCreateWithMetadataSpecifications failed with error %d", (int)metadataStatus);
    }
    _writerMetadataInput = [AVAssetWriterInput assetWriterInputWithMediaType:AVMediaTypeMetadata
                                                              outputSettings:nil
                                                            sourceFormatHint:metadataFormatDescription];
    _metadataAdaptor = [AVAssetWriterInputMetadataAdaptor assetWriterInputMetadataAdaptorWithAssetWriterInput: _writerMetadataInput];
    _writerMetadataInput.expectsMediaDataInRealTime = YES;
    
    [_writerMetadataInput addTrackAssociationWithTrackOfInput:_writerVideoInput type:AVTrackAssociationTypeMetadataReferent];
    [_writer addInput:_writerMetadataInput];
    
    // Recording start
    if (![_writer startWriting])
    {
        NSLog(@"Failed to start (%ld: %@)", _writer.status, _writer.error);
        return;
    }

    [_writer startSessionAtSourceTime:kCMTimeZero];
    _frameCount = 0;
}

extern void Avfi_AppendFrame(
    const void* source, uint32_t size,
    const void* metadata, uint32_t metadataSize,
    double time)
{
    if (!_writer)
    {
        NSLog(@"Recording hasn't been initiated.");
        return;
    }

    if (!_writerVideoInput.isReadyForMoreMediaData || !_writerMetadataInput.isReadyForMoreMediaData)
    {
        NSLog(@"Writer is not ready.");
        return;
    }

    // Buffer allocation
    CVPixelBufferRef buffer;
    CVReturn ret = CVPixelBufferPoolCreatePixelBuffer
      (NULL, _pixelBufferAdaptor.pixelBufferPool, &buffer);

    if (ret != kCVReturnSuccess)
    {
        NSLog(@"Can't allocate a pixel buffer (%d)", ret);
        NSLog(@"%ld: %@", _writer.status, _writer.error);
        return;
    }

    // Buffer update
    CVPixelBufferLockBaseAddress(buffer, 0);

    void* pointer = CVPixelBufferGetBaseAddress(buffer);
    size_t buffer_size = CVPixelBufferGetDataSize(buffer);
    memcpy(pointer, source, MIN(size, buffer_size));

    CVPixelBufferUnlockBaseAddress(buffer, 0);

    // Buffer submission
    BOOL success = [_pixelBufferAdaptor appendPixelBuffer:buffer
                                withPresentationTime:CMTimeMakeWithSeconds(time, kTIMESCALE)];
    if (!success) {
        NSLog(@"Warning: Unable to write buffer to video");
    }

    CVPixelBufferUnlockBaseAddress(buffer, 0);
    CVPixelBufferRelease(buffer);

    if (metadataSize > 0)
    {
        // Metadata submission
        AVMutableMetadataItem* metadataItem = [AVMutableMetadataItem metadataItem];
        metadataItem.identifier = kMETADATA_ID_RAW;
        metadataItem.dataType = (__bridge NSString *)kCMMetadataBaseDataType_RawData;
        metadataItem.value = [NSData dataWithBytes:metadata length:metadataSize];

        CMTimeRange metadataTime = CMTimeRangeMake(CMTimeMakeWithSeconds(time, kTIMESCALE), kCMTimeInvalid);
        AVTimedMetadataGroup* metadataGroup = [[AVTimedMetadataGroup alloc] initWithItems:@[metadataItem]
                                                                                timeRange:metadataTime];
        [_metadataAdaptor appendTimedMetadataGroup:metadataGroup];
    }    
}

extern void Avfi_AddMetadata(const char* key, const char* value)
{
    if (!_writer)
    {
        NSLog(@"Recording hasn't been initiated.");
        return;
    }
    // Create metadata from JSON value
    AVMutableMetadataItem* metadataItem = [AVMutableMetadataItem metadataItem];
    metadataItem.identifier = [NSString stringWithUTF8String:key];
    metadataItem.dataType = (__bridge NSString *)kCMMetadataBaseDataType_JSON;
    metadataItem.value = [NSString stringWithUTF8String:key];

    _writer.metadata = @[metadataItem];
    NSLog(@"Set metadta");
}

extern void Avfi_EndRecording(bool isSave)
{
    if (!_writer)
    {
        NSLog(@"Recording hasn't been initiated.");
        return;
    }

    [_writerVideoInput markAsFinished];

    if (isSave)
    {
#if TARGET_OS_IOS
        NSString* path = _writer.outputURL.path;
        [_writer finishWritingWithCompletionHandler: ^{
            UISaveVideoAtPathToSavedPhotosAlbum(path, nil, nil, nil);
        }];
#else
        [_writer finishWritingWithCompletionHandler: ^{}];

#endif
    }

    _writer = NULL;
    _writerVideoInput = NULL;
    _pixelBufferAdaptor = NULL;
    _writerMetadataInput = NULL;
    _metadataAdaptor = NULL;
}
