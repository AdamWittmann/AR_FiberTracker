using System.Collections.Generic;
using UnityEngine;

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

        // Runtime cached data for dynamic positioning (not serialized)
        [System.NonSerialized]
        public List<Vector3> relativePositions; // Positions relative to camera at last GPS update
        [System.NonSerialized]
        public Vector3 lastCameraPosition; // Camera position when positions were calculated
    }

    public List<ConduitLineData> conduitLines = new List<ConduitLineData>();
    public pLab_LatLon referencePoint;
}

// Component to render conduit lines from the asset
public class ConduitLineRenderer : MonoBehaviour
{
    [Header("Conduit Line Set Assignment")]
    [SerializeField]
    [Tooltip("Assign the ConduitLineSet generated from POIAssetCreator2")]
    private ConduitLineSet conduitLineSet;

    [Header("AR Integration")]
    [SerializeField]
    [Tooltip("AR True North Finder for heading updates")]
    private pLab_ARTrueNorthFinder arTrueNorthFinder;

    [SerializeField]
    [Tooltip("Location provider for GPS updates")]
    private pLab_LocationProvider locationProvider;

    [SerializeField]
    [Tooltip("AR Camera for position calculations")]
    private Camera arCamera;

    [SerializeField]
    [Tooltip("Device elevation estimator for ground level calculation")]
    private pLab_ARDeviceElevationEstimater deviceElevationEstimater;

    [Header("Rendering Settings")]
    [SerializeField]
    [Tooltip("Default material to use if conduit doesn't have one assigned")]
    private Material defaultLineMaterial;

    [SerializeField]
    [Tooltip("Automatically render lines when the component starts")]
    private bool createOnStart = true;

    [SerializeField]
    [Tooltip("Show/hide all rendered lines")]
    private bool showLines = true;

    [Header("Line Positioning")]
    [SerializeField]
    [Tooltip("Height offset from ground level (negative = below ground, positive = above). Default: -1.524m (~5 feet below)")]
    private float groundHeightOffset = -1.524f; // ~5 feet below ground

    [SerializeField]
    [Tooltip("Maximum distance to render lines (in meters). Lines beyond this distance will be hidden. Default: 15.24m (~50 feet)")]
    private float trackingRadius = 15.24f; // ~50 feet

    [SerializeField]
    [Tooltip("Distance margin before hiding lines (prevents flickering at boundary)")]
    private float trackingExitMargin = 3f; // Extra 3 meters before hiding

    [Header("Debug Info")]
    [SerializeField]
    [Tooltip("Show debug information about rendered lines")]
    private bool showDebugInfo = false;

    // Runtime data
    private List<GameObject> renderedLines = new List<GameObject>();
    private Transform arCameraTransform;
    private double previousUpdateTimestamp = 0;
    private float previousUpdateAccuracy = 999f; // Track GPS accuracy to prevent drift from poor readings

    private void Start()
    {
        if (arCamera != null)
        {
            arCameraTransform = arCamera.transform;
        }

        if (createOnStart && conduitLineSet != null)
        {
            RenderConduitLines();
        }
    }

