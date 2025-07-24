using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// ScriptableObject to hold conduit line data
[CreateAssetMenu(fileName = "ConduitLineSet", menuName = "POI/Conduit Line Set")]
public class ConduitLineSet : ScriptableObject
{
    [System.Serializable]
    public class ConduitLineData
    {
        public string conduitName;
        public string description;
        public List<Vector3> worldPositions;
        public Color lineColor = Color.red;
        public float lineWidth = 0.5f;
        public Material lineMaterial;
    }

    public List<ConduitLineData> conduitLines = new List<ConduitLineData>();
    public pLab_LatLon referencePoint;
}

// Component to render conduit lines from the asset
public class ConduitLineRenderer : MonoBehaviour
{
    [Header("Conduit Line Settings")]
    [SerializeField]
    private ConduitLineSet conduitLineSet;
    
    [SerializeField]
    private Material defaultLineMaterial;
    
    [SerializeField]
    private bool createOnStart = true;

    private List<GameObject> renderedLines = new List<GameObject>();

    private void Start()
    {
        if (createOnStart && conduitLineSet != null)
        {
            RenderConduitLines();
        }
    }

    public void SetConduitLineSet(ConduitLineSet lineSet)
    {
        conduitLineSet = lineSet;
        RenderConduitLines();
    }

    public void RenderConduitLines()
    {
        // Clear existing lines
        ClearRenderedLines();

        if (conduitLineSet == null)
        {
            Debug.LogWarning("No conduit line set assigned");
            return;
        }

        foreach (var conduitData in conduitLineSet.conduitLines)
        {
            CreateLineRenderer(conduitData);
        }

        Debug.Log($"Rendered {conduitLineSet.conduitLines.Count} conduit lines");
    }

    private void CreateLineRenderer(ConduitLineSet.ConduitLineData conduitData)
    {
        if (conduitData.worldPositions == null || conduitData.worldPositions.Count < 2)
        {
            Debug.LogWarning($"Conduit {conduitData.conduitName} has insufficient positions");
            return;
        }

        GameObject lineObj = new GameObject($"ConduitLine_{conduitData.conduitName}");
        lineObj.transform.SetParent(transform);

        LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
        
        // Configure line renderer
        Material material = conduitData.lineMaterial ?? defaultLineMaterial;
        if (material == null)
        {
            material = new Material(Shader.Find("Sprites/Default"));
        }
        
        lineRenderer.material = material;
        lineRenderer.material.color = conduitData.lineColor;
        lineRenderer.startWidth = conduitData.lineWidth;
        lineRenderer.endWidth = conduitData.lineWidth;
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = conduitData.worldPositions.Count;

        // Set positions
        for (int i = 0; i < conduitData.worldPositions.Count; i++)
        {
            lineRenderer.SetPosition(i, conduitData.worldPositions[i]);
        }

        renderedLines.Add(lineObj);
    }

    public void ClearRenderedLines()
    {
        foreach (var line in renderedLines)
        {
            if (line != null)
            {
                if (Application.isPlaying)
                    Destroy(line);
                else
                    DestroyImmediate(line);
            }
        }
        renderedLines.Clear();
    }

    private void OnValidate()
    {
        if (Application.isPlaying && conduitLineSet != null)
        {
            RenderConduitLines();
        }
    }
}
public class POIAssetCreator2 : MonoBehaviour
{
    [Header("Conduit Line Asset Settings")]
    [SerializeField]
    private bool createConduitLineAsset = true;
    [SerializeField]
private string conduitLineSetName = "Generated Conduit Lines";
    [Header("Asset Creation Settings")]
    [SerializeField]
    private string assetFolderPath = "Assets/POI Assets";

    [SerializeField]
    private string poiSetName = "Generated POI Set";

    [Header("JSON File Settings")]
    [SerializeField]
    private string jsonFilePath = "/Assets/StreamingAssets/Zone_Donnelly.json";

