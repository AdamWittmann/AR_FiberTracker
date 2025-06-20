//Stores Origin GPS point and handles conversion to Unity space
//vector3 for relative position offsets 
//mathf for cosine of latitude
//MonoBehavior Unity component structure
using UnityEngine;
using System.Runtime.InteropServices;

// public class GPSOriginManager : MonoBehaviour
// {
//     // Declare public variables to store origin latitude, longitude, and altitude
//     public double originLatitude;
//     public double originLongitude;

//     public double originAltitude;
//     // Optional: define a constant for Earth's radius in meters
//     private const double earthRadius = 6378137;
//     IEnumerator Start()
//     {
//         while (LocationManager.Instance == null || !LocationManager.Instance.IsReady)
//             yield return null;

//         SetOrigin(LocationManager.Instance.GetCurrentLocation());

//     }

//     public void SetOrigin(LocationInfo location)
//     {
//         // Set the origin values using parameters
//         originLatitude = location.Latitude;
//         originLongitude = location.Longitude;
//         originAltitude = location.Altitude;

//         isOriginSet = true;
//     }

//     public Vector3 ConvertToUnityPosition(double targetLat, double targetLon, double targetAlt)
// {
//     // Convert degrees to radians for math
//     double latRadOrigin = originLatitude * Mathf.Deg2Rad;
//     double latRadTarget = targetLat * Mathf.Deg2Rad;

//     // Calculate differences in radians
//     double dLat = latRadTarget - latRadOrigin;
//     double dLon = (targetLon - originLongitude) * Mathf.Deg2Rad;

//     // Calculate offsets in meters using equirectangular approximation:
//     double x = earthRadius * dLon * Mathf.Cos((float)latRadOrigin);  // East-West offset (x axis)
//     double z = earthRadius * dLat;                                   // North-South offset (z axis)
//     double y = targetAlt - originAltitude;                           // Up-Down offset (y axis)

//     // Unity's coordinate system: (x, y, z) => (East, Up, North)
//     return new Vector3((float)x, (float)y, (float)z);
// }
// }

public class GPSOriginManager : MonoBehaviour
{
    #if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void StartGeoSession();

    [DllImport("__Internal")]
    private static extern void AddGeoAnchor(double lat, double lon);

    [DllImport("__Internal")]
    private static extern void EnableCoachingOverlay();
#else
    private static void StartGeoSession() {}
    private static void AddGeoAnchor(double lat, double lon) {}
    private static void EnableCoachingOverlay() {}
#endif

    void Start()
    {
        StartGeoSession();
        EnableCoachingOverlay();
    }

    public void PlaceGeoAnchor()
    {
        AddGeoAnchor(); // Example: Donnelly
    }

}