    private void OnEnable()
    {
        // Subscribe to heading updates (compass changes)
        if (arTrueNorthFinder != null)
        {
            arTrueNorthFinder.OnHeadingUpdated += OnNorthHeadingUpdated;
        }

        // Subscribe to GPS location updates
        if (locationProvider != null)
        {
            locationProvider.OnLocationUpdated += OnLocationUpdated;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        if (arTrueNorthFinder != null)
        {
            arTrueNorthFinder.OnHeadingUpdated -= OnNorthHeadingUpdated;
        }

        if (locationProvider != null)
        {
            locationProvider.OnLocationUpdated -= OnLocationUpdated;
        }
    }

    /// <summary>
    /// Assign a new ConduitLineSet and automatically render the lines
    /// </summary>
    /// <param name="lineSet">The ConduitLineSet to render</param>
    public void SetConduitLineSet(ConduitLineSet lineSet)
    {
        conduitLineSet = lineSet;
        RenderConduitLines();
    }

    /// <summary>
    /// Main method to render all conduit lines from the assigned set
    /// </summary>
    [ContextMenu("Render Conduit Lines")]
    public void RenderConduitLines()
    {
        // Clear existing lines first
        ClearRenderedLines();

        if (conduitLineSet == null)
        {
            Debug.LogWarning("No conduit line set assigned. Please assign a ConduitLineSet in the inspector or via SetConduitLineSet()");
            return;
        }

        if (conduitLineSet.conduitLines == null || conduitLineSet.conduitLines.Count == 0)
        {
            Debug.LogWarning("ConduitLineSet contains no conduit lines to render");
            return;
        }

        int successfullyRendered = 0;
        foreach (var conduitData in conduitLineSet.conduitLines)
        {
            if (CreateLineRenderer(conduitData))
            {
                successfullyRendered++;
            }
        }

        Debug.Log($"Successfully rendered {successfullyRendered} out of {conduitLineSet.conduitLines.Count} conduit lines");
        
        // Apply visibility setting
        SetLinesVisibility(showLines);
    }

    /// <summary>
    /// Create a LineRenderer for a single conduit line
    /// </summary>
    /// <param name="conduitData">The conduit data to render</param>
    /// <returns>True if successfully created, false otherwise</returns>
    private bool CreateLineRenderer(ConduitLineSet.ConduitLineData conduitData)
    {
        if (conduitData.worldPositions == null || conduitData.worldPositions.Count < 2)
        {
            Debug.LogWarning($"Conduit '{conduitData.conduitName}' has insufficient positions ({conduitData.worldPositions?.Count ?? 0} positions). Need at least 2 points to create a line.");
            return false;
        }

        // Create the line object
        GameObject lineObj = new GameObject($"ConduitLine_{conduitData.conduitName}");
        lineObj.transform.SetParent(transform);

        // Add and configure LineRenderer component
        LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();

        // Set material
        Material material = conduitData.lineMaterial ?? defaultLineMaterial;
        if (material == null)
        {
            // Create a default material if none provided
            material = new Material(Shader.Find("Sprites/Default"));
            if (showDebugInfo)
            {
                Debug.Log($"Using default Sprites/Default shader for conduit '{conduitData.conduitName}'");
            }
        }

        lineRenderer.material = material;
        lineRenderer.material.color = conduitData.lineColor;
        lineRenderer.startWidth = conduitData.lineWidth;
        lineRenderer.endWidth = conduitData.lineWidth;
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = conduitData.worldPositions.Count;

        // Initialize relative positions cache - store positions relative to camera
        Vector3 currentARCameraPosition = arCameraTransform != null ? arCameraTransform.position : Vector3.zero;
        currentARCameraPosition.y = 0; // Only care about XZ plane

        conduitData.relativePositions = new List<Vector3>();
        conduitData.lastCameraPosition = currentARCameraPosition;

        // Set all positions along the line and cache relative positions
        for (int i = 0; i < conduitData.worldPositions.Count; i++)
        {
            lineRenderer.SetPosition(i, conduitData.worldPositions[i]);
            // Store position relative to camera (same as POI system)
            conduitData.relativePositions.Add(conduitData.worldPositions[i] - currentARCameraPosition);
        }

        // Store reference for management
        renderedLines.Add(lineObj);

        if (showDebugInfo)
        {
            Debug.Log($"Created line renderer for '{conduitData.conduitName}' with {conduitData.worldPositions.Count} points");
        }

        return true;
    }

    /// <summary>
    /// Clear all currently rendered lines
    /// </summary>
    [ContextMenu("Clear All Lines")]
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
        
        if (showDebugInfo)
        {
            Debug.Log("Cleared all rendered conduit lines");
        }
    }