    [Header("Line Rendering Settings")]
    [SerializeField]
    private bool createLineRenderers = true;
    [SerializeField]
    private float lineWidth = 0.5f;
    [SerializeField]
    private Color lineColor = Color.red;
    [SerializeField]
    private pLab_LatLon referencePoint; // GPS reference point for coordinate conversion

    [Space]
    [SerializeField]
    private bool showGenerateButton = true;

    // Method to create assets programmatically
    public pLab_PointOfInterestSet CreatePOIAssets(List<POIData> poiDataList)
    {
#if UNITY_EDITOR
        // Ensure the folder exists
        if (!AssetDatabase.IsValidFolder(assetFolderPath))
        {
            string parentFolder = System.IO.Path.GetDirectoryName(assetFolderPath);
            string folderName = System.IO.Path.GetFileName(assetFolderPath);
            AssetDatabase.CreateFolder(parentFolder, folderName);
        }

        // Create Point of Interest Set
        pLab_PointOfInterestSet poiSet = ScriptableObject.CreateInstance<pLab_PointOfInterestSet>();
        poiSet.PointOfInterests = new List<pLab_PointOfInterest>();

        // Create individual POIs
        foreach (var poiData in poiDataList)
        {
            pLab_PointOfInterest poi = CreateSinglePOI(poiData);
            if (poi != null)
            {
                poiSet.PointOfInterests.Add(poi);
            }
        }

        // Save the POI Set asset
        string poiSetPath = $"{assetFolderPath}/{poiSetName}_{System.DateTime.Now:yyyyMMdd_HHmmss}.asset";
        AssetDatabase.CreateAsset(poiSet, poiSetPath);

        // Save and refresh
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Created POI Set with {poiSet.PointOfInterests.Count} POIs at: {poiSetPath}");
        return poiSet;
#else
        Debug.LogWarning("POI Asset creation only works in the Unity Editor");
        return null;
#endif
    }

    private pLab_PointOfInterest CreateSinglePOI(POIData data)
    {
#if UNITY_EDITOR
        // Create the POI ScriptableObject
        pLab_PointOfInterest poi = ScriptableObject.CreateInstance<pLab_PointOfInterest>();

        // Set basic properties
        poi.PoiName = data.name;
        poi.Description = data.description;
        poi.Icon = data.icon;

        // Set GPS coordinates
        poi.Coordinates = new pLab_LatLon(data.latitude, data.longitude);

        // Set tracking distances
        poi.TrackingRadius = (int)data.trackingDistance;
        poi.CloseTrackingRadius = (int)data.closeTrackingDistance;

        // Set positioning
        poi.PositionMode = data.positionMode;
        poi.RelativeHeight = data.relativeHeight;
        poi.FacingDirectionHeading = data.facingDirectionHeading;

        // Set prefabs
        poi.ObjectPrefab = data.objectPrefab;
        poi.ModelPrefab = data.modelPrefab;
        poi.CanvasPrefab = data.canvasPrefab;

        // Save individual POI as asset
        string poiPath = $"{assetFolderPath}/POI_{data.name}_{System.DateTime.Now:yyyyMMdd_HHmmss}.asset";
        AssetDatabase.CreateAsset(poi, poiPath);

        return poi;
#else
        return null;
#endif
    }

