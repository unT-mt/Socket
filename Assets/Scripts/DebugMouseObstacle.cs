using UnityEngine;
using System;

namespace Urg
{
    public class DebugMouseObstacle : MonoBehaviour
    {
        public RayVisualizer rayVisualizer;
        public RayDataSender rayDataSender;
        public Camera mainCamera;
        public float obstacleRadius = 0.2f; // 20cmの半径
        public float maxRayDistance = 10f; // Rayの最大距離
        private bool isSensorActive = false;

        void Start()
        {
            // URGセンサーが接続されていない場合にのみこのシミュレーターを有効にする
            if (!rayVisualizer.urg.transport.IsConnected())
            {
                Debug.Log("URGセンサーが無効のため、マウスによる障害物シミュレーターを使用します。");
                isSensorActive = true;
            }
            else
            {
                return;
            }
        }

        void Update()
        {
            if (!isSensorActive) return;

            SimulateMouseObstacle();
        }

        private async void SimulateMouseObstacle()
        {
            float[] distances = new float[rayVisualizer.rayCount];

            // 全ての光線を最大距離に設定
            for (int i = 0; i < rayVisualizer.rayCount; i++)
            {
                distances[i] = maxRayDistance; // 最大距離を設定
            }

            // マウスが画面内にあるか確認
            if (Input.mousePosition.x >= 0 && Input.mousePosition.x <= Screen.width &&
                Input.mousePosition.y >= 0 && Input.mousePosition.y <= Screen.height)
            {
                // マウス位置をワールド座標に変換
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                // マウス位置でのRayのヒットを確認 (地面や特定の平面にヒットさせる)
                if (Physics.Raycast(ray, out hit, 100.0f) && Input.GetMouseButton(0))
                {
                    Vector3 obstacleCenter = hit.point;

                    // RayVisualizerのセンサー位置を取得
                    Vector3 sensorPos = rayVisualizer.urg != null ? rayVisualizer.urg.transform.position : Vector3.zero;

                    for (int i = 0; i < rayVisualizer.rayCount; i++)
                    {
                        float angle = rayVisualizer.StepAngleRadians * i + rayVisualizer.OffsetRadians;
                        Vector3 direction = new Vector3(-Mathf.Sin(angle), 0, Mathf.Cos(angle)).normalized;

                        // Ray origin and direction
                        Vector3 rayOrigin = sensorPos;
                        Vector3 rayDir = direction;

                        // Compute intersection with obstacle circle in XZ-plane
                        Vector2 rayOrigin2D = new Vector2(rayOrigin.x, rayOrigin.z);
                        Vector2 rayDir2D = new Vector2(rayDir.x, rayDir.z);
                        Vector2 obstacleCenter2D = new Vector2(obstacleCenter.x, obstacleCenter.z);

                        Vector2 toCircle = obstacleCenter2D - rayOrigin2D;
                        float t = Vector2.Dot(toCircle, rayDir2D);
                        Vector2 closestPoint = rayOrigin2D + t * rayDir2D;
                        float distToCircleCenter = Vector2.Distance(closestPoint, obstacleCenter2D);

                        if (distToCircleCenter <= obstacleRadius)
                        {
                            // Compute distance from ray origin to intersection point
                            float offset = Mathf.Sqrt(obstacleRadius * obstacleRadius - distToCircleCenter * distToCircleCenter);
                            float intersectionDistance = t - offset;

                            if (intersectionDistance >= 0 && intersectionDistance <= maxRayDistance)
                            {
                                distances[i] = intersectionDistance;
                            }
                            else
                            {
                                distances[i] = maxRayDistance;
                            }
                        }
                        else
                        {
                            distances[i] = maxRayDistance;
                        }
                    }
                }
            }

            // RayVisualizerにデータを設定
            rayVisualizer.UpdateMesh(distances);

            // RayDataSenderにデータを送信
            await rayDataSender.SendDataAsync(distances); // 必要に応じて有効化
        }
    }
}