    /// <summary>
    /// Show or hide all rendered lines
    /// </summary>
    /// <param name="visible">True to show lines, false to hide them</param>
    public void SetLinesVisibility(bool visible)
    {
        showLines = visible;
        foreach (var line in renderedLines)
        {
            if (line != null)
            {
                line.SetActive(visible);
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"Set conduit lines visibility to: {visible}");
        }
    }

    /// <summary>
    /// Toggle visibility of all lines
    /// </summary>
    [ContextMenu("Toggle Lines Visibility")]
    public void ToggleLinesVisibility()
    {
        SetLinesVisibility(!showLines);
    }

    /// <summary>
    /// Get information about currently rendered lines
    /// </summary>
    /// <returns>Number of active rendered lines</returns>
    public int GetRenderedLineCount()
    {
        int activeCount = 0;
        foreach (var line in renderedLines)
        {
            if (line != null && line.activeInHierarchy)
            {
                activeCount++;
            }
        }
        return activeCount;
    }

    /// <summary>
    /// Update line properties for a specific conduit
    /// </summary>
    /// <param name="conduitName">Name of the conduit to update</param>
    /// <param name="newColor">New color for the line</param>
    /// <param name="newWidth">New width for the line</param>
    public void UpdateConduitLineProperties(string conduitName, Color newColor, float newWidth)
    {
        foreach (var line in renderedLines)
        {
            if (line != null && line.name == $"ConduitLine_{conduitName}")
            {
                LineRenderer lr = line.GetComponent<LineRenderer>();
                if (lr != null)
                {
                    lr.material.color = newColor;
                    lr.startWidth = newWidth;
                    lr.endWidth = newWidth;
                    
                    if (showDebugInfo)
                    {
                        Debug.Log($"Updated properties for conduit '{conduitName}': Color={newColor}, Width={newWidth}");
                    }
                }
                break;
            }
        }
    }

    /// <summary>
    /// Event handler for GPS location updates - recalculates line positions
    /// </summary>
    private void OnLocationUpdated(object sender, pLab_LocationUpdatedEventArgs e)
    {
        if (conduitLineSet == null || conduitLineSet.conduitLines == null) return;

        // GPS accuracy filtering - prevents drift from poor GPS readings
        // Uses same logic as pLab_ARPointOfInterestManager to maintain consistency
        float accuracy = Mathf.Max(e.horizontalAccuracy, e.verticalAccuracy);
        float deltaAccuracy = accuracy - previousUpdateAccuracy;

        // Only update if GPS accuracy is good (≤8m) or improving significantly (≥5m improvement)
        bool shouldUpdate = (accuracy <= 8f || deltaAccuracy <= -5f);

        if (!shouldUpdate)
        {
            if (showDebugInfo)
            {
                Debug.Log($"Skipping line update - GPS accuracy insufficient ({accuracy:F1}m, previous: {previousUpdateAccuracy:F1}m)");
            }
            return;
        }

        // Track accuracy for next comparison
        previousUpdateAccuracy = accuracy;

        Vector3 currentARCameraPosition = arCameraTransform != null ? arCameraTransform.position : Vector3.zero;
        currentARCameraPosition.y = 0; // Only XZ plane

        float trueNorthHeadingDifference = arTrueNorthFinder != null ? arTrueNorthFinder.Heading : 0;

        // Update all line positions based on new GPS location
        for (int i = 0; i < conduitLineSet.conduitLines.Count; i++)
        {
            var conduitData = conduitLineSet.conduitLines[i];
            if (conduitData.relativePositions == null || conduitData.relativePositions.Count == 0) continue;

            // Update cached camera position
            conduitData.lastCameraPosition = currentARCameraPosition;

            // Find the corresponding line renderer
            if (i < renderedLines.Count && renderedLines[i] != null)
            {
                GameObject lineObj = renderedLines[i];
                LineRenderer lr = lineObj.GetComponent<LineRenderer>();
                if (lr != null)
                {
                    // Update line positions
                    UpdateLinePositions(lr, conduitData, currentARCameraPosition, trueNorthHeadingDifference);

                    // Update visibility based on distance
                    UpdateLineVisibility(lineObj, conduitData, currentARCameraPosition);
                }
            }
        }

        if (showDebugInfo)
        {
            Debug.Log($"Updated {conduitLineSet.conduitLines.Count} conduit lines based on GPS update");
        }
    }

