using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class GPSPointLoader : MonoBehaviour
{
    public List<GPSPoint> gpsPoints = new List<GPSPoint>();
    public static GPSPointLoader Instance { get; private set; }

    void Start()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
        LoadGPSPoints();
    }


    void LoadGPSPoints()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "gps_points.json");

        if (File.Exists(filePath))
        {
            string jsonString = File.ReadAllText(filePath);
            try
            {
                // Wrap the JSON in a container class
                GPSPointList pointList = JsonUtility.FromJson<GPSPointList>(FixJson(jsonString));
                gpsPoints = new List<GPSPoint>(pointList.points);
                Debug.Log($"Loaded {gpsPoints.Count} GPS points.");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error parsing JSON: " + e.Message);
            }
        }
        else
        {
            Debug.LogError($"File not found: {filePath}");
        }
    }
    // Unity's JsonUtility doesn't support top-level arrays, so this wraps it
    private string FixJson(string value)
    {
        return "{\"points\":" + value + "}";
    }
}