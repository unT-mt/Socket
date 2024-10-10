using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

public class ObstacleReceiver : MonoBehaviour
{
    [Header("UDP Settings")]
    public int listenPort = 5000; // 受信ポート
    public string serverIP = "127.0.0.1"; // 送信先のIPアドレス
    public int serverPort = 5000; // 送信先のポート番号

    [Header("Obstacle Settings")]
    public GameObject obstaclePrefab; // 配置するPrefab
    public Transform obstaclesParent; // 障害物を格納する親オブジェクト
    public float maxDistance = 10f; // 最大距離

    private UdpClient udpClient;
    private IPEndPoint remoteEndPoint;

    // 管理用のDictionary (光線ID -> Obstacle)
    private Dictionary<int, GameObject> obstacles = new Dictionary<int, GameObject>();

    private CancellationTokenSource cts;

    void Start()
    {
        InitializeUdpClient();
        cts = new CancellationTokenSource();
        StartReceivingData(cts.Token).Forget();
    }

    void InitializeUdpClient()
    {
        try
        {
            udpClient = new UdpClient(listenPort);
            remoteEndPoint = new IPEndPoint(IPAddress.Any, listenPort);
            Debug.Log($"ObstacleReceiver がポート {listenPort} で待ち受けを開始しました。");
        }
        catch (Exception e)
        {
            Debug.LogError($"UDPクライアントの初期化エラー: {e.Message}");
        }
    }

    async UniTaskVoid StartReceivingData(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                UdpReceiveResult result;
                try
                {
                    result = await udpClient.ReceiveAsync();
                }
                catch (OperationCanceledException)
                {
                    // キャンセレーションが発生した場合
                    Debug.Log("受信ループがキャンセルされました。");
                    break;
                }
                catch (ObjectDisposedException)
                {
                    // UdpClient が閉じられた場合
                    Debug.Log("UdpClient が閉じられました。受信ループを終了します。");
                    break;
                }

                // オブジェクトが破棄されている場合は処理を中断
                if (this == null)
                {
                    Debug.LogWarning("ObstacleReceiverが既に破棄されています。受信ループを終了します。");
                    break;
                }

                string receivedData = Encoding.UTF8.GetString(result.Buffer).Trim();
                Debug.Log($"受信: {receivedData}");

                ParseAndPlaceObstacles(receivedData);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"受信ループエラー: {e.Message}");
        }
        finally
        {
            // finally ブロックから udpClient.Close() を削除
            Debug.Log("受信ループが終了しました。");
        }
    }

    void ParseAndPlaceObstacles(string data)
    {
        // データフォーマット: id,distance;id,distance;...
        string[] rayData = data.Split(';');
        foreach (string ray in rayData)
        {
            string[] parts = ray.Split(',');
            if (parts.Length != 2)
            {
                Debug.LogWarning($"無効なデータ形式: {ray}");
                continue;
            }

            if (int.TryParse(parts[0], out int rayId) && float.TryParse(parts[1], out float distance))
            {
                if (distance <= maxDistance)
                {
                    PlaceOrUpdateObstacle(rayId, distance);
                }
                else
                {
                    RemoveObstacle(rayId);
                }
            }
            else
            {
                Debug.LogWarning($"パース失敗: {ray}");
            }
        }
    }

    void PlaceOrUpdateObstacle(int rayId, float distance)
    {
        Vector3 position = CalculatePosition(rayId, distance);

        if (obstacles.ContainsKey(rayId))
        {
            // 既存の障害物があれば位置を更新
            obstacles[rayId].transform.position = position;
        }
        else
        {
            // 新規の障害物を生成
            GameObject obstacle = Instantiate(obstaclePrefab, position, Quaternion.identity, obstaclesParent);
            obstacles.Add(rayId, obstacle);
        }
    }

    void RemoveObstacle(int rayId)
    {
        if (obstacles.ContainsKey(rayId))
        {
            Destroy(obstacles[rayId]);
            obstacles.Remove(rayId);
        }
    }

    Vector3 CalculatePosition(int rayId, float distance)
    {
        // 光線の角度を計算 (送信側と一致させる)
        float stepAngleDegrees = 0.25f;
        float offsetDegrees = -135f;
        float angleDegrees = stepAngleDegrees * rayId + offsetDegrees;
        float angleRadians = angleDegrees * Mathf.Deg2Rad;

        Vector3 direction = new Vector3(-Mathf.Sin(angleRadians), 0, Mathf.Cos(angleRadians));
        Vector3 position = transform.position + direction * distance;

        return position;
    }

    private void OnDestroy()
    {
        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
            cts = null;
        }

        if (udpClient != null)
        {
            udpClient.Close();
            udpClient = null;
        }
    }
}