    /// <summary>
    /// Event handler for compass heading updates - rotates lines around north axis
    /// Same pattern as RotatePOIsRelativeToNorth in pLab_ARPointOfInterestManager
    /// </summary>
    private void OnNorthHeadingUpdated(object sender, pLab_NorthHeadingUpdatedEventArgs e)
    {
        if (conduitLineSet == null || conduitLineSet.conduitLines == null) return;

        // Only update every 5 seconds or if priority update (same as POI system)
        if (!e.isPriority && System.TimeSpan.FromMilliseconds(e.timestamp - previousUpdateTimestamp).TotalSeconds <= 5f)
        {
            return;
        }

        previousUpdateTimestamp = e.timestamp;

        Vector3 currentARCameraPosition = arCameraTransform != null ? arCameraTransform.position : Vector3.zero;
        currentARCameraPosition.y = 0;

        // Rotate all lines relative to new north heading
        for (int i = 0; i < conduitLineSet.conduitLines.Count; i++)
        {
            var conduitData = conduitLineSet.conduitLines[i];
            if (conduitData.relativePositions == null || conduitData.relativePositions.Count == 0) continue;

            // Find the corresponding line renderer
            if (i < renderedLines.Count && renderedLines[i] != null)
            {
                GameObject lineObj = renderedLines[i];
                LineRenderer lr = lineObj.GetComponent<LineRenderer>();
                if (lr != null)
                {
                    // Update line positions with new heading
                    UpdateLinePositions(lr, conduitData, conduitData.lastCameraPosition, e.heading);

                    // Update visibility based on distance (camera may have moved since last GPS update)
                    UpdateLineVisibility(lineObj, conduitData, currentARCameraPosition);
                }
            }
        }

        if (showDebugInfo)
        {
            Debug.Log($"Rotated {conduitLineSet.conduitLines.Count} conduit lines to heading: {e.heading:F1}°");
        }
    }

    /// <summary>
    /// Update line positions based on camera position and heading
    /// Mirrors the logic in pLab_ARPointOfInterestManager.RecheckPOITrackings
    /// </summary>
    private void UpdateLinePositions(LineRenderer lineRenderer, ConduitLineSet.ConduitLineData conduitData,
        Vector3 cameraPosition, float trueNorthHeadingDifference)
    {
        // Get ground level for Y-axis positioning
        float groundLevel = deviceElevationEstimater != null ? deviceElevationEstimater.GroundLevelEstimate : 0f;
        float lineHeight = groundLevel + groundHeightOffset;

        for (int i = 0; i < conduitData.relativePositions.Count; i++)
        {
            // Calculate new world position: camera position + rotated relative position
            // This is exactly how POIs are positioned (see line 440 in pLab_ARPointOfInterestManager)
            Vector3 newPos = cameraPosition + (Quaternion.AngleAxis(trueNorthHeadingDifference, Vector3.up) * conduitData.relativePositions[i]);

            // Set Y position to ground level + offset (e.g., 5 feet below ground)
            newPos.y = lineHeight;

            lineRenderer.SetPosition(i, newPos);
        }
    }

