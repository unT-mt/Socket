using UnityEngine;
using System.Collections;

public class UrgSensorWrapper : MonoBehaviour
{
    private Urg.UrgSensor urgSensor;
    private int retryAttempts = 1;  // 再接続の試行回数
    private float retryDelay = 2.0f;  // 再接続を試行するまでの待ち時間

    void Awake()
    {
        urgSensor = GetComponent<Urg.UrgSensor>();
        InitializeSensor();
    }

    public void RestartSensor()
    {
        StartCoroutine(TryRestartSensor());
    }

    private IEnumerator TryRestartSensor()
    {
        if (urgSensor != null)
        {
            urgSensor.SendMessage("Close", SendMessageOptions.DontRequireReceiver);
            yield return new WaitForSeconds(5.0f);  // 少し待ってから再接続を試みる

            for (int i = 0; i < retryAttempts; i++)
            {
                urgSensor.SendMessage("Awake", SendMessageOptions.DontRequireReceiver);
                yield return new WaitForSeconds(retryDelay);  // 再接続の試行間に待機時間を設ける

                if (urgSensor.transport.IsConnected())  // 接続が確立されたか確認
                {
                    Debug.Log("UrgSensorの再接続に成功しました。");
                    yield break;
                }
                else
                {
                    Debug.LogWarning($"UrgSensorの再接続に失敗しました。試行回数: {i + 1}");
                }
            }

            Debug.LogError("UrgSensorの再接続に複数回失敗しました。");
        }
    }

    private void InitializeSensor()
    {
        Debug.Log("UrgSensorを初期化しました。");
    }
}
