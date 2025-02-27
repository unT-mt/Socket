using UnityEngine;
using UnityEngine.UI; // Or TMPro if using TextMeshPro

public class InGameLogDisplay : MonoBehaviour
{
    public Text logText; // Drag your UI Text component here in the Inspector
    private string logMessages = "";

    void OnEnable()
    {
        // Subscribe to Unity's log message event
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        // Unsubscribe when the object is disabled
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        // Append the log message to the existing text
        logMessages += logString + "\n";

        // Update the UI text to display the log
        logText.text = logMessages;
    }
}