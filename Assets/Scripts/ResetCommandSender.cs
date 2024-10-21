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
    public Button resetSensorAppButton; // リセット信号送信用のUIボタン
    public Button toggleUDPConnectionButton; // UDP接続切断/再開用のUIボタン
    public Button resetURGInstanceButton; // UDP接続切断/再開用のUIボタン

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

        if (resetSensorAppButton != null)
        {
            resetSensorAppButton.onClick.AddListener(SendResetCommand);
        }
        else
        {
            Debug.LogWarning("SendResetCommandが割り当てられていません。");
        }

        if (toggleUDPConnectionButton != null)
        {
            toggleUDPConnectionButton.onClick.AddListener(SendUDPConnectionToggleCommand);
        }
        else
        {
            Debug.LogWarning("SendUDPConnectionToggleCommandが割り当てられていません。");
        }

        if (resetURGInstanceButton != null)
        {
            resetURGInstanceButton.onClick.AddListener(SendURGInstanceResetCommand);
        }
        else
        {
            Debug.LogWarning("SendURGInstanceResetCommandが割り当てられていません。");
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
        if (Input.GetKeyDown(KeyCode.V))
        {
            SendURGInstanceResetCommand();
        }
    }

    public void SendResetCommand()
    {
        string resetMessage = "RESET_SENSORAPP";
        byte[] sendData = Encoding.UTF8.GetBytes(resetMessage);
        try
        {
            udpClient.Send(sendData, sendData.Length, remoteEndPoint);
            Debug.Log($"センサー送信アプリリセット信号送信: {resetMessage}");
        }
        catch (Exception e)
        {
            Debug.LogError($"センサー送信アプリリセット信号送信エラー: {e.Message}");
        }
    }

    public void SendUDPConnectionToggleCommand()
    {
        string resetMessage = "TOGGLE_UDPCONNECTION";
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

    public void SendURGInstanceResetCommand()
    {
        string resetMessage = "RESET_URGINSTANCE";
        byte[] sendData = Encoding.UTF8.GetBytes(resetMessage);
        try
        {
            udpClient.Send(sendData, sendData.Length, remoteEndPoint);
            Debug.Log($"URGリセット信号送信: {resetMessage}");
        }
        catch (Exception e)
        {
            Debug.LogError($"URGリセット信号信号送信エラー: {e.Message}");
        }
    }

    private void OnDestroy()
    {
        if (udpClient != null)
        {
            udpClient.Close();
        }

        if (resetSensorAppButton != null)
        {
            resetSensorAppButton.onClick.RemoveListener(SendResetCommand);
        }

        if (toggleUDPConnectionButton != null)
        {
            toggleUDPConnectionButton.onClick.RemoveListener(SendUDPConnectionToggleCommand);
        }

        if (resetURGInstanceButton != null)
        {
            resetURGInstanceButton.onClick.RemoveListener(SendURGInstanceResetCommand);
        }
    }
}
