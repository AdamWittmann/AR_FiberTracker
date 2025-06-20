#ifdef __cplusplus
extern "C" {
#endif

void StartGeoSession(void);
void AddGeoAnchor(double lat, double lon, double alt);
void EnableCoachingOverlay(void);

#ifdef __cplusplus
}
#endif