using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class JSONPOIData
{
    public string zone;
    public List<Conduit> conduits;
    public List<Manhole> manholes;
}

[Serializable]
public class Conduit
{
    public string name;
    public List<Segment> segment;
    public string description;
    public int id;
}

[Serializable]
public class Segment
{
    public float lat;
    public float lng;
    public string notes;
}

[Serializable]
public class Manhole
{
    public string id;
    public string mid;
    public string description;
    public string Latitude;  // Note: JSON has these as strings
    public string Longitude; // Note: JSON has these as strings
}