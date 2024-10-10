using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class TcpServer : MonoBehaviour
{
    TcpListener server;

    void Start()
    {
        server = new TcpListener(IPAddress.Any, 5000); // ポート5000で待ち受け
        server.Start();
        Debug.Log("サーバーが起動しました");
        server.BeginAcceptTcpClient(OnClientConnect, null); // 非同期で接続待ち
    }

    void OnClientConnect(IAsyncResult result)
    {
        TcpClient client = server.EndAcceptTcpClient(result);
        Debug.Log("クライアントが接続しました");
        server.BeginAcceptTcpClient(OnClientConnect, null); // 再度接続待ち
    }

    void OnApplicationQuit()
    {
        server.Stop();
    }
}
