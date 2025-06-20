#import <Foundation/Foundation.h>

@interface GeoAnchorManager : NSObject

- (void)startGeoSession;
- (void)addGeoAnchorWithLatitude:(double)latitude longitude:(double)longitude;
- (void)enableCoachingOverlay;

@end