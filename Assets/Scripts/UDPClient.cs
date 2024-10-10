using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using System.Collections.Concurrent;

public class UDPClient : MonoBehaviour
{
    private GameObject redCube;   // クライアント側の赤Cube
    private GameObject blueCube;  // サーバー側の青Cube
    private UdpClient udpClient;
    private IPEndPoint serverEndPoint;

    private Thread receiveThread;
    private ConcurrentQueue<string> incomingMessages = new ConcurrentQueue<string>();

    private int sendSequenceNumber = 0; // 送信側シーケンス番号
    private int expectedReceiveSequence = 1; // 受信側期待シーケンス番号

    void Start()
    {
        // 赤Cubeを生成
        redCube = CreateColoredCube(Color.red, new Vector3(2, 0.5f, 0));

        // サーバーのエンドポイントを設定
        serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5000);

        // UDPクライアントを初期化
        udpClient = new UdpClient();
        udpClient.Connect(serverEndPoint);

        try
        {
            Debug.Log("UDPサーバーに接続しました");
            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            // 青Cubeを生成
            blueCube = CreateColoredCube(Color.blue, new Vector3(0, 0.5f, 0));
        }
        catch (Exception e)
        {
            Debug.LogError("クライアント接続エラー: " + e.Message);
        }

        // クライアント接続時に初回メッセージを送信してサーバーに通知
        SendInitialPosition();
    }

    void SendInitialPosition()
    {
        if (udpClient != null)
        {
            sendSequenceNumber++;
            string message = $"{sendSequenceNumber}:{redCube.transform.position.x},{redCube.transform.position.y}";
            byte[] data = Encoding.UTF8.GetBytes(message);
            udpClient.Send(data, data.Length);
            Debug.Log($"クライアント初回送信: {message}");
        }
    }

    void ReceiveData()
    {
        try
        {
            while (true)
            {
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpClient.Receive(ref remoteEndPoint);
                string message = Encoding.UTF8.GetString(data);
                // Debug.Log($"クライアント受信: {message} from {remoteEndPoint}");
                if (!string.IsNullOrEmpty(message))
                {
                    incomingMessages.Enqueue(message);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("受信エラー: " + e.Message);
        }
    }

    void Update()
    {
        // クライアント側のCube移動 (WASDまたは上下左右キーで移動)
        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) move += Vector3.up * Time.deltaTime;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) move += Vector3.down * Time.deltaTime;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) move += Vector3.left * Time.deltaTime;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) move += Vector3.right * Time.deltaTime;

        redCube.transform.position += move;

        // クライアントの赤Cubeの位置をサーバーに送信
        if (udpClient != null)
        {
            sendSequenceNumber++;
            string message = $"{sendSequenceNumber}:{redCube.transform.position.x},{redCube.transform.position.y}";
            byte[] data = Encoding.UTF8.GetBytes(message);
            udpClient.Send(data, data.Length);
            // Debug.Log($"クライアント送信: {message}");
        }

        // サーバーからの青Cubeの位置情報を処理
        while (incomingMessages.TryDequeue(out string serverData))
        {
            // メッセージをシーケンス番号とデータに分割
            string[] parts = serverData.Split(':');
            if (parts.Length != 2)
            {
                Debug.LogWarning("不正なメッセージ形式: " + serverData);
                continue;
            }

            if (int.TryParse(parts[0], out int receivedSequence))
            {
                if (receivedSequence > expectedReceiveSequence)
                {
                    Debug.LogWarning($"パケット損失検出: 期待 {expectedReceiveSequence} だが {receivedSequence} を受信");
                    expectedReceiveSequence = receivedSequence + 1; // 次の期待シーケンス番号を更新
                }
                else if (receivedSequence < expectedReceiveSequence)
                {
                    Debug.LogWarning($"パケット順序乱れ検出: 期待 {expectedReceiveSequence} だが {receivedSequence} を受信");
                    // 重複パケットとして無視するか、再処理するかを選択
                }
                else
                {
                    expectedReceiveSequence++;
                }

                string data = parts[1];
                string[] positions = data.Split(',');
                if (positions.Length >= 2)
                {
                    if (float.TryParse(positions[0], out float x) && float.TryParse(positions[1], out float y))
                    {
                        if (blueCube == null)
                        {
                            // 初回のみ青Cubeを生成
                            blueCube = CreateColoredCube(Color.blue, new Vector3(x, y, 0));
                            Debug.Log("blueCubeを生成しました");
                        }
                        else
                        {
                            // blueCubeの位置を更新
                            blueCube.transform.position = new Vector3(x, y, 0);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("位置データのパースに失敗: " + data);
                    }
                }
            }
            else
            {
                Debug.LogWarning("シーケンス番号のパースに失敗: " + parts[0]);
            }
        }
    }

    void OnApplicationQuit()
    {
        if (udpClient != null)
            udpClient.Close();
        if (receiveThread != null && receiveThread.IsAlive)
            receiveThread.Abort();
    }

    // カラー付きキューブを生成するヘルパーメソッド
    private GameObject CreateColoredCube(Color color, Vector3 position)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Renderer renderer = cube.GetComponent<Renderer>();

        // 新しいマテリアルを作成し、Standardシェーダーを設定
        Material newMaterial = new Material(Shader.Find("Standard"));
        newMaterial.color = color;
        renderer.material = newMaterial;

        cube.transform.position = position;
        return cube;
    }
}
