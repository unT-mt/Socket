using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections.Concurrent;

public class UDPClient : MonoBehaviour
{
    private GameObject redCube;   // クライアント側の赤Cube
    private GameObject blueCube;  // サーバー側の青Cube
    private UdpClient udpClient;
    private IPEndPoint serverEndPoint;
    private ConcurrentQueue<string> incomingMessages = new ConcurrentQueue<string>();
    private int sendSequenceNumber = 0; // 送信側シーケンス番号
    private int expectedReceiveSequence = 1; // 受信側期待シーケンス番号

    void Start()
    {
        // 赤Cubeを生成
        redCube = CreateColoredCube(Color.red, new Vector3(2, 0.5f, 0));

        StartClient();
    }

    async void StartClient()
    {
        serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5000);
        udpClient = new UdpClient();
        udpClient.Connect(serverEndPoint);
        Debug.Log("UDPサーバーに接続しました");

        // 非同期でデータを受信開始
        await ReceiveDataAsync();
    }

    async Task ReceiveDataAsync()
    {
        try
        {
            while (true)
            {
                var result = await udpClient.ReceiveAsync();
                string message = Encoding.UTF8.GetString(result.Buffer);
                incomingMessages.Enqueue(message);
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
            ProcessReceivedData(serverData);
        }
    }

    private void ProcessReceivedData(string serverData)
    {
        string[] parts = serverData.Split(':');
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
                    if (blueCube == null) blueCube = CreateColoredCube(Color.blue, new Vector3(x, y, 0));
                    blueCube.transform.position = new Vector3(x, y, 0);
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
        udpClient?.Close();
    }
}