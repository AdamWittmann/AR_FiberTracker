
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class JSONPOILoader : MonoBehaviour
{
    [Header("JSON Loading")]
    [SerializeField]
    private string jsonFileName = "Zone_Donnelly.json";
    
    [Header("POI Configuration")]
    [SerializeField]
    private GameObject defaultManholePrefab;
    
    [SerializeField]
    private GameObject defaultConduitPrefab;
    
    [SerializeField]
    private GameObject defaultCanvasPrefab;
    
    [SerializeField]
    private Sprite defaultManholeIcon;
    
    [SerializeField]
    private Sprite defaultConduitIcon;
    
    [Header("Tracking Settings")]
    [SerializeField]
    private int defaultTrackingRadius = 250;
    
    [SerializeField]
    private int defaultCloseTrackingRadius = 7;
    
    [Header("Manager Reference")]
    [SerializeField]
    private pLab_ARPointOfInterestManager poiManager;

    private pLab_PointOfInterestSet currentPOISet;

    private void Start()
    {
        LoadPOIsFromJSON();
    }

    public void LoadPOIsFromJSON()
    {
        string jsonPath = Path.Combine(Application.streamingAssetsPath, jsonFileName);
        
        if (File.Exists(jsonPath))
        {
            string jsonContent = File.ReadAllText(jsonPath);
            LoadPOIsFromJSONString(jsonContent);
        }
        else
        {
            Debug.LogError($"JSON file not found at: {jsonPath}");
            Debug.LogError("Make sure to place your JSON file in the StreamingAssets folder");
        }
    }

    public void LoadPOIsFromJSONString(string jsonString)
    {
        try
        {
            Debug.Log($"JSON Content: {jsonString}");
            JSONPOIData jsonData = JsonUtility.FromJson<JSONPOIData>(jsonString);
            
            if (jsonData == null)
            {
                Debug.LogError("JSON data is null after parsing");
                return;
            }
            
            Debug.Log($"Parsed JSON - Manholes: {(jsonData.manholes?.Count ?? 0)}, Conduits: {(jsonData.conduits?.Count ?? 0)}");
            CreatePOIsFromJSONData(jsonData);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing JSON: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
        }
    }

    private void CreatePOIsFromJSONData(JSONPOIData jsonData)
    {
        if (jsonData == null)
        {
            Debug.LogError("JSONPOIData is null");
            return;
        }

        // Create a new POI set
        currentPOISet = ScriptableObject.CreateInstance<pLab_PointOfInterestSet>();
        currentPOISet.PointOfInterests = new List<pLab_PointOfInterest>();

        // Create POIs for manholes
        if (jsonData.manholes != null)
        {
            Debug.Log($"Processing {jsonData.manholes.Count} manholes");
            foreach (var manhole in jsonData.manholes)
            {
                if (manhole != null)
                {
                    pLab_PointOfInterest poi = CreateManholesPOI(manhole);
                    if (poi != null)
                    {
                        currentPOISet.PointOfInterests.Add(poi);
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("No manholes found in JSON data");
        }

        // Create POIs for conduit segments (optional - you might want POIs at key points)
        if (jsonData.conduits != null)
        {
            Debug.Log($"Processing {jsonData.conduits.Count} conduits");
            foreach (var conduit in jsonData.conduits)
            {
                if (conduit != null)
                {
                    var conduitPOIs = CreateConduitPOIs(conduit);
                    if (conduitPOIs != null && conduitPOIs.Count > 0)
                    {
                        currentPOISet.PointOfInterests.AddRange(conduitPOIs);
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("No conduits found in JSON data");
        }

        // Assign the POI set to the manager
        if (poiManager != null)
        {
            poiManager.PointOfInterestSet = currentPOISet;
        }
        else
        {
            Debug.LogError("POI Manager is not assigned!");
        }

        Debug.Log($"Loaded {currentPOISet.PointOfInterests.Count} POIs from JSON");
    }

    private pLab_PointOfInterest CreateManholesPOI(Manhole manhole)
    {
        pLab_PointOfInterest poi = ScriptableObject.CreateInstance<pLab_PointOfInterest>();
        
        // Set basic info
        poi.PoiName = $"Manhole {manhole.mid}";
        poi.Description = manhole.description;
        poi.Icon = defaultManholeIcon;

        // Set prefabs
        poi.ModelPrefab = defaultManholePrefab;
        poi.CanvasPrefab = defaultCanvasPrefab;

        // Set coordinates - Create new pLab_LatLon instance
        poi.Coordinates = new pLab_LatLon();
        
        // Parse and assign coordinates using the correct property names
        if (double.TryParse(manhole.Latitude, out double lat))
        {
            poi.Coordinates.Lat = lat;
        }
        else
        {
            Debug.LogWarning($"Failed to parse latitude for manhole {manhole.mid}: {manhole.Latitude}");
        }
        
        if (double.TryParse(manhole.Longitude, out double lon))
        {
            poi.Coordinates.Lon = lon;
        }
        else
        {
            Debug.LogWarning($"Failed to parse longitude for manhole {manhole.mid}: {manhole.Longitude}");
        }

        // Set tracking settings
        poi.TrackingRadius = defaultTrackingRadius;
        poi.CloseTrackingRadius = defaultCloseTrackingRadius;
        
        // Set position mode
        poi.PositionMode = POIPositionMode.AlignWithGround;

        return poi;
    }

    private List<pLab_PointOfInterest> CreateConduitPOIs(Conduit conduit)
    {
        List<pLab_PointOfInterest> conduitPOIs = new List<pLab_PointOfInterest>();

        // Create POIs for start and end points of conduit (you can modify this logic)
        if (conduit.segment != null && conduit.segment.Count > 0)
        {
            // Start point
            var startPOI = CreateConduitPOI(conduit, conduit.segment[0], $"{conduit.name} Start");
            conduitPOIs.Add(startPOI);

            // End point (if more than one segment)
            if (conduit.segment.Count > 1)
            {
                var endPOI = CreateConduitPOI(conduit, conduit.segment[conduit.segment.Count - 1], $"{conduit.name} End");
                conduitPOIs.Add(endPOI);
            }
        }

        return conduitPOIs;
    }

    private pLab_PointOfInterest CreateConduitPOI(Conduit conduit, Segment segment, string poiName)
    {
        pLab_PointOfInterest poi = ScriptableObject.CreateInstance<pLab_PointOfInterest>();
        
        // Set basic info
        poi.PoiName = poiName;
        poi.Description = $"{conduit.description}\nNotes: {segment.notes}";
        poi.Icon = defaultConduitIcon;

        // Set prefabs
        poi.ModelPrefab = defaultConduitPrefab;
        poi.CanvasPrefab = defaultCanvasPrefab;

        // Set coordinates - Create new pLab_LatLon instance
        poi.Coordinates = new pLab_LatLon();
        poi.Coordinates.Lat = segment.lat;
        poi.Coordinates.Lon = segment.lng;

        // Set tracking settings
        poi.TrackingRadius = defaultTrackingRadius;
        poi.CloseTrackingRadius = defaultCloseTrackingRadius;
        
        // Set position mode
        poi.PositionMode = POIPositionMode.AlignWithGround;

        return poi;
    }

    // Alternative method if pLab_LatLon has different property names
    private void SetCoordinatesAlternative(pLab_PointOfInterest poi, double lat, double lon)
    {
        poi.Coordinates = new pLab_LatLon();
        
        // Try common property name variations
        var coordsType = typeof(pLab_LatLon);
        
        // Try setting latitude
        var latField = coordsType.GetField("Lat") ?? coordsType.GetField("latitude") ?? coordsType.GetField("Latitude") ?? coordsType.GetField("lat");
        var latProp = coordsType.GetProperty("Lat") ?? coordsType.GetProperty("latitude") ?? coordsType.GetProperty("Latitude") ?? coordsType.GetProperty("lat");
        
        if (latField != null)
            latField.SetValue(poi.Coordinates, lat);
        else if (latProp != null && latProp.CanWrite)
            latProp.SetValue(poi.Coordinates, lat);
        
        // Try setting longitude
        var lonField = coordsType.GetField("Lon") ?? coordsType.GetField("longitude") ?? coordsType.GetField("Longitude") ?? coordsType.GetField("lon") ?? coordsType.GetField("lng");
        var lonProp = coordsType.GetProperty("Lon") ?? coordsType.GetProperty("longitude") ?? coordsType.GetProperty("Longitude") ?? coordsType.GetProperty("lon") ?? coordsType.GetProperty("lng");
        
        if (lonField != null)
            lonField.SetValue(poi.Coordinates, lon);
        else if (lonProp != null && lonProp.CanWrite)
            lonProp.SetValue(poi.Coordinates, lon);
    }

    // Optional: Method to create POIs for all conduit segments
    public void CreatePOIsForAllConduitSegments()
    {
        // This would create a POI for every segment point - might be too many
        // Use this if you want to visualize the entire conduit path
    }

    // Optional: Save the created POI set as an asset
    #if UNITY_EDITOR
    [ContextMenu("Save POI Set as Asset")]
    public void SavePOISetAsAsset()
    {
        if (currentPOISet != null)
        {
            UnityEditor.AssetDatabase.CreateAsset(currentPOISet, $"Assets/POI_Set_{System.DateTime.Now.Ticks}.asset");
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log("POI Set saved as asset");
        }
    }
    #endif
}