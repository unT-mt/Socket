using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;

public class ObstacleReceiver : MonoBehaviour
{
    [Header("UDP Settings")]
    public int listenPort = 5000; // 受信ポート
    public string serverIP = "127.0.0.1"; // 送信先のIPアドレス

    [Header("Obstacle Settings")]
    public GameObject obstaclePrefab; // 配置するPrefab
    public Transform obstaclesParent; // 障害物を格納する親オブジェクト
    public float maxDistance = 10f; // 最大距離

    [Header("Reset Command Settings")]
    public ResetCommandSender resetCommandSender; // リセット信号送信クラスへの参照

    private UdpClient udpClient;
    private IPEndPoint remoteEndPoint;

    // 管理用のDictionary (光線ID -> Obstacle)
    private Dictionary<int, GameObject> obstacles = new Dictionary<int, GameObject>();

    private CancellationTokenSource cts;

    // スレッドセーフなキューを使用して受信データをメインスレッドに渡す
    private ConcurrentQueue<string> receivedDataQueue = new ConcurrentQueue<string>();

    void Start()
    {
        InitializeUdpClient();
        cts = new CancellationTokenSource();
        StartReceivingData(cts.Token);
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

    void StartReceivingData(CancellationToken token)
    {
        Task.Run(async () =>
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

                    string receivedData = Encoding.UTF8.GetString(result.Buffer).Trim();
                    Debug.Log($"受信: {receivedData}");

                    // データをキューに追加
                    receivedDataQueue.Enqueue(receivedData);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"受信ループエラー: {e.Message}");
            }
            finally
            {
                Debug.Log("受信ループが終了しました。");
            }
        }, token);
    }

    void Update()
    {
        // キューからデータを処理
        while (receivedDataQueue.TryDequeue(out string data))
        {
            if (data.Equals("REQUEST_RESET", StringComparison.OrdinalIgnoreCase))
            {
                // リセット信号を送信
                if (resetCommandSender != null)
                {
                    resetCommandSender.SendResetCommand();
                }
                else
                {
                    Debug.LogWarning("ResetCommandSender が割り当てられていません。");
                }
            }
            else
            {
                ParseAndPlaceObstacles(data);
            }
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

    public Dictionary<int, float> GetObstacleData()
    {
        // 障害物のデータ（光線ID -> 距離）を返す
        Dictionary<int, float> obstacleData = new Dictionary<int, float>();
        foreach (var entry in obstacles)
        {
            int rayId = entry.Key;
            float distance = Vector3.Distance(transform.position, entry.Value.transform.position);
            obstacleData.Add(rayId, distance);
        }
        return obstacleData;
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
