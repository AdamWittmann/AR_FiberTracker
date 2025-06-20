#import "GeoAnchorPlugin.h"
#import "GeoAnchorManager.h"

GeoAnchorManager* geoManager;

GeoAnchorManager* geoManager = nil;

void StartGeoTracking() {
    if (!geoManager) geoManager = [[GeoAnchorManager alloc] init];
    [geoManager startTracking];
}

void AddGeoAnchor(double lat, double lon, const char* anchorName) {
    if (geoManager) {
        [geoManager addAnchorWithLat:lat lon:lon name:[NSString stringWithUTF8String:anchorName]];
    }
} 