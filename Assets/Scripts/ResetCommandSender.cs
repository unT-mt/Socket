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
    public Button toggleUDPButton; // UDP接続切断/再開用のUIボタン

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

        if (toggleUDPButton != null)
        {
            toggleUDPButton.onClick.AddListener(SendUDPConnectionToggleCommand);
        }
        else
        {
            Debug.LogWarning("toggleUDPボタンが割り当てられていません。");
        }
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SendResetCommand();
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            SendUDPConnectionToggleCommand();
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

    public void SendUDPConnectionToggleCommand()
    {
        string resetMessage = "TOGGLE";
        byte[] sendData = Encoding.UTF8.GetBytes(resetMessage);
        try
        {
            udpClient.Send(sendData, sendData.Length, remoteEndPoint);
            Debug.Log($"UDP接続切断/再開信号送信: {resetMessage}");
        }
        catch (Exception e)
        {
            Debug.LogError($"UDP接続切断/再開信号送信エラー: {e.Message}");
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

        if (toggleUDPButton != null)
        {
            toggleUDPButton.onClick.RemoveListener(SendUDPConnectionToggleCommand);
        }
    }
}
