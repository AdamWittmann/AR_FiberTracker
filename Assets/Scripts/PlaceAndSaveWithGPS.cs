using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using System.IO;
using System.Collections;

[System.Serializable]
public class GPSLoc
{
    public double latitude;
    public double longitude;
    public double altitude;
}

public class GPSLocList
{
    public List<GPSLoc> points = new List<GPSLoc>();
}

//PlacedPrefab is the object (marker)
//spawnedObject Will hold/store the placed object when instantiate it
//hits is a list to store results of a raycast.
public class PlaceAndSaveWithGPS : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Instantiates this prefab on a plane at the touch location.")]
    GameObject placedPrefab;

    [SerializeField]
    [Tooltip("Enable to log debug messages in the console.")]
    bool debugMode = true;

    GameObject spawnedObject;

    ARRaycastManager aRRaycastManager;
    List<ARRaycastHit> hits = new List<ARRaycastHit>();

    GPSLocList savedPoints = new GPSLocList();

    //Start location tracking once program boots -> initialize
    void Awake()
    {
        aRRaycastManager = GetComponent<ARRaycastManager>();
        StartCoroutine(StartLocationService());
    }

    void Update()
    {
        //If no touchy, do nothing
        if (Input.touchCount == 0 || Input.GetTouch(0).phase != TouchPhase.Began)
            return;

        //If plane gets tapped, get the touch, plot the point if on a 'seen' plane
        //'seen' plane meaning recognized by the camera (shows up yellow)
        if (aRRaycastManager.Raycast(Input.GetTouch(0).position, hits, TrackableType.PlaneWithinPolygon))
        {
            //performs a raycast from the touch position into the AR world
            //from screen to AR world plane: Where they intersect
            var hitPose = hits[0].pose;

            //If object hasnt been placed, place, if not, move to new location
            //^^^^^^^^^^^^ This will be changed bc we'll want to plot more than 1 point...
            if (spawnedObject == null)
            {
                //instantiate means like stamp it down
                //spawn copy of the prefab, placedPrefab is the template object (prefab), hitPose.position is the exact world position on the AR plane
                //hitPose.rotation = orientation of that point
                //Simply: Spawn a copy, orientate on the taps position on the plane -> store reference in spawnedObject
                spawnedObject = Instantiate(placedPrefab, hitPose.position, hitPose.rotation);
            }
            else
            {
                spawnedObject.transform.position = hitPose.position;
                spawnedObject.transform.rotation = hitPose.rotation;
            }

            // Make the object face the camera (optional)
            Vector3 lookPos = Camera.main.transform.position - spawnedObject.transform.position;
            lookPos.y = 0;
            spawnedObject.transform.rotation = Quaternion.LookRotation(lookPos);

            // Save GPS at this point
            var gps = Input.location.lastData;
            savedPoints.points.Add(new GPSLoc
            {
                latitude = gps.latitude,
                longitude = gps.longitude,
                altitude = gps.altitude
            });
            DebugDisplay.Instance.Log($"GPS: {Input.location.lastData.latitude}, {Input.location.lastData.longitude}");
            if (debugMode)
            {
                Debug.Log($"Placed point at Lat: {gps.latitude}, Lon: {gps.longitude}, Alt: {gps.altitude}");
            }

            SaveToJSON();
        }
    }

    void SaveToJSON()
    {
        string path = Application.persistentDataPath + "/gps_points.json";
        string json = JsonUtility.ToJson(savedPoints, true);
        File.WriteAllText(path, json);
        
        if (debugMode)
        {
            Debug.Log($"Saved GPS point. File path: {path}");
        }
    }

    //This function checks if GPS is enabled, starts it, and waits up to 20 seconds for it to initialize.
    //initialize meaning boot up properly, if it doesn't and error gets logged in console.
    IEnumerator StartLocationService()
    {
        if (!Input.location.isEnabledByUser)
        {
            if (debugMode)
            {
                Debug.Log("Location services not enabled");
            }
            yield break;
        }

        Input.location.Start();

        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            if (debugMode)
            {
                Debug.Log("Waiting for GPS to initialize...");
            }

            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (Input.location.status != LocationServiceStatus.Running)
        {
            if (debugMode)
            {
                Debug.Log("Location service failed");
            }
            yield break;
        }

        if (debugMode)
        {
            Debug.Log("Location service started");
        }
    }
}
