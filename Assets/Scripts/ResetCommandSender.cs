using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class ResetCommandSender : MonoBehaviour
{
    [Header("Reset Command Settings")]
    public string senderIP = "127.0.0.1"; // センサー送信側のIPアドレス
    public int senderResetPort = 6000; // センサー送信側のリセット受信ポート

    [Header("UI Settings")]
    public Button sendResetButton; // リセット信号送信用のUIボタン

    private UdpClient udpClient;
    private IPEndPoint remoteEndPoint;

    void Start()
    {
        try
        {
            udpClient = new UdpClient();
            remoteEndPoint = new IPEndPoint(IPAddress.Parse(senderIP), senderResetPort);
        }
        catch (Exception e)
        {
            Debug.LogError($"ResetCommandSender のUDP初期化エラー: {e.Message}");
        }

        if (sendResetButton != null)
        {
            sendResetButton.onClick.AddListener(SendResetCommand);
        }
        else
        {
            Debug.LogWarning("SendResetボタンが割り当てられていません。");
        }
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SendResetCommand();
        }
    }

    public void SendResetCommand()
    {
        string resetMessage = "RESET";
        byte[] sendData = Encoding.UTF8.GetBytes(resetMessage);
        try
        {
            udpClient.Send(sendData, sendData.Length, remoteEndPoint);
            Debug.Log($"リセット信号送信: {resetMessage}");
        }
        catch (Exception e)
        {
            Debug.LogError($"リセット信号送信エラー: {e.Message}");
        }
    }

    private void OnDestroy()
    {
        if (udpClient != null)
        {
            udpClient.Close();
        }

        if (sendResetButton != null)
        {
            sendResetButton.onClick.RemoveListener(SendResetCommand);
        }
    }
}
