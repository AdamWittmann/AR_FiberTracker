using TMPro;
using UnityEngine;

public class DebugDisplay : MonoBehaviour
{
    public static DebugDisplay Instance;
    public bool debugEnabled = true;

    [SerializeField] private TextMeshProUGUI debugText;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    public void Log(string message)
    {
        if (debugEnabled && debugText != null)
        {
            debugText.text = message;
        }
    }
}
