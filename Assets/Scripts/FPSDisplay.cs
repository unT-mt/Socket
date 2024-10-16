using UnityEngine;
using UnityEngine.UI; // Textを使用する場合
// using TMPro; // TextMeshProを使用する場合

/// <summary>
/// FPS（Frames Per Second）を計算し、UIに表示するクラス
/// </summary>
public class FPSDisplay : MonoBehaviour
{
    [Header("UI Settings")]
    [Tooltip("FPSを表示するUI Textコンポーネント")]
    public Text fpsText; // Textを使用する場合
    // public TextMeshProUGUI fpsText; // TextMeshProを使用する場合

    [Header("Calculation Settings")]
    [Tooltip("FPSを計算する間隔（秒）")]
    public float updateInterval = 0.5f;

    private float accum = 0f; // FPSの合計値
    private int frames = 0; // フレーム数
    private float timeleft; // 更新までの残り時間

    void Start()
    {
        if (fpsText == null)
        {
            Debug.LogError("FPSDisplay: fpsTextがアサインされていません。");
            enabled = false;
            return;
        }

        timeleft = updateInterval;
    }

    void Update()
    {
        timeleft -= Time.deltaTime;
        accum += Time.timeScale / Time.deltaTime;
        frames++;

        // 指定された間隔でFPSを更新
        if (timeleft <= 0.0)
        {
            float fps = accum / frames;
            string fpsString = string.Format("FPS: {0:F1}", fps);
            fpsText.text = fpsString;

            // TextMeshProを使用する場合
            // fpsText.text = $"FPS: {fps:F1}";

            // リセット
            timeleft = updateInterval;
            accum = 0f;
            frames = 0;
        }
    }
}