    /// <summary>
    /// Check if any segment of the line is within tracking radius
    /// </summary>
    private bool IsLineWithinTrackingRadius(ConduitLineSet.ConduitLineData conduitData, Vector3 cameraPosition)
    {
        if (conduitData.relativePositions == null || conduitData.relativePositions.Count == 0)
            return false;

        // Check if any point on the line is within tracking radius
        foreach (var relativePos in conduitData.relativePositions)
        {
            // Calculate world position (ignore Y for distance check)
            Vector3 worldPos = conduitData.lastCameraPosition + relativePos;
            worldPos.y = 0;
            Vector3 camPosFlat = cameraPosition;
            camPosFlat.y = 0;

            float distance = Vector3.Distance(worldPos, camPosFlat);

            if (distance <= trackingRadius)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Update visibility of a specific line based on distance
    /// </summary>
    private void UpdateLineVisibility(GameObject lineObj, ConduitLineSet.ConduitLineData conduitData, Vector3 cameraPosition)
    {
        if (lineObj == null || conduitData == null) return;

        bool isWithinRadius = IsLineWithinTrackingRadius(conduitData, cameraPosition);

        // Use exit margin to prevent flickering at boundary
        float exitRadius = trackingRadius + trackingExitMargin;

        // If line is currently visible, use exit radius for hiding
        if (lineObj.activeSelf)
        {
            bool shouldHide = !IsLineWithinExitRadius(conduitData, cameraPosition, exitRadius);
            if (shouldHide)
            {
                lineObj.SetActive(false);
                if (showDebugInfo)
                {
                    Debug.Log($"Hiding line '{conduitData.conduitName}' - beyond exit radius ({exitRadius}m)");
                }
            }
        }
        // If line is hidden, use normal radius for showing
        else
        {
            if (isWithinRadius)
            {
                lineObj.SetActive(true);
                if (showDebugInfo)
                {
                    Debug.Log($"Showing line '{conduitData.conduitName}' - within tracking radius ({trackingRadius}m)");
                }
            }
        }
    }

    /// <summary>
    /// Check if any segment is within exit radius
    /// </summary>
    private bool IsLineWithinExitRadius(ConduitLineSet.ConduitLineData conduitData, Vector3 cameraPosition, float exitRadius)
    {
        if (conduitData.relativePositions == null || conduitData.relativePositions.Count == 0)
            return false;

        foreach (var relativePos in conduitData.relativePositions)
        {
            Vector3 worldPos = conduitData.lastCameraPosition + relativePos;
            worldPos.y = 0;
            Vector3 camPosFlat = cameraPosition;
            camPosFlat.y = 0;

            float distance = Vector3.Distance(worldPos, camPosFlat);

            if (distance <= exitRadius)
            {
                return true;
            }
        }

        return false;
    }

    // Editor validation
    private void OnValidate()
    {
        if (Application.isPlaying && conduitLineSet != null)
        {
            RenderConduitLines();
        }
    }

#if UNITY_EDITOR
    private void Reset()
    {
        // Auto-find required components if not assigned
        if (arTrueNorthFinder == null)
        {
            arTrueNorthFinder = GameObject.FindObjectOfType<pLab_ARTrueNorthFinder>();
        }

        if (arCamera == null)
        {
            arCamera = this.GetComponentInChildren<Camera>();
            if (arCamera == null)
            {
                arCamera = Camera.main;
            }
        }

        if (locationProvider == null)
        {
            locationProvider = GameObject.FindObjectOfType<pLab_LocationProvider>();
        }

        if (deviceElevationEstimater == null)
        {
            deviceElevationEstimater = GameObject.FindObjectOfType<pLab_ARDeviceElevationEstimater>();
        }
    }
#endif

    // Debug information in inspector
    private void OnDrawGizmosSelected()
    {
        if (showDebugInfo && conduitLineSet != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var conduitData in conduitLineSet.conduitLines)
            {
                if (conduitData.worldPositions != null && conduitData.worldPositions.Count > 1)
                {
                    for (int i = 0; i < conduitData.worldPositions.Count - 1; i++)
                    {
                        Gizmos.DrawLine(conduitData.worldPositions[i], conduitData.worldPositions[i + 1]);
                    }
                }
            }
        }
    }

#if UNITY_EDITOR
    [Header("Editor Tools")]
    [SerializeField]
    private bool showEditorButtons = true;

    // Custom inspector buttons would go here if you create a custom editor
    // For now, using ContextMenu attributes for easy access
#endif
}