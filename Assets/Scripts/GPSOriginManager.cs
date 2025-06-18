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
    
    //Testing with spawning at the opposite side of basement of DN
    // public double originLatitude = 41.72081;
    // public double originLongitude = -73.9328304;
    public double originAltitude = 30.0;
    // Optional: define a constant for Earth's radius in meters
    private const double earthRadius = 6378137;

    public GameObject originPrefab;
    public GameObject sceneRoot;
    void Start()
    {
        sceneRoot ??= this.gameObject;
        StartCoroutine(PlaceOriginWhenReady());
        
    }
        IEnumerator PlaceOriginWhenReady()
        {
        while (!LocationManager.Instance.IsReady)
    {
        yield return null;
    }

    if (sceneRoot == null)
    {
        Debug.LogError("sceneRoot is not assigned!");
        yield break;
    }

    if (originPrefab == null)
    {
        Debug.LogError("originPrefab is not assigned!");
        yield break;
    }

    Debug.Log("LocationManager is ready. Proceeding to place origin.");
            Vector3 originPos = ConvertToUnityPosition(originLatitude, originLongitude, originAltitude);
            Vector3 devicePos = ConvertToUnityPosition(LocationManager.Instance.deviceLat, LocationManager.Instance.deviceLong, LocationManager.Instance.deviceAlt);
            Vector3 offset = devicePos - originPos;
            sceneRoot.transform.position = -offset;
            Debug.Log($"Origin: ({originLatitude}, {originLongitude}, {originAltitude})");
            Debug.Log($"Device: ({LocationManager.Instance.deviceLat}, {LocationManager.Instance.deviceLong}, {LocationManager.Instance.deviceAlt})");
            Debug.Log($"OriginPos: {originPos}, DevicePos: {devicePos}, Offset: {offset}");
            Instantiate(originPrefab, -offset, Quaternion.identity, sceneRoot.transform);
            //test
            Vector3 placePos = new Vector3(0, 0, 10); // 10 meters forward in Unity space
            Instantiate(originPrefab, placePos, Quaternion.identity, sceneRoot.transform);
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