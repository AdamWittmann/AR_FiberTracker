using UnityEngine;
using Mapbox.Utils;
using Mapbox.Unity.Map;
using System.Net;

public class SpawnObject : MonoBehaviour
{
    public AbstractMap map;
    public GameObject prefabToSpawn;

    double targetLatitude = 41.72055696104794;
    double targetLongitude = -73.93346685342131;

    private bool hasSpawned = false;
    private float timer = 0f;
    public float waitTime = 20f; // Adjustable delay (in seconds)

    void Update()
    {
        if (hasSpawned) return;

        // Wait for map to initialize before spawning
        timer += Time.deltaTime;
        if (timer < waitTime) return;

        // Wait for map to be fully initialized
        if (!map.IsEditorPreviewEnabled && !map.IsAccessTokenValid)
        {
            Debug.LogWarning("Map not ready - access token invalid");
            return;
        }

        // Basic map initialization check
        if (map.InitializeOnStart && map.AbsoluteZoom <= 0)
        {
            Debug.LogWarning("Map still loading...");
            return;
        }

        // Confirm map and location are ready
        Debug.Log("Map Center: " + map.CenterLatitudeLongitude);
        Debug.Log("Map Zoom: " + map.AbsoluteZoom);
        Debug.Log("Map Scale: " + map.transform.localScale);
        
        // Calculate distance from campus center to spawn point
        Vector2d campusCenter = new Vector2d(41.721880016645834, -73.93512181104894);
        Vector2d spawnPoint = new Vector2d(targetLatitude, targetLongitude);
        double distance = Vector2d.Distance(campusCenter, spawnPoint);
        Debug.Log("Distance from campus center to spawn: " + (distance * 111320) + " meters"); // Convert to meters

        Vector2d latLon = new Vector2d(targetLatitude, targetLongitude);
        
        // Use the overload that considers map's transform
        Vector3 worldPos = map.GeoToWorldPosition(latLon, true);
        
        // Set to 10 meters high for visibility
        worldPos.y = 10f;
        
        // Create the object
        GameObject spawnedObject = Instantiate(prefabToSpawn, worldPos, Quaternion.identity);
        
        // Make sure it's a child of the map for proper scaling/positioning
        spawnedObject.transform.SetParent(map.transform, true);
        
        // Add some visual debugging
        CreateDebugMarker(worldPos);
        
        Debug.Log("---------------Object Spawned-------------");
        Debug.Log("Target Lat/Lon: " + targetLatitude + ", " + targetLongitude);
        Debug.Log("Spawned at World Position: " + worldPos);
        Debug.Log("Spawned at Local Position: " + spawnedObject.transform.localPosition);
        Debug.Log("Map Transform Position: " + map.transform.position);
        Debug.Log("Distance from map center: " + Vector3.Distance(worldPos, map.transform.position));

        hasSpawned = true;
    }

    // Helper method to get ground height at position
    private float GetGroundHeight(Vector3 position)
    {
        RaycastHit hit;
        if (Physics.Raycast(new Vector3(position.x, 1000f, position.z), Vector3.down, out hit))
        {
            Debug.Log("Ground height found at: " + hit.point.y);
            return hit.point.y;
        }
        
        // Fallback to map's Y position if no ground found
        Debug.Log("No ground found, using map Y: " + map.transform.position.y);
        return map.transform.position.y;
    }

    // Create a visible debug marker to help locate the spawn point
    private void CreateDebugMarker(Vector3 position)
    {
        GameObject debugCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        debugCube.name = "DEBUG_SpawnMarker";
        debugCube.transform.position = position + Vector3.up * 15f; // 15 units above spawn point for visibility
        debugCube.transform.localScale = Vector3.one * 5f; // Make it big and visible
        debugCube.GetComponent<Renderer>().material.color = Color.red;
        
        // Make it a child of the map
        debugCube.transform.SetParent(map.transform, true);
        
        Debug.Log("Debug marker created at: " + debugCube.transform.position);
    }
}