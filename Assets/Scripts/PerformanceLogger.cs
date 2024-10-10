using UnityEngine;
using System.IO;

public class PerformanceLogger : MonoBehaviour
{
    private float elapsedTime;
    private int frameCount;
    private string logFilePath;

    void Start()
    {
        elapsedTime = 0f;
        frameCount = 0;
        
        // ログファイルのパス設定
        string directory = Application.persistentDataPath + "/PerformanceLogs";
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        logFilePath = Path.Combine(directory, "performance_log.csv");

        // CSVヘッダーの作成
        File.WriteAllText(logFilePath, "Time,FPS,Memory(MB)\n");
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;
        frameCount++;

        // 1秒ごとにログを出力
        if (elapsedTime >= 1f)
        {
            float fps = frameCount;
            long memory = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / (1024 * 1024);
            
            // CSVファイルにデータを追加
            string logLine = $"{Time.time},{fps},{memory}\n";
            File.AppendAllText(logFilePath, logLine);
            
            elapsedTime = 0f;
            frameCount = 0;
        }
    }

    private void OnApplicationQuit()
    {
        Debug.Log($"Performance log saved to: {logFilePath}");
    }
}
