using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using WindowsInput;
using WindowsInput.Native;

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

        private InputSimulator inputSimulator;

        public Dropdown restartKeyDropdown;
        public Dropdown toggleKeyDropdown;
        public Dropdown resetSensorKeyDropdown;

        private Dictionary<string, VirtualKeyCode> keyMappings;

        private Dictionary<string, bool> previousKeyState;

        void Start()
        {
            inputSimulator = new InputSimulator(); // InputSimulatorの初期化

            // 前回のキー状態を追跡する辞書を初期化
            previousKeyState = new Dictionary<string, bool>
            {
                { "RestartKey", false },
                { "ToggleKey", false },
                { "ResetSensorKey", false }
            };

            // 初回起動時にデフォルトのキーバインディングを設定
            InitializeDefaultKeyBindings();

            // UIボタンの設定
            if (resetSensorAppButton != null)
            {
                resetSensorAppButton.onClick.AddListener(RestartApp.RestartApplication);
            }

            if (toggleUDPConnectionButton != null)
            {
                toggleUDPConnectionButton.onClick.AddListener(ToggleConnection);
                UpdateButtonLabel();
            }

            if (resetURGInstanceButton != null)
            {
                resetURGInstanceButton.onClick.AddListener(ResetUrgSensor);
            }

            // キーバインディングの設定
            InitializeKeyMappings();
            InitializeDropdowns();

            // UDPリスナー初期化
            InitializeUdpListener();
        }

        private void InitializeDefaultKeyBindings()
        {
            // 初回起動時のみデフォルトのキー設定を行う
            if (!PlayerPrefs.HasKey("RestartKey"))
            {
                PlayerPrefs.SetString("RestartKey", "VK_R");
            }

            if (!PlayerPrefs.HasKey("ToggleKey"))
            {
                PlayerPrefs.SetString("ToggleKey", "VK_F");
            }

            if (!PlayerPrefs.HasKey("ResetSensorKey"))
            {
                PlayerPrefs.SetString("ResetSensorKey", "VK_V");
            }

            PlayerPrefs.Save(); // 設定を保存
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

        void InitializeKeyMappings()
        {
            // VirtualKeyCodeのマッピングを設定
            keyMappings = new Dictionary<string, VirtualKeyCode>();

            // キーボードの標準的なキーのみを追加 (A～Z, 数字, F1～F12, 矢印キー)
            List<VirtualKeyCode> allowedKeys = new List<VirtualKeyCode>
            {
                VirtualKeyCode.VK_A, VirtualKeyCode.VK_B, VirtualKeyCode.VK_C, VirtualKeyCode.VK_D, VirtualKeyCode.VK_E,
                VirtualKeyCode.VK_F, VirtualKeyCode.VK_G, VirtualKeyCode.VK_H, VirtualKeyCode.VK_I, VirtualKeyCode.VK_J,
                VirtualKeyCode.VK_K, VirtualKeyCode.VK_L, VirtualKeyCode.VK_M, VirtualKeyCode.VK_N, VirtualKeyCode.VK_O,
                VirtualKeyCode.VK_P, VirtualKeyCode.VK_Q, VirtualKeyCode.VK_R, VirtualKeyCode.VK_S, VirtualKeyCode.VK_T,
                VirtualKeyCode.VK_U, VirtualKeyCode.VK_V, VirtualKeyCode.VK_W, VirtualKeyCode.VK_X, VirtualKeyCode.VK_Y, VirtualKeyCode.VK_Z,
                VirtualKeyCode.VK_0, VirtualKeyCode.VK_1, VirtualKeyCode.VK_2, VirtualKeyCode.VK_3, VirtualKeyCode.VK_4,
                VirtualKeyCode.VK_5, VirtualKeyCode.VK_6, VirtualKeyCode.VK_7, VirtualKeyCode.VK_8, VirtualKeyCode.VK_9,
                VirtualKeyCode.F1, VirtualKeyCode.F2, VirtualKeyCode.F3, VirtualKeyCode.F4, VirtualKeyCode.F5,
                VirtualKeyCode.F6, VirtualKeyCode.F7, VirtualKeyCode.F8, VirtualKeyCode.F9, VirtualKeyCode.F10, VirtualKeyCode.F11, VirtualKeyCode.F12,
                VirtualKeyCode.LEFT, VirtualKeyCode.RIGHT, VirtualKeyCode.UP, VirtualKeyCode.DOWN
            };

            // フィルタリングしたキーのみ辞書に追加
            foreach (VirtualKeyCode key in allowedKeys)
            {
                keyMappings[key.ToString()] = key;
            }
        }

        private void InitializeDropdowns()
        {
            // フィルタリングされたキーのみオプションとして追加
            List<string> keyOptions = new List<string>(keyMappings.Keys);

            restartKeyDropdown.ClearOptions();
            toggleKeyDropdown.ClearOptions();
            resetSensorKeyDropdown.ClearOptions();

            restartKeyDropdown.AddOptions(keyOptions);
            toggleKeyDropdown.AddOptions(keyOptions);
            resetSensorKeyDropdown.AddOptions(keyOptions);

            // 保存された設定の読み込み
            restartKeyDropdown.value = keyOptions.IndexOf(PlayerPrefs.GetString("RestartKey", "VK_R"));
            toggleKeyDropdown.value = keyOptions.IndexOf(PlayerPrefs.GetString("ToggleKey", "VK_F"));
            resetSensorKeyDropdown.value = keyOptions.IndexOf(PlayerPrefs.GetString("ResetSensorKey", "VK_V"));

            // キーバインディング変更時の処理
            restartKeyDropdown.onValueChanged.AddListener(delegate { OnKeyBindingChanged("RestartKey", restartKeyDropdown); });
            toggleKeyDropdown.onValueChanged.AddListener(delegate { OnKeyBindingChanged("ToggleKey", toggleKeyDropdown); });
            resetSensorKeyDropdown.onValueChanged.AddListener(delegate { OnKeyBindingChanged("ResetSensorKey", resetSensorKeyDropdown); });
        }

        private void OnKeyBindingChanged(string keyName, Dropdown dropdown)
        {
            string selectedKey = dropdown.options[dropdown.value].text;
            PlayerPrefs.SetString(keyName, selectedKey);
            PlayerPrefs.Save();
        }

        void Update()
        {
            CheckKeyPress("RestartKey", () => RestartApp.RestartApplication());
            CheckKeyPress("ToggleKey", () => ToggleConnection());
            CheckKeyPress("ResetSensorKey", () => ResetUrgSensor());
        }

        private void CheckKeyPress(string keyName, Action action)
        {
            string key = PlayerPrefs.GetString(keyName, keyName == "RestartKey" ? "VK_R" : keyName == "ToggleKey" ? "VK_F" : "VK_V");
            
            // 現在のキーの状態を取得
            bool isKeyDown = inputSimulator.InputDeviceState.IsKeyDown(keyMappings[key]);

            // 前回のキー状態と比較し、現在押されていて前回押されていない場合にのみ処理を実行
            if (isKeyDown && !previousKeyState[keyName])
            {
                action.Invoke(); // 指定されたアクションを実行
            }

            // 現在のキー状態を保存
            previousKeyState[keyName] = isKeyDown;
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
                toggleUDPConnectionButton.GetComponentInChildren<Text>().text = rayDataSender.IsSending ? "切断" : "再接続";
            }
        }

        void ResetUrgSensor()
        {
            StartCoroutine(ResetUrgSensorCoroutine());
        }

        private IEnumerator ResetUrgSensorCoroutine()
        {
            if (!isResettingURGSensor)
            {
                isResettingURGSensor = true;
                resetURGInstanceButton.GetComponentInChildren<Text>().text = "リセット中...";
                rayDataSender.StopSending();
                urgSensor.RestartSensor();
                yield return new WaitForSeconds(6);
                rayDataSender.StartSending();
                resetURGInstanceButton.GetComponentInChildren<Text>().text = "URGリセット";
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
