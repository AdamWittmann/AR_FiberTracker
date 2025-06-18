// LocationManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class LocationManager : MonoBehaviour
{
    public static LocationManager Instance;

    public double deviceLat;
    public double deviceLong;
    public double deviceAlt;
    public bool IsReady { get; private set; } = false;

    void Awake()
    {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        StartCoroutine(StartLocationService());
    }

    private IEnumerator StartLocationService()
    {
        if (!Input.location.isEnabledByUser)
        {
            Debug.LogWarning("GPS not enabled");
            yield break;
        }

        Input.location.Start();

        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (maxWait <= 0)
        {
            Debug.LogError("Timed out while initializing GPS.");
            yield break;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.LogError("Unable to determine device location.");
            yield break;
        }

        var data = Input.location.lastData;
        deviceLat = data.latitude;
        deviceLong = data.longitude;
        deviceAlt = data.altitude;

        Debug.Log($"GPS Ready: Lat {deviceLat}, Lon {deviceLong}, Alt {deviceAlt}");

        IsReady = true;
    }
}
