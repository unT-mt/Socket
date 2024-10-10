using System;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using UnityEngine;
using System.Collections.Concurrent;

public class TCPClient : MonoBehaviour
{
    private GameObject redCube;   // クライアント側の赤Cube
    private GameObject blueCube;  // サーバー側の青Cube
    private TcpClient client;
    private StreamReader reader;
    private StreamWriter writer;

    private Thread receiveThread;
    private ConcurrentQueue<string> incomingMessages = new ConcurrentQueue<string>();

    void Start()
    {
        // 赤Cubeを生成
        redCube = CreateColoredCube(Color.red, new Vector3(2, 0.5f, 0));

        // サーバーに接続
        client = new TcpClient();
        try
        {
            client.Connect("127.0.0.1", 5000);
            Debug.Log("サーバーに接続しました");
            reader = new StreamReader(client.GetStream());
            writer = new StreamWriter(client.GetStream()) { AutoFlush = true };
            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();
    
            blueCube = CreateColoredCube(Color.blue, new Vector3(0, 0.5f, 0));
        }
        catch (Exception e)
        {
            Debug.LogError("クライアント接続エラー: " + e.Message);
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
        // クライアント側のCube移動 (上下左右キーで移動)
        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) move += Vector3.up * Time.deltaTime;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) move += Vector3.down * Time.deltaTime;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) move += Vector3.left * Time.deltaTime;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) move += Vector3.right * Time.deltaTime;

        redCube.transform.position += move;

        // クライアントの赤Cubeの位置をサーバーに送信
        if (client != null && client.Connected)
        {
            string message = $"{redCube.transform.position.x},{redCube.transform.position.y}";
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

        //     // クライアントの赤Cubeの位置をサーバーに送信
        //     if (client != null && client.Connected)
        //     {
        //         string message = $"{redCube.transform.position.x},{redCube.transform.position.y}";
        //         writer.WriteLine(message);
        //     }
        // }

        // サーバーからの青Cubeの位置情報を処理
        while (incomingMessages.TryDequeue(out string serverData))
        {
            string[] positions = serverData.Split(',');
            if (positions.Length >= 2)
            {
                if (float.TryParse(positions[0], out float x) && float.TryParse(positions[1], out float y))
                {
                    // サーバーの青Cubeを動かす
                    blueCube.transform.position = new Vector3(x, y, 0);
                }
                else
                {
                    Debug.LogWarning("位置データのパースに失敗: " + serverData);
                }
            }
        }
    }

    void OnApplicationQuit()
    {
        if (client != null)
            client.Close();
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
