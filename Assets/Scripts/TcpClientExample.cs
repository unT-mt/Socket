using System;
using System.Net.Sockets;
using UnityEngine;

public class TcpClientExample : MonoBehaviour
{
    TcpClient client;

    void Start()
    {
        client = new TcpClient();
        client.BeginConnect("127.0.0.1", 5000, OnConnect, null); // サーバーに接続
    }

    void OnConnect(IAsyncResult result)
    {
        if (client.Connected)
        {
            Debug.Log("サーバーに接続しました");
        }
    }

    void OnApplicationQuit()
    {
        if (client != null)
        {
            client.Close();
        }
    }
}
