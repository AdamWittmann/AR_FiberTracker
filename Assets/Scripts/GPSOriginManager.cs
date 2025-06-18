//Stores Origin GPS point and handles conversion to Unity space
//vector3 for relative position offsets 
//mathf for cosine of latitude
//MonoBehavior Unity component structure
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;


public class GPSOriginManager : MonoBehaviour
{
    // Declare public variables to store origin latitude, longitude, and altitude
    public double originLatitude = 41.721247;
    public double originLongitude = -73.932518;
    public double originAltitude = 30.0;
    // Optional: define a constant for Earth's radius in meters
    private const double earthRadius = 6378137;

    public GameObject originPrefab;
    void Awake()
    {
        Vector3 originPos = ConvertToUnityPosition(originLatitude, originLongitude, originAltitude);
        Instantiate(originPrefab, originPos, Quaternion.identity);

    }
    public Vector3 ConvertToUnityPosition(double targetLat, double targetLon, double targetAlt)
    {
        // Convert degrees to radians for math
        double latRadOrigin = originLatitude * Mathf.Deg2Rad;
        double latRadTarget = targetLat * Mathf.Deg2Rad;

        // Calculate differences in radians
        double dLat = latRadTarget - latRadOrigin;
        double dLon = (targetLon - originLongitude) * Mathf.Deg2Rad;

        // Calculate offsets in meters using equirectangular approximation:
        double x = earthRadius * dLon * Mathf.Cos((float)latRadOrigin);  // East-West offset (x axis)
        double z = earthRadius * dLat;                                   // North-South offset (z axis)
        double y = targetAlt - originAltitude;                           // Up-Down offset (y axis)

        // Unity's coordinate system: (x, y, z) => (East, Up, North)
        return new Vector3((float)x, (float)y, (float)z);
    }
}