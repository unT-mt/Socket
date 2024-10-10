using UnityEngine;
using System.Collections.Generic;

namespace Urg
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class RayVisualizerMesh : MonoBehaviour
    {
        public UrgSensor urg; // UrgSensor コンポーネントへの参照
        public Material lineMaterial; // MeshRenderer に使用するマテリアル
        public float maxDistance = 10f; // 最大距離
        public int rayCount = 1081; // 光線の数 (ID: 0～1080)

        private Mesh mesh;
        private DistanceRecord latestData = null;
        private readonly object dataLock = new object();

        void Awake()
        {
            InitializeMesh();
            if (urg != null)
            {
                urg.OnDistanceReceived += Urg_OnDistanceReceived;
            }
            else
            {
                Debug.LogWarning("UrgSensor が割り当てられていません。");
            }
        }

        void InitializeMesh()
        {
            MeshFilter mf = GetComponent<MeshFilter>();
            mesh = new Mesh();
            mf.mesh = mesh;

            MeshRenderer mr = GetComponent<MeshRenderer>();
            mr.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Unlit/Color"));

            // 初期頂点とインデックスの設定
            Vector3[] vertices = new Vector3[rayCount * 2];
            int[] indices = new int[rayCount * 2];

            for (int i = 0; i < rayCount * 2; i++)
            {
                vertices[i] = Vector3.zero;
                indices[i] = i;
            }

            mesh.vertices = vertices;
            mesh.SetIndices(indices, MeshTopology.Lines, 0);
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
            Vector3[] vertices = new Vector3[rayCount * 2];

            for (int i = 0; i < rayCount; i++)
            {
                float distance = Mathf.Clamp(distances[i], 0f, maxDistance);
                float angle = StepAngleRadians * i + OffsetRadians;
                Vector3 direction = new Vector3(-Mathf.Sin(angle), 0, Mathf.Cos(angle));
                Vector3 startPos = urg.transform.position;
                Vector3 endPos = startPos + direction * distance;

                vertices[i * 2] = startPos;
                vertices[i * 2 + 1] = endPos;
            }

            mesh.vertices = vertices;
            mesh.RecalculateBounds();
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
