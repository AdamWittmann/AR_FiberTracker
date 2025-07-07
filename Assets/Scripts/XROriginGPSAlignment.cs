using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Unity.Location;
using Mapbox.Utils;
using TMPro;
using System.Collections;

public class XROriginGPSAlignment : MonoBehaviour
{
    [SerializeField] private TMP_Text calibrationText;
    [SerializeField] private Transform xrOrigin;
    [SerializeField] private AbstractMap map;
    [SerializeField] private Vector2d campusCenterCoordinates;
    [SerializeField] private float accuracyThreshold = 10f;
    
    private ILocationProvider locationProvider;
    private float bestAccuracy = float.MaxValue;
    private bool isCalibrated = false;

    void Start()
    {
        calibrationText.text = "Starting system...";
        StartCoroutine(InitializeSystem());
    }

    private IEnumerator InitializeSystem()
    {
        // Step 1: Initialize compass first
        yield return StartCoroutine(InitializeCompass());
        
        // Step 2: Start GPS
        yield return StartCoroutine(InitializeGPS());
        
        // Step 3: Initialize map centered on campus
        yield return StartCoroutine(InitializeMap());
        
        // Step 4: Start tracking phone position relative to campus
        StartLocationTracking();
    }

    private IEnumerator InitializeCompass()
    {
        calibrationText.text = "Starting compass...";
        
        Input.compass.enabled = true;
        
        // Wait for compass to stabilize
        yield return new WaitForSeconds(2f);
        
        calibrationText.text = "Compass ready";
    }

    private IEnumerator InitializeGPS()
    {
        calibrationText.text = "Starting GPS...";
        
        if (!Input.location.isEnabledByUser)
        {
            calibrationText.text = "Enable location services in settings";
            yield break;
        }

        Input.location.Start(1f, 1f);
        
        while (Input.location.status == LocationServiceStatus.Initializing)
        {
            calibrationText.text = "Initializing GPS...";
            yield return new WaitForSeconds(1f);
        }

        if (Input.location.status == LocationServiceStatus.Running)
        {
            calibrationText.text = "GPS ready";
        }
        else
        {
            calibrationText.text = $"GPS failed: {Input.location.status}";
            yield return new WaitForSeconds(2f);
            StartCoroutine(InitializeGPS()); // Retry
        }
    }

    private IEnumerator InitializeMap()
    {
        calibrationText.text = "Loading campus map...";
        
        if (map == null)
        {
            calibrationText.text = "Map reference missing - check inspector";
            yield break;
        }
        
        if (!map.IsAccessTokenValid)
        {
            calibrationText.text = "Invalid Mapbox token - check MapboxAccess.txt";
            yield break;
        }

        // Check if map is already initialized
        if (map.IsEditorPreviewEnabled)
        {
            calibrationText.text = "Map preview enabled - should work";
            yield return new WaitForSeconds(1f);
            calibrationText.text = "Campus map ready";
            yield break;
        }

        bool mapReady = false;
        float waitTime = 0f;

        // Try multiple events to catch initialization
        map.OnInitialized += () => {
            Debug.Log("Map OnInitialized fired");
            calibrationText.text = "Map initialized!";
            mapReady = true;
        };

        map.OnUpdated += () => {
            Debug.Log("Map OnUpdated fired");
            if (!mapReady) 
            {
                calibrationText.text = "Map updating...";
                mapReady = true;
            }
        };

        // Wait for initialization with periodic updates
        while (!mapReady && waitTime < 30f)
        {
            waitTime += 1f;
            
            // Update status every few seconds
            if (waitTime % 3 == 0)
            {
                calibrationText.text = $"Loading tiles... ({waitTime:F0}s)";
            }

            // Force update the map periodically
            if (waitTime % 5 == 0)
            {
                Debug.Log("Forcing map update...");
                map.UpdateMap();
            }

            yield return new WaitForSeconds(1f);
        }

        if (mapReady)
        {
            calibrationText.text = "Campus map loaded";
        }
        else
        {
            calibrationText.text = "Map slow to load - proceeding anyway";
            Debug.LogWarning("Map didn't initialize properly but proceeding");
        }
    }

    private void StartLocationTracking()
    {
        calibrationText.text = "Connecting to location provider...";
        
        if (LocationProviderFactory.Instance?.DefaultLocationProvider == null)
        {
            StartCoroutine(RetryLocationProvider());
            return;
        }

        locationProvider = LocationProviderFactory.Instance.DefaultLocationProvider;
        locationProvider.OnLocationUpdated += OnLocationUpdated;
        calibrationText.text = "Tracking started - walk around campus";
    }

    private IEnumerator RetryLocationProvider()
    {
        yield return new WaitForSeconds(1f);
        StartLocationTracking();
    }

    void OnLocationUpdated(Location location)
    {
        if (location.LatitudeLongitude == null) return;

        float accuracy = location.Accuracy;
        
        // Calculate your position relative to campus center
        Vector3 campusWorldPos = map.GeoToWorldPosition(campusCenterCoordinates);
        Vector3 yourWorldPos = map.GeoToWorldPosition(location.LatitudeLongitude);
        Vector3 offset = yourWorldPos - campusWorldPos;

        // Apply compass heading correction
        float heading = Input.compass.trueHeading;
        xrOrigin.rotation = Quaternion.Euler(0, -heading, 0);

        // Update XR Origin so campus stays at world center, you move relative to it
        xrOrigin.position = -offset;

        // Track best accuracy for calibration
        if (accuracy < bestAccuracy)
        {
            bestAccuracy = accuracy;
            if (accuracy < accuracyThreshold)
            {
                isCalibrated = true;
            }
        }

        // Display status: GPS accuracy + your game world position
        string status = isCalibrated ? "✓ " : "";
        calibrationText.text = $"{status}GPS: {accuracy:F1}m | Position: ({offset.x:F1}, {offset.z:F1}) | Best: {bestAccuracy:F1}m";
        
        Debug.Log($"GPS: {accuracy:F1}m, Game Position: {offset}, XR Origin: {xrOrigin.position}, Heading: {heading:F1}°");
    }

    void OnDestroy()
    {
        if (locationProvider != null)
            locationProvider.OnLocationUpdated -= OnLocationUpdated;
        
        Input.location.Stop();
        Input.compass.enabled = false;
    }
}