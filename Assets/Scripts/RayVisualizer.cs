using UnityEngine;
using System.Collections.Generic;
using System.Threading;

namespace Urg
{
    [RequireComponent(typeof(LineRenderer))]
    public class RayVisualizer : MonoBehaviour
    {
        public UrgSensor urg; // UrgSensor コンポーネントへの参照
        public Material lineMaterial; // LineRenderer に使用するマテリアル
        public float maxDistance = 10f; // 最大距離
        public int rayCount = 1081; // 光線の数 (ID: 0～1080)

        private List<LineRenderer> lineRenderers = new List<LineRenderer>();
        private DistanceRecord latestData = null;
        private readonly object dataLock = new object();

        void Awake()
        {
            InitializeLines();
            if (urg != null)
            {
                urg.OnDistanceReceived += Urg_OnDistanceReceived;
            }
            else
            {
                Debug.LogWarning("UrgSensor が割り当てられていません。");
            }
        }

        void InitializeLines()
        {
            // 光線ごとに LineRenderer を生成
            for (int i = 0; i < rayCount; i++)
            {
                GameObject lineObj = new GameObject($"Ray_{i}");
                lineObj.transform.parent = this.transform;

                LineRenderer lr = lineObj.AddComponent<LineRenderer>();
                lr.positionCount = 2;
                lr.startWidth = 0.05f;
                lr.endWidth = 0.05f;
                lr.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
                lr.startColor = Color.blue;
                lr.endColor = Color.blue;
                lr.useWorldSpace = true;

                lineRenderers.Add(lr);
            }
        }

        private void Urg_OnDistanceReceived(DistanceRecord data)
        {
            // スレッドセーフに最新データを保存
            lock (dataLock)
            {
                latestData = data;
            }
        }

        void Update()
        {
            DistanceRecord dataToProcess = null;

            // スレッドセーフにデータを取得
            lock (dataLock)
            {
                if (latestData != null)
                {
                    dataToProcess = latestData;
                    latestData = null; // 処理後はクリア
                }
            }

            if (dataToProcess == null || dataToProcess.RawDistances == null || dataToProcess.RawDistances.Length != rayCount)
            {
                return;
            }

            float[] distances = dataToProcess.RawDistances;

            for (int i = 0; i < rayCount; i++)
            {
                float distance = Mathf.Clamp(distances[i], 0f, maxDistance);
                float angle = StepAngleRadians * i + OffsetRadians;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                Vector3 startPos = urg.transform.position;
                Vector3 endPos = startPos + direction * distance;

                LineRenderer lr = lineRenderers[i];
                lr.SetPosition(0, startPos);
                lr.SetPosition(1, endPos);
            }
        }

        private float StepAngleRadians => Mathf.Deg2Rad * urg.stepAngleDegrees;
        private float OffsetRadians => Mathf.Deg2Rad * urg.offsetDegrees;

        private void OnDestroy()
        {
            if (urg != null)
            {
                urg.OnDistanceReceived -= Urg_OnDistanceReceived;
            }
        }
    }
}
