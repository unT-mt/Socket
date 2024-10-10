using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using UnityEngine;
using System.Collections.Concurrent;

public class Server : MonoBehaviour
{
    public GameObject blueCube; // サーバー側の青Cube
    public GameObject redCube;  // クライアント側の赤Cube
    private TcpListener server;
    private TcpClient client;
    private StreamReader reader;
    private StreamWriter writer;

    private Thread listenThread;
    private Thread receiveThread;
    private ConcurrentQueue<string> incomingMessages = new ConcurrentQueue<string>();

    void Start()
    {
        server = new TcpListener(IPAddress.Any, 5000); // ポート5000で待ち受け
        server.Start();
        Debug.Log("サーバーが起動しました");
        listenThread = new Thread(ListenForClients);
        listenThread.IsBackground = true;
        listenThread.Start();

        // 青Cubeを生成
        blueCube = CreateColoredCube(Color.blue, new Vector3(0, 0.5f, 0));
    }

    void ListenForClients()
    {
        try
        {
            client = server.AcceptTcpClient();
            Debug.Log("クライアントが接続しました");
            reader = new StreamReader(client.GetStream());
            writer = new StreamWriter(client.GetStream()) { AutoFlush = true };
            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            // 赤Cubeを生成
            redCube = CreateColoredCube(Color.red, new Vector3(2, 0.5f, 0)); // サーバー上では位置を少しずらす
        }
        catch (Exception e)
        {
            Debug.LogError("サーバーエラー: " + e.Message);
        }
    }

    void ReceiveData()
    {
        try
        {
            while (client.Connected)
            {
                string data = reader.ReadLine();
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
        if (Input.GetKey(KeyCode.UpArrow)) move += Vector3.up * Time.deltaTime;
        if (Input.GetKey(KeyCode.DownArrow)) move += Vector3.down * Time.deltaTime;
        if (Input.GetKey(KeyCode.LeftArrow)) move += Vector3.left * Time.deltaTime;
        if (Input.GetKey(KeyCode.RightArrow)) move += Vector3.right * Time.deltaTime;

        blueCube.transform.position += move;

        // サーバーの青Cubeの位置をクライアントに送信
        if (client != null && client.Connected)
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

        //     // サーバーの青Cubeの位置をクライアントに送信
        //     if (client != null && client.Connected)
        //     {
        //         string message = $"{blueCube.transform.position.x},{blueCube.transform.position.y}";
        //         writer.WriteLine(message);
        //     }
        // }

        // クライアントからの赤Cubeの位置情報を処理
        while (incomingMessages.TryDequeue(out string clientData))
        {
            string[] positions = clientData.Split(',');
            if (positions.Length >= 2)
            {
                if (float.TryParse(positions[0], out float x) && float.TryParse(positions[1], out float y))
                {
                    // クライアントの赤Cubeを動かす
                    redCube.transform.position = new Vector3(x, y, 0);
                }
                else
                {
                    Debug.LogWarning("位置データのパースに失敗: " + clientData);
                }
            }
        }
    }

    void OnApplicationQuit()
    {
        if (client != null)
            client.Close();
        if (server != null)
            server.Stop();
        if (listenThread != null && listenThread.IsAlive)
            listenThread.Abort();
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
