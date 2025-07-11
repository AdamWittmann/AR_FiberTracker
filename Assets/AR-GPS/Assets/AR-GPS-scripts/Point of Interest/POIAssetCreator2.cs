using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class POIAssetCreator2 : MonoBehaviour
{
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
            
            // Convert conduits to POI data (with lines)
            if (jsonData.conduits != null)
            {
                foreach (var conduit in jsonData.conduits)
                {
                    if (conduit != null)
                    {
                        var conduitPOIData = ConvertConduitToPOIDataWithLines(conduit);
                        if (conduitPOIData != null && conduitPOIData.Count > 0)
                        {
                            poiDataList.AddRange(conduitPOIData);
                        }
                    }
                }
            }
            
            // Create the assets
            pLab_PointOfInterestSet createdSet = CreatePOIAssets(poiDataList);
            
            // Assign to manager if available
            // var poiManager = FindObjectOfType<ARPointOfInterestManager>();
            // if (poiManager != null && createdSet != null)
            // {
            //     // Note: You'll need to check the actual property name in ARPointOfInterestManager
            //     // poiManager.pointOfInterestSet = createdSet;
            //     Debug.Log("POI Set created. Please assign it manually to ARPointOfInterestManager");
            // }
            
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error creating POI assets from JSON: {e.Message}");
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

    private List<POIData> ConvertConduitToPOIDataWithLines(Conduit conduit)
    {
        List<POIData> conduitPOIData = new List<POIData>();

        if (conduit.segment != null && conduit.segment.Count > 0)
        {
            GameObject conduitCanvasPrefab = Resources.Load<GameObject>("Prefabs/POIFiberLine");
            
            // Create the line renderer if enabled
            if (createLineRenderers)
            {
                GameObject lineObj = CreateConduitLineRenderer(conduit);
                if (lineObj != null)
                {
                    Debug.Log($"Created line renderer for conduit: {conduit.name}");
                }
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
                facingDirectionHeading = 0f, // North
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
                    facingDirectionHeading = 0f, // North
                    canvasPrefab = conduitCanvasPrefab
                });
            }
        }

        return conduitPOIData;
    }

    // Add this method to create LineRenderer GameObjects for conduits
    private GameObject CreateConduitLineRenderer(Conduit conduit)
    {
        Debug.Log($"Attempting to create line renderer for conduit: {conduit.name}");
        
        if (conduit.segment == null || conduit.segment.Count < 2)
        {
            Debug.Log($"Conduit {conduit.name} has insufficient segments: {conduit.segment?.Count ?? 0}");
            return null;
        }

        Debug.Log($"Conduit {conduit.name} has {conduit.segment.Count} segments");

        GameObject linePrefab = Resources.Load<GameObject>("Prefabs/SegmentLine");
        GameObject lineObj;
        LineRenderer lineRenderer;
        
        // Use prefab if available, otherwise create new GameObject
        if (linePrefab != null)
        {
            Debug.Log($"Using prefab for line renderer");
            lineObj = Instantiate(linePrefab);
            lineObj.name = $"ConduitLine_{conduit.name}";
            lineRenderer = lineObj.GetComponent<LineRenderer>();
            
            // If prefab doesn't have LineRenderer, add it
            if (lineRenderer == null)
            {
                Debug.Log($"Adding LineRenderer component to prefab");
                lineRenderer = lineObj.AddComponent<LineRenderer>();
            }
        }
        else
        {
            Debug.Log($"Creating new GameObject for line renderer (no prefab found)");
            // Create a GameObject for the line
            lineObj = new GameObject($"ConduitLine_{conduit.name}");
            lineRenderer = lineObj.AddComponent<LineRenderer>();
        }
        
        // Configure LineRenderer
        if (lineRenderer.material == null)
        {
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }
        lineRenderer.material.color = lineColor;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = conduit.segment.Count;
        
        
        // Convert GPS coordinates to Unity positions
        for (int i = 0; i < conduit.segment.Count; i++)
        {
            var segment = conduit.segment[i];
            Vector3 worldPos = ConvertGPSToUnityPosition(segment.lat, segment.lng);
            lineRenderer.SetPosition(i, worldPos);
            Debug.Log($"Segment {i}: GPS({segment.lat}, {segment.lng}) -> Unity({worldPos.x}, {worldPos.y}, {worldPos.z})");
        }
        
        Debug.Log($"Successfully created line renderer for conduit: {conduit.name}");
        return lineObj;
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
}