    // Method to load JSON from file path and create assets
    public void LoadPOIsFromJSONFile(string filePath)
    {
        try
        {
            if (System.IO.File.Exists(filePath))
            {
                string jsonString = System.IO.File.ReadAllText(filePath);
                LoadPOIsFromJSONAndCreateAssets(jsonString);
            }
            else
            {
                Debug.LogError($"JSON file not found at path: {filePath}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error reading JSON file: {e.Message}");
        }
    }

    // Method to load JSON from Resources folder
    public void LoadPOIsFromJSONResource(string resourceName)
    {
        try
        {
            TextAsset jsonFile = Resources.Load<TextAsset>(resourceName);
            if (jsonFile != null)
            {
                LoadPOIsFromJSONAndCreateAssets(jsonFile.text);
            }
            else
            {
                Debug.LogError($"JSON resource not found: {resourceName}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading JSON resource: {e.Message}");
        }
    }
    [ContextMenu("Load Enclosures From JSON")]
    public void LoadEnclosuresFromJSON()
    {
        if (string.IsNullOrEmpty(jsonFilePath))
        {
            Debug.LogError("JSON file path is empty. Please set it in the inspector.");
            return;
        }

        if (!System.IO.File.Exists(jsonFilePath))
        {
            Debug.LogError($"JSON file not found at path: {jsonFilePath}");
            return;
        }

        try
        {
            string jsonString = System.IO.File.ReadAllText(jsonFilePath);
            Enclosure[] enclosures = JsonHelper.FromJson<Enclosure>(jsonString);

            if (enclosures == null || enclosures.Length == 0)
            {
                Debug.LogWarning("No enclosures found in JSON.");
                return;
            }

            List<POIData> poiDataList = new List<POIData>();
            foreach (var enclosure in enclosures)
            {
                poiDataList.Add(ConvertEnclosureToPOIData(enclosure));
            }

            CreatePOIAssets(poiDataList);
            Debug.Log($"Successfully created {poiDataList.Count} enclosure POIs from JSON.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error parsing Enclosure JSON: {ex.Message}");
        }
    }

    // Updated JSON loader method that creates assets
    public void LoadPOIsFromJSONAndCreateAssets(string jsonString)
    {
        try
        {
            JSONPOIData jsonData = JsonUtility.FromJson<JSONPOIData>(jsonString);

            if (jsonData == null)
            {
                Debug.LogError("JSON data is null after parsing");
                return;
            }

            // Auto-set reference point if not already set
            AutoSetReferencePoint(jsonData);

            List<POIData> poiDataList = new List<POIData>();
            List<Conduit> conduits = new List<Conduit>();

            // Convert manholes to POI data
            if (jsonData.manholes != null)
            {
                foreach (var manhole in jsonData.manholes)
                {
                    if (manhole != null)
                    {
                        POIData poiData = ConvertManholeToPOIData(manhole);
                        if (poiData != null)
                        {
                            poiDataList.Add(poiData);
                        }
                    }
                }
            }

           if (jsonData.conduits != null)
        {
            Debug.Log($"Processing {jsonData.conduits.Count} conduits");
            foreach (var conduit in jsonData.conduits)
            {
                if (conduit != null)
                {
                    // Add conduit POI data (start/end/mid points) - using your existing method
                    var conduitPOIData = ConvertConduitToPOIDataWithLines(conduit);
                    if (conduitPOIData != null && conduitPOIData.Count > 0)
                    {
                        poiDataList.AddRange(conduitPOIData);
                    }

                    // Collect conduit for line asset creation
                    conduits.Add(conduit);
                }
            }
        }

        // Create POI assets
        pLab_PointOfInterestSet poiSet = CreatePOIAssets(poiDataList);

        // Create conduit line asset
        ConduitLineSet conduitLineSet = null;
        if (createConduitLineAsset && conduits != null && conduits.Count > 0)
        {
            conduitLineSet = CreateConduitLineAsset(conduits);
        }

        Debug.Log($"Created POI Set with {poiDataList.Count} POIs and Conduit Line Set with {conduits?.Count ?? 0} conduits");

    }
    catch (System.Exception e)
    {
        Debug.LogError($"Error creating POI assets from JSON: {e.Message}\nStack trace: {e.StackTrace}");
    }
}

    private POIData ConvertManholeToPOIData(Manhole manhole)
    {
        if (!double.TryParse(manhole.Latitude, out double lat) ||
            !double.TryParse(manhole.Longitude, out double lon))
        {
            Debug.LogWarning($"Invalid coordinates for manhole {manhole.mid}");
            return null;
        }
        //Load identifiers in resources/prefabs folder
        GameObject manholeCanvasPrefab = Resources.Load<GameObject>("Prefabs/POIManhole");
        Sprite manholeIcon = Resources.Load<Sprite>("Prefabs/POIManholeIcon");

        return new POIData
        {
            name = $"Manhole {manhole.mid}",
            description = manhole.description,
            latitude = lat,
            longitude = lon,
            trackingDistance = 75f,
            closeTrackingDistance = 15f,
            positionMode = POIPositionMode.AlignWithGround,
            relativeHeight = 0f,
            facingDirectionHeading = 0f, // North
            // You'll need to assign these in the inspector or set defaults
            icon = manholeIcon, // Set default icon
            objectPrefab = null, // Set default prefab
            modelPrefab = null, // Set default prefab
            canvasPrefab = manholeCanvasPrefab // Set manhole prefab
        };
    }
    private POIData ConvertEnclosureToPOIData(Enclosure enclosure)
    {
        GameObject prefab = Resources.Load<GameObject>("Prefabs/POIManhole"); // or a custom one if needed

        return new POIData
        {
            name = enclosure.name,
            description = enclosure.notes,
            latitude = enclosure.gps_coordinates.latitude,
            longitude = enclosure.gps_coordinates.longitude,
            trackingDistance = 75f,
            closeTrackingDistance = 15f,
            positionMode = POIPositionMode.AlignWithGround,
            relativeHeight = 0f,
            facingDirectionHeading = 0f,
            canvasPrefab = prefab
        };
    }

    private List<POIData> ConvertConduitToPOIDataWithLines(Conduit conduit)
{
    List<POIData> conduitPOIData = new List<POIData>();

    if (conduit.segment == null || conduit.segment.Count == 0)
    {
        Debug.LogWarning($"Conduit {conduit.name} has no segments");
        return conduitPOIData;
    }

    Debug.Log($"Processing conduit: {conduit.name} with {conduit.segment.Count} segments");

    GameObject conduitCanvasPrefab = Resources.Load<GameObject>("Prefabs/POIFiberLine");
    if (conduitCanvasPrefab == null)
    {
        Debug.LogWarning("POIFiberLine prefab not found in Resources/Prefabs/");
    }

    // Start point
    conduitPOIData.Add(new POIData
    {
        name = $"{conduit.name} Start",
        description = $"{conduit.description}\nNotes: {conduit.segment[0].notes}",
        latitude = conduit.segment[0].lat,
        longitude = conduit.segment[0].lng,
        trackingDistance = 75f,
        closeTrackingDistance = 15f,
        positionMode = POIPositionMode.AlignWithGround,
        relativeHeight = 0f,
        facingDirectionHeading = 0f,
        canvasPrefab = conduitCanvasPrefab
    });

    // End point (if more than one segment)
    if (conduit.segment.Count > 1)
    {
        var lastSegment = conduit.segment[conduit.segment.Count - 1];
        conduitPOIData.Add(new POIData
        {
            name = $"{conduit.name} End",
            description = $"{conduit.description}\nNotes: {lastSegment.notes}",
            latitude = lastSegment.lat,
            longitude = lastSegment.lng,
            trackingDistance = 75f,
            closeTrackingDistance = 15f,
            positionMode = POIPositionMode.AlignWithGround,
            relativeHeight = 0f,
            facingDirectionHeading = 0f,
            canvasPrefab = conduitCanvasPrefab
        });

        // Midpoint (if more than 2 segments)
        if (conduit.segment.Count > 2)
        {
            int midIndex = conduit.segment.Count / 2;
            var mid = conduit.segment[midIndex];
            conduitPOIData.Add(new POIData
            {
                name = $"{conduit.name} Midpoint",
                description = $"Midpoint of {conduit.name}\nNotes: {mid.notes}",
                latitude = mid.lat,
                longitude = mid.lng,
                trackingDistance = 75f,
                closeTrackingDistance = 15f,
                positionMode = POIPositionMode.AlignWithGround,
                relativeHeight = 0f,
                facingDirectionHeading = 0f,
                canvasPrefab = conduitCanvasPrefab
            });
        }
    }

    return conduitPOIData;
}

    // Add this method to create LineRenderer GameObjects for conduits
    // Create conduit line asset
private ConduitLineSet CreateConduitLineAsset(List<Conduit> conduits)
{
#if UNITY_EDITOR
    if (referencePoint == null)
    {
        Debug.LogError("Reference point must be set to create conduit line assets");
        return null;
    }

    // Ensure the folder exists
    if (!AssetDatabase.IsValidFolder(assetFolderPath))
    {
        string parentFolder = System.IO.Path.GetDirectoryName(assetFolderPath);
        string folderName = System.IO.Path.GetFileName(assetFolderPath);
        AssetDatabase.CreateFolder(parentFolder, folderName);
    }

    // Create the conduit line set
    ConduitLineSet conduitLineSet = ScriptableObject.CreateInstance<ConduitLineSet>();
    conduitLineSet.referencePoint = referencePoint;
    conduitLineSet.conduitLines = new List<ConduitLineSet.ConduitLineData>();

    foreach (var conduit in conduits)
    {
        if (conduit.segment == null || conduit.segment.Count < 2)
        {
            Debug.LogWarning($"Skipping conduit {conduit.name} - insufficient segments");
            continue;
        }

        var conduitLineData = new ConduitLineSet.ConduitLineData
        {
            conduitName = conduit.name,
            description = conduit.description,
            lineColor = lineColor,
            lineWidth = lineWidth,
            worldPositions = new List<Vector3>()
        };

        // Convert GPS coordinates to world positions
        foreach (var segment in conduit.segment)
        {
            try
            {
                Vector3 worldPos = ConvertGPSToUnityPosition(segment.lat, segment.lng);
                conduitLineData.worldPositions.Add(worldPos);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error converting GPS to Unity position for conduit {conduit.name}: {e.Message}");
            }
        }

        if (conduitLineData.worldPositions.Count >= 2)
        {
            conduitLineSet.conduitLines.Add(conduitLineData);
            Debug.Log($"Added conduit {conduit.name} with {conduitLineData.worldPositions.Count} positions");
        }
    }

    // Save the conduit line set asset
    string conduitLineSetPath = $"{assetFolderPath}/{conduitLineSetName}_{System.DateTime.Now:yyyyMMdd_HHmmss}.asset";
    AssetDatabase.CreateAsset(conduitLineSet, conduitLineSetPath);
    
    // Save and refresh
    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();
    
    Debug.Log($"Created Conduit Line Set with {conduitLineSet.conduitLines.Count} conduits at: {conduitLineSetPath}");
    return conduitLineSet;
#else
    return null;
#endif
}

    // GPS to Unity position conversion using your existing pLab_GeoTools
    private Vector3 ConvertGPSToUnityPosition(double lat, double lng)
    {
        // Create a LatLon point for the target coordinates
        pLab_LatLon targetPoint = new pLab_LatLon(lat, lng);

        // Get the UTM difference from your reference point
        Vector2 utmDifference = pLab_GeoTools.UTMDifferenceBetweenPoints(referencePoint, targetPoint);

        // Convert to Unity coordinates (UTM X = Unity X, UTM Y = Unity Z, Unity Y = 0 for ground level)
        return new Vector3(utmDifference.x, 0f, utmDifference.y);
    }

    // Method to set the reference point (call this before creating line renderers)
    public void SetReferencePoint(double lat, double lng)
    {
        referencePoint = new pLab_LatLon(lat, lng);
    }

    // Auto-set reference point from first manhole or conduit segment
    private void AutoSetReferencePoint(JSONPOIData jsonData)
    {
        if (referencePoint == null)
        {
            // Try to get reference from first manhole
            if (jsonData.manholes != null && jsonData.manholes.Count > 0)
            {
                var firstManhole = jsonData.manholes[0];
                if (double.TryParse(firstManhole.Latitude, out double lat) &&
                    double.TryParse(firstManhole.Longitude, out double lng))
                {
                    SetReferencePoint(lat, lng);
                    Debug.Log($"Auto-set reference point from first manhole: {lat}, {lng}");
                    return;
                }
            }

            // Try to get reference from first conduit segment
            if (jsonData.conduits != null && jsonData.conduits.Count > 0)
            {
                var firstConduit = jsonData.conduits[0];
                if (firstConduit.segment != null && firstConduit.segment.Count > 0)
                {
                    SetReferencePoint(firstConduit.segment[0].lat, firstConduit.segment[0].lng);
                    Debug.Log($"Auto-set reference point from first conduit: {firstConduit.segment[0].lat}, {firstConduit.segment[0].lng}");
                    return;
                }
            }

            Debug.LogWarning("Could not auto-set reference point. Please set it manually.");
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Generate POI Assets from JSON")]
    public void GeneratePOIAssetsFromJSON()
    {
        if (string.IsNullOrEmpty(jsonFilePath))
        {
            Debug.LogError("JSON file path is empty. Please set the file path in the inspector.");
            return;
        }

        if (!System.IO.File.Exists(jsonFilePath))
        {
            Debug.LogError($"JSON file not found at path: {jsonFilePath}");
            return;
        }

        try
        {
            string jsonString = System.IO.File.ReadAllText(jsonFilePath);
            LoadPOIsFromJSONAndCreateAssets(jsonString);
            Debug.Log($"Successfully generated POI assets from: {jsonFilePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error generating POI assets: {e.Message}");
        }
    }

    [ContextMenu("Create Sample POI Assets")]
    public void CreateSamplePOIAssets()
    {
        List<POIData> sampleData = new List<POIData>
        {
            new POIData
            {
                name = "Sample POI 1",
                description = "This is a sample POI",
                latitude = 60.1699,
                longitude = 24.9384,
                trackingDistance = 75f,
                closeTrackingDistance = 15f,
                positionMode = POIPositionMode.AlignWithGround,
                relativeHeight = 0f,
                facingDirectionHeading = 0f
            }
        };

        CreatePOIAssets(sampleData);
    }
#endif
}

// Data structure to hold POI information
[System.Serializable]
public class POIData
{
    public string name;
    public string description;
    public double latitude;
    public double longitude;
    public float trackingDistance;
    public float closeTrackingDistance;
    public POIPositionMode positionMode;
    public float relativeHeight;
    public float facingDirectionHeading;
    public Sprite icon;
    public GameObject objectPrefab;
    public GameObject modelPrefab;
    public GameObject canvasPrefab;
    public GameObject manholeCanvasPrefab;
    public GameObject conduitCanvasPrefab;
    public List<Conduit> conduits;
}
[System.Serializable]
public class Enclosure
{
    public string id;
    public string name;
    public GPS_Coordinates gps_coordinates;
    public Directions directions;
    public string notes;
}
[System.Serializable]
public class GPS_Coordinates
{
    public double latitude;
    public double longitude;

}
[System.Serializable]
public class Directions
{
    public List<string> North;
    public List<string> South;
    public List<string> East;
    public List<string> West;
    public List<string> NorthEast;
    public List<string> NorthWest;
    public List<string> SouthEast;
    public List<string> SouthWest;
}
[System.Serializable]
public class ConduitData
{
    public string name;             // Example: "Line 5"
    public List<SegmentPoint> segment; // List of points forming the path
}

[System.Serializable]
public class SegmentPoint
{
    public double lat;              // Latitude
    public double lng;              // Longitude
}

public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        string newJson = "{\"array\":" + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
        return wrapper.array;
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] array;
    }
}