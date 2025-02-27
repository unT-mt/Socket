using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Urg
{
    public class RayDataSender : MonoBehaviour
    {
        public UrgSensor urg; // UrgSensor コンポーネントへの参照
        public string serverIP = "127.0.0.1"; // 送信先のIPアドレス
        public int serverPort = 5000; // 送信先のポート番号
        public float sendInterval = 0.05f; // 送信間隔 (秒) - 20Hz
        public float maxDistance = 10f; // 最大距離

        private UdpClient udpClient;
        private IPEndPoint remoteEndPoint;
        private float lastSendTime = 0f;

        // 送信状態を示すプロパティ
        public bool IsSending { get; private set; } = true;

        void Start()
        {
            InitializeUdpClient();
            if (urg != null)
            {
                urg.OnDistanceReceived += Urg_OnDistanceReceived;
            }
            else
            {
                Debug.LogWarning("UrgSensor が割り当てられていません。");
            }
        }

        void InitializeUdpClient()
        {
            udpClient = new UdpClient();
            remoteEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), serverPort);
        }

        private async void Urg_OnDistanceReceived(DistanceRecord data)
        {
            if (data == null || data.RawDistances == null || data.RawDistances.Length != 1081)
            {
                Debug.LogWarning("受信した距離データが不正です。");
                return;
            }

            float[] distances = data.RawDistances;

            // 送信間隔をチェック
            float currentTime = Time.time;
            if (currentTime - lastSendTime < sendInterval)
            {
                return; // 間隔が足りない場合は送信しない
            }
            lastSendTime = currentTime;

            await SendDataAsync(distances); // 非同期メソッドをawait
        }

        public async Task SendDataAsync(float[] distances)
        {
            if (!IsSending)
            return;

            if (distances == null)
            {
                Debug.LogWarning("Invalid distances array for sending.");
                return;
            }

            // CSVフォーマット: id,distance;id,distance;...
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < distances.Length; i++)
            {
                float distance = Mathf.Clamp(distances[i], 0f, maxDistance);
                sb.Append($"{i},{distance}");
                if (i < distances.Length - 1)
                {
                    sb.Append(";");
                }
            }

            string csvData = sb.ToString();
            byte[] sendData = Encoding.UTF8.GetBytes(csvData + "\n");

            try
            {
                await udpClient.SendAsync(sendData, sendData.Length, remoteEndPoint); // 非同期送信
                //Debug.Log($"送信: {csvData}");
            }
            catch (Exception e)
            {
                Debug.LogError($"送信エラー: {e.Message}");
            }
        }

        public void StartSending()
        {
            if (!IsSending)
            {
                IsSending = true;
                if (udpClient != null)
                {
                    udpClient.Close();
                }
                udpClient = new UdpClient();
                Debug.Log("UDP送信を再開しました。");
            }
        }

        public void StopSending()
        {
            if (IsSending)
            {
                IsSending = false;
                udpClient.Close();
                Debug.Log("UDP送信を停止しました。");
            }
        }

        private void OnDestroy()
        {
            if (udpClient != null)
            {
                udpClient.Close();
            }

            if (urg != null)
            {
                urg.OnDistanceReceived -= Urg_OnDistanceReceived;
            }
        }
    }
}
