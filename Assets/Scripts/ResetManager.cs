using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Urg
{
    public class ResetManager : MonoBehaviour
    {
        [Header("UI Settings")]
        public Button resetSensorAppButton; // リセット信号送信用のUIボタン
        public Button toggleUDPConnectionButton; // UDP接続切断/再開用のUIボタン
        public Button resetURGInstanceButton; // UDP接続切断/再開用のUIボタン

        [Header("UDP Reset Settings")]
        public int resetListenPort = 6000; // リセット信号を受信するポート
        public string senderIP = "127.0.0.1"; // リセット信号を送信する送信元IPアドレス
        public int senderPort = 5000; // リセット信号を送信する送信元ポート番号

        public UrgSensorWrapper urgSensor; // Wrapperを経由したUrgSensor コンポーネントへの参照
        public RayDataSender rayDataSender; // RayDataSender への参照
        private bool isResettingURGSensor = false; // リセット処理中かどうかのフラグ

        private UdpClient resetUdpClient;
        private CancellationTokenSource cts;

        void Start()
        {
            rayDataSender = GetComponent<RayDataSender>();
            if (rayDataSender == null)
            {
                Debug.LogError("RayDataSender コンポーネントが見つかりません。");
                return;
            }

            if (resetSensorAppButton != null)
            {
                resetSensorAppButton.onClick.AddListener(RestartApp.RestartApplication);
            }
            else
            {
                Debug.LogWarning("resetSensorAppボタンが割り当てられていません。");
            }

            if (toggleUDPConnectionButton != null)
            {
                toggleUDPConnectionButton.onClick.AddListener(ToggleConnection);
                UpdateButtonLabel();
            }
            else
            {
                Debug.LogWarning("toggleUDPConnectionボタンが割り当てられていません。");
            }

            if (resetURGInstanceButton != null)
            {
                resetURGInstanceButton.onClick.AddListener(ResetUrgSensor);
            }
            else
            {
                Debug.LogWarning("resetURGInstanceボタンが割り当てられていません。");
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
                    if (message.Equals("RESET_SENSORAPP", StringComparison.OrdinalIgnoreCase))
                    {
                        RestartApp.RestartApplication();
                    }
                    if (message.Equals("TOGGLE_UDPCONNECTION", StringComparison.OrdinalIgnoreCase))
                    {
                        ToggleConnection();
                    }
                    if (message.Equals("RESET_URGINSTANCE", StringComparison.OrdinalIgnoreCase))
                    {
                        ResetUrgSensor();
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
            if (Input.GetKeyDown(KeyCode.V))
            {
                ResetUrgSensor();
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
            if (toggleUDPConnectionButton != null)
            {
                toggleUDPConnectionButton.GetComponentInChildren<Text>().text = rayDataSender.IsSending ? "切断 (F)" : "再接続 (F)";
            }
        }

        void ResetUrgSensor()
        {
            StartCoroutine(ResetUrgSensorCoroutine());
        }

        private IEnumerator ResetUrgSensorCoroutine()
        {
            if(!isResettingURGSensor)
            {
                isResettingURGSensor = true;
                resetURGInstanceButton.GetComponentInChildren<Text>().text =  "リセット中...";
                // センサーの停止
                rayDataSender.StopSending();
                urgSensor.RestartSensor(); // 新たに追加したRestartSensor()メソッドを呼び出す
                Debug.Log("URGセンサーの受信をリセットしました。");

                // 5秒待機
                yield return new WaitForSeconds(6);

                // センサーの再開
                rayDataSender.StartSending();
                Debug.Log("URGセンサーの受信を再開しました。");
                
                resetURGInstanceButton.GetComponentInChildren<Text>().text =  "URGリセット (V)";
                isResettingURGSensor = false;
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

            if (resetSensorAppButton != null)
            {
                resetSensorAppButton.onClick.RemoveListener(RestartApp.RestartApplication);
            }

            if (toggleUDPConnectionButton != null)
            {
                toggleUDPConnectionButton.onClick.RemoveListener(ToggleConnection);
            }

            if (resetURGInstanceButton != null)
            {
                resetURGInstanceButton.onClick.RemoveListener(ResetUrgSensor);
            }
        }
    }
}
