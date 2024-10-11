using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections.Concurrent;

public class TCPServer : MonoBehaviour
{
    private GameObject blueCube; // サーバー側の青Cube
    private GameObject redCube;  // クライアント側の赤Cube
    private TcpListener server;
    private TcpClient client;
    private StreamReader reader;
    private StreamWriter writer;
    private ConcurrentQueue<string> incomingMessages = new ConcurrentQueue<string>();

    void Start()
    {
        // 青Cubeを生成
        blueCube = CreateColoredCube(Color.blue, new Vector3(0, 0.5f, 0));

        StartServer();
    }

    async void StartServer()
    {
        server = new TcpListener(IPAddress.Any, 5000); // ポート5000で待ち受け
        server.Start();
        Debug.Log("サーバーが起動しました");

        // クライアントの接続を待つ
        await ListenForClientsAsync();
    }

    async Task ListenForClientsAsync()
    {
        try
        {
            client = await server.AcceptTcpClientAsync();
            Debug.Log("クライアントが接続しました");
            reader = new StreamReader(client.GetStream());
            writer = new StreamWriter(client.GetStream()) { AutoFlush = true };

            // 赤Cubeを生成
            redCube = CreateColoredCube(Color.red, new Vector3(2, 0.5f, 0)); // サーバー上では位置を少しずらす

            // データ受信の非同期タスクを開始
            await ReceiveDataAsync();
        }
        catch (Exception e)
        {
            Debug.LogError("サーバーエラー: " + e.Message);
        }
    }

    async Task ReceiveDataAsync()
    {
        try
        {
            while (client.Connected)
            {
                string data = await reader.ReadLineAsync();
                if (!string.IsNullOrEmpty(data))
                {
                    incomingMessages.Enqueue(data);
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

        // writerが初期化しているかを確認しサーバーの青Cubeの位置をクライアントに送信
        if (client != null && client.Connected && writer != null)
        {
            string message = $"{blueCube.transform.position.x},{blueCube.transform.position.y}";
            writer.WriteLine(message);
        }
        //送信の頻度を下げる場合は下記のコード
        // private float sendInterval = 0.05f; // 20Hz
        // private float sendTimer = 0f;
        // // 送信タイマーを更新
        // sendTimer += Time.deltaTime;
        // if (sendTimer >= sendInterval)
        // {
        //     sendTimer = 0f;

        //     // クライアントの青Cubeの位置をサーバーに送信
        //     if (client != null && client.Connected && writer != null)
        //     {
        //         string message = $"{blueCube.transform.position.x},{blueCube.transform.position.y}";
        //         writer.WriteLine(message);
        //     }
        // }

        // クライアントからの赤Cubeの位置情報を処理
        while (incomingMessages.TryDequeue(out string clientData))
        {
            string[] positions = clientData.Split(',');
            if (positions.Length >= 2 && float.TryParse(positions[0], out float x) && float.TryParse(positions[1], out float y))
            {
                redCube.transform.position = new Vector3(x, y, 0);
            }
        }
    }

    void OnApplicationQuit()
    {
        client?.Close();
        server?.Stop();
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
}
