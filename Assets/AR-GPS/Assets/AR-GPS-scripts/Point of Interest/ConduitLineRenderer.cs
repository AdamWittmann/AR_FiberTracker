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

    [Header("Debug Info")]
    [SerializeField]
    [Tooltip("Show debug information about rendered lines")]
    private bool showDebugInfo = false;

    // Runtime data
    private List<GameObject> renderedLines = new List<GameObject>();

    private void Start()
    {
        if (createOnStart && conduitLineSet != null)
        {
            RenderConduitLines();
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

        // Set all positions along the line
        for (int i = 0; i < conduitData.worldPositions.Count; i++)
        {
            lineRenderer.SetPosition(i, conduitData.worldPositions[i]);
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

    // Editor validation
    private void OnValidate()
    {
        if (Application.isPlaying && conduitLineSet != null)
        {
            RenderConduitLines();
        }
    }

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