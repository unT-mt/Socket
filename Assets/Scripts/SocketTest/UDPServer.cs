using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections.Concurrent;

public class UDPServer : MonoBehaviour
{
    private GameObject blueCube; // サーバー側の青Cube
    private GameObject redCube;  // クライアント側の赤Cube
    private UdpClient udpServer;
    private IPEndPoint clientEndPoint;
    private ConcurrentQueue<string> incomingMessages = new ConcurrentQueue<string>();
    private int sendSequenceNumber = 0; // 送信側シーケンス番号
    private int expectedReceiveSequence = 1; // 受信側期待シーケンス番号

    void Start()
    {
        // 青Cubeを生成
        blueCube = CreateColoredCube(Color.blue, new Vector3(0, 0.5f, 0));

        StartServer();
    }

    async void StartServer()
    {
        udpServer = new UdpClient(5000); // ポート5000で待ち受け
        Debug.Log("UDPサーバーが起動しました");

        // 非同期でデータを受信開始
        await ReceiveDataAsync();
    }

    async Task ReceiveDataAsync()
    {
        try
        {
            while (true)
            {
                var result = await udpServer.ReceiveAsync();
                clientEndPoint = result.RemoteEndPoint; // クライアントのエンドポイントを保存
                string data = Encoding.UTF8.GetString(result.Buffer);
                incomingMessages.Enqueue(data);
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

        // クライアントが接続している場合はデータ送信
        if (clientEndPoint != null)
        {
            sendSequenceNumber++;
            string message = $"{sendSequenceNumber}:{blueCube.transform.position.x},{blueCube.transform.position.y}";
            byte[] data = Encoding.UTF8.GetBytes(message);
            udpServer.Send(data, data.Length, clientEndPoint);
        }

        // 受信したメッセージを処理
        while (incomingMessages.TryDequeue(out string clientData))
        {
            ProcessReceivedData(clientData);
        }
    }

    private void ProcessReceivedData(string clientData)
    {
        // メッセージをシーケンス番号とデータに分割
        string[] parts = clientData.Split(':');
        if (parts.Length != 2) return;

        if (int.TryParse(parts[0], out int receivedSequence))
        {
            if (receivedSequence > expectedReceiveSequence)
            {
                Debug.LogWarning($"パケット損失検出: 期待 {expectedReceiveSequence} だが {receivedSequence} を受信");
            }
            else if (receivedSequence < expectedReceiveSequence)
            {
                Debug.LogWarning($"パケット順序乱れ検出: 期待 {expectedReceiveSequence} だが {receivedSequence} を受信");
            }
            else if (receivedSequence == expectedReceiveSequence)
            {
                expectedReceiveSequence++;
                string[] positions = parts[1].Split(',');
                if (positions.Length >= 2 && float.TryParse(positions[0], out float x) && float.TryParse(positions[1], out float y))
                {
                    if (redCube == null) redCube = CreateColoredCube(Color.red, new Vector3(x, y, 0));
                    redCube.transform.position = new Vector3(x, y, 0);
                }
            }
        }
    }

    private GameObject CreateColoredCube(Color color, Vector3 position)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Renderer renderer = cube.GetComponent<Renderer>();
        Material newMaterial = new Material(Shader.Find("Standard"));
        newMaterial.color = color;
        renderer.material = newMaterial;
        cube.transform.position = position;
        return cube;
    }

    void OnApplicationQuit()
    {
        udpServer?.Close();
    }
}