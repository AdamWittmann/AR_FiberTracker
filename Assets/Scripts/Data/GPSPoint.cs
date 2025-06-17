[System.Serializable]
public class GPSPoint
{
    public string id;
    public string mid;
    public string description;
    public string Latitude;
    public string Longitude;
}

[System.Serializable]
public class GPSPointList
{
    public GPSPoint[] points;
}