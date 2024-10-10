using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using System.Collections.Concurrent;

public class UDPServer : MonoBehaviour
{
    private GameObject blueCube; // サーバー側の青Cube
    private GameObject redCube;  // クライアント側の赤Cube
    private UdpClient udpServer;
    private IPEndPoint clientEndPoint;

    private Thread receiveThread;
    private ConcurrentQueue<string> incomingMessages = new ConcurrentQueue<string>();

    private int sendSequenceNumber = 0; // 送信側シーケンス番号
    private int expectedReceiveSequence = 1; // 受信側期待シーケンス番号

    void Start()
    {
        udpServer = new UdpClient(5000); // ポート5000で待ち受け
        Debug.Log("UDPサーバーが起動しました");
        receiveThread = new Thread(ReceiveData);
        receiveThread.IsBackground = true;
        receiveThread.Start();

        // 青Cubeを生成
        blueCube = CreateColoredCube(Color.blue, new Vector3(0, 0.5f, 0));
    }

    void ReceiveData()
    {
        try
        {
            while (true)
            {
                // データを受信
                byte[] data = udpServer.Receive(ref clientEndPoint);
                string message = Encoding.UTF8.GetString(data);
                //Debug.Log($"サーバー受信: {message} from {clientEndPoint}");
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
        // サーバー側のCube移動 (上下左右キーで移動)
        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) move += Vector3.up * Time.deltaTime;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) move += Vector3.down * Time.deltaTime;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) move += Vector3.left * Time.deltaTime;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) move += Vector3.right * Time.deltaTime;

        blueCube.transform.position += move;

        // サーバーの青Cubeの位置をクライアントに送信
        if (clientEndPoint != null)
        {
            sendSequenceNumber++;
            string message = $"{sendSequenceNumber}:{blueCube.transform.position.x},{blueCube.transform.position.y}";
            byte[] data = Encoding.UTF8.GetBytes(message);
            udpServer.Send(data, data.Length, clientEndPoint);
            // Debug.Log($"サーバー送信: {message} to {clientEndPoint}");
        }

        // クライアントからの赤Cubeの位置情報を処理
        while (incomingMessages.TryDequeue(out string clientData))
        {
            // メッセージをシーケンス番号とデータに分割
            string[] parts = clientData.Split(':');
            if (parts.Length != 2)
            {
                Debug.LogWarning("不正なメッセージ形式: " + clientData);
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
                        if (redCube == null)
                        {
                            // redCubeが未生成の場合、生成する
                            redCube = CreateColoredCube(Color.red, new Vector3(x, y, 0));
                            Debug.Log("redCubeを生成しました");
                        }
                        else
                        {
                            // redCubeの位置を更新
                            redCube.transform.position = new Vector3(x, y, 0);
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
        if (udpServer != null)
            udpServer.Close();
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
