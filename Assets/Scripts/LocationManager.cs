// LocationManager.cs
using UnityEngine;
using System.Collections;
public class LocationManager : MonoBehaviour
{
    public static LocationManager Instance;

    void Awake()
    {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    public LocationInfo GetCurrentLocation()
    {
        return Input.location.lastData;
    }

    IEnumerator Start()
    {
        if (!Input.location.isEnabledByUser)
            yield break;

        Input.location.Start();

        while (Input.location.status == LocationServiceStatus.Initializing)
            yield return new WaitForSeconds(1);

        if (Input.location.status == LocationServiceStatus.Running)
            Debug.Log("Location service started");
        else
            Debug.Log("Location service failed");
    }
}
