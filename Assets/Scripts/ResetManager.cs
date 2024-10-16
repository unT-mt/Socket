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
        public Button resetButton; // リセット用のUIボタン

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

            if (resetButton != null)
            {
                resetButton.onClick.AddListener(ToggleConnection);
                UpdateButtonLabel();
            }
            else
            {
                Debug.LogWarning("Resetボタンが割り当てられていません。");
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
            if (resetButton != null)
            {
                resetButton.GetComponentInChildren<Text>().text = rayDataSender.IsSending ? "切断 (R)" : "再接続 (R)";
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

            if (resetButton != null)
            {
                resetButton.onClick.RemoveListener(ToggleConnection);
            }
        }
    }
}
