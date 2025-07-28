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
    [Header("Conduit Line Settings")]
    
    private ConduitLineSet conduitLineSet;
    
    private Material defaultLineMaterial;
    
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