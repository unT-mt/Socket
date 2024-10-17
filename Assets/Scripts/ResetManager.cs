using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Urg
{
    public class ResetManager : MonoBehaviour
    {
        [Header("UI Settings")]
        public Button appResetButton; // リセット信号送信用のUIボタン
        public Button toggleUDPButton; // UDP接続切断/再開用のUIボタン

        [Header("UDP Reset Settings")]
        public int resetListenPort = 6000; // リセット信号を受信するポート
        public string senderIP = "127.0.0.1"; // リセット信号を送信する送信元IPアドレス
        public int senderPort = 5000; // リセット信号を送信する送信元ポート番号

        private UdpClient resetUdpClient;
        private CancellationTokenSource cts;
        private RayDataSender rayDataSender;

        void Start()
        {
            rayDataSender = GetComponent<RayDataSender>();
            if (rayDataSender == null)
            {
                Debug.LogError("RayDataSender コンポーネントが見つかりません。");
                return;
            }

            if (appResetButton != null)
            {
                appResetButton.onClick.AddListener(RestartApp.RestartApplication);
            }
            else
            {
                Debug.LogWarning("appResetボタンが割り当てられていません。");
            }

            if (toggleUDPButton != null)
            {
                toggleUDPButton.onClick.AddListener(ToggleConnection);
                UpdateButtonLabel();
            }
            else
            {
                Debug.LogWarning("toggleUDPボタンが割り当てられていません。");
            }

            InitializeUdpListener();
        }

        void InitializeUdpListener()
        {
            try
            {
                resetUdpClient = new UdpClient(resetListenPort);
                cts = new CancellationTokenSource();
                StartListening(cts.Token);
                Debug.Log($"ResetManager がポート {resetListenPort} でリセット信号を待ち受けています。");
            }
            catch (Exception e)
            {
                Debug.LogError($"ResetManager のUDP初期化エラー: {e.Message}");
            }
        }

        async void StartListening(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    UdpReceiveResult result = await resetUdpClient.ReceiveAsync();
                    string message = Encoding.UTF8.GetString(result.Buffer).Trim();
                    Debug.Log($"Reset信号受信: {message}");
                    if (message.Equals("RESET", StringComparison.OrdinalIgnoreCase))
                    {
                        RestartApp.RestartApplication();
                    }
                    if (message.Equals("TOGGLE", StringComparison.OrdinalIgnoreCase))
                    {
                        ToggleConnection();
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // UdpClientが閉じられた場合
                Debug.Log("ResetManager のUdpClientが閉じられました。");
            }
            catch (Exception e)
            {
                Debug.LogError($"ResetManager のリスニングエラー: {e.Message}");
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartApp.RestartApplication();
            }
            if (Input.GetKeyDown(KeyCode.F))
            {
                ToggleConnection();
            }
        }

        public void ToggleConnection()
        {
            if (rayDataSender.IsSending)
            {
                rayDataSender.StopSending();
            }
            else
            {
                rayDataSender.StartSending();
            }
            UpdateButtonLabel();
        }

        void UpdateButtonLabel()
        {
            if (toggleUDPButton != null)
            {
                toggleUDPButton.GetComponentInChildren<Text>().text = rayDataSender.IsSending ? "切断 (F)" : "再接続 (F)";
            }
        }

        private void OnDestroy()
        {
            if (resetUdpClient != null)
            {
                resetUdpClient.Close();
            }

            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
            }

            if (appResetButton != null)
            {
                appResetButton.onClick.RemoveListener(RestartApp.RestartApplication);
            }

            if (toggleUDPButton != null)
            {
                toggleUDPButton.onClick.RemoveListener(ToggleConnection);
            }
        }
    }
}
