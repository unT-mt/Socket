using UnityEngine;
using System.Collections;

namespace Urg
{
    public class DebugMouseObstacle : MonoBehaviour
    {
        [Header("Dependencies")]
        public RayVisualizer rayVisualizer;     // 扇形の可視化
        public RayDataSender rayDataSender;     // 距離データ送信用
        public Camera mainCamera;               // マウス座標→ワールド座標変換用

        [Header("Circle Obstacle Settings")]
        public float obstacleRadius = 0.2f;     // 左クリックで作る仮想円 (半径 20cm)
        public float maxRayDistance = 10f;      // 扇形の最大距離

        private bool isSensorActive = false;    // URGセンサーが繋がっていない時だけ動作
        
        void Start()
        {
            // URGセンサーが存在し、かつ接続されていないならばマウスデバッグを使用
            if (rayVisualizer.urg != null && !rayVisualizer.urg.transport.IsConnected())
            {
                Debug.Log("URGセンサーが無効のため、マウスによる障害物シミュレーターを使用します。");
                isSensorActive = true;
            }
            else
            {
                // センサーが有効であればこのスクリプトは何もしない
                isSensorActive = false;
            }
        }

        void Update()
        {
            if (!isSensorActive) return;

            HandleRightClick();   // 右クリックでキューブの生成/削除
            SimulateObstacles();  // 扇形・送信データ用の距離計算
        }

        /// <summary>
        /// 右クリック時に、カーソル下がデバッグキューブなら削除、そうでなければ新規生成
        /// </summary>
        private void HandleRightClick()
        {
            if (Input.GetMouseButtonDown(1)) // 1 = 右クリック
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                // 画面上をRaycastし、何かに当たったか判定
                if (Physics.Raycast(ray, out hit, 100f))
                {
                    // 当たったオブジェクトを調べる
                    GameObject hitObj = hit.collider.gameObject;

                    // 当たった相手がデバッグキューブなら削除
                    if (hitObj.CompareTag("DebugCube"))
                    {
                        Destroy(hitObj);
                    }
                    else
                    {
                        // 別のオブジェクトや地面なら、新しいキューブを生成
                        CreateCubeAt(hit.point);
                    }
                }
                else
                {
                    // 何もない場所(コリジョンなし)なら、そのレイの着地点が不明なので生成は行わないか、
                    // または任意の高さで生成するなど必要に応じて処理を追加してください。
                }
            }
        }

        /// <summary>
        /// 新規にデバッグ用キューブを生成する
        /// </summary>
        /// <param name="position">生成するワールド座標</param>
        private void CreateCubeAt(Vector3 position)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = position;
            cube.transform.localScale = Vector3.one * 0.05f; // 適度なサイズ

            // 見た目を白色に
            var renderer = cube.GetComponent<Renderer>();
            renderer.material.color = Color.green;

            // 物理挙動のためにRigidbodyアタッチ
            cube.AddComponent<Rigidbody>();

            // このオブジェクトを識別するために "DebugCube" タグを設定
            // ※ あらかじめ「DebugCube」というタグをUnityのタグ管理で作成しておく必要があります
            cube.tag = "DebugCube";
        }

        /// <summary>
        /// 毎フレーム、左クリックで円形障害物＋シーン内のキューブを含む総合的な
        /// 距離計算をして RayVisualizer / RayDataSender に反映する
        /// </summary>
        private async void SimulateObstacles()
        {
            int rayCount = rayVisualizer.rayCount;
            float[] distances = new float[rayCount];

            // 1. 全レイを最大距離に初期化
            for (int i = 0; i < rayCount; i++)
            {
                distances[i] = maxRayDistance;
            }

            // 2. 左クリック中なら、マウス位置を中心とする円形障害物を計算
            if (Input.GetMouseButton(0))
            {
                ApplyCircleObstacle(distances);
            }

            // 3. シーン内オブジェクト(右クリック生成キューブ含む)との衝突をRaycastで探す
            ApplyPhysicsObstacle(distances);

            // 4. 結果を RayVisualizer の見た目に反映
            rayVisualizer.UpdateMesh(distances);

            // 5. RayDataSender でUDP送信
            await rayDataSender.SendDataAsync(distances);
        }

        /// <summary>
        /// 左クリックで作成される円形障害物を distances に反映する
        /// </summary>
        private void ApplyCircleObstacle(float[] distances)
        {
            if (Input.mousePosition.x < 0 || Input.mousePosition.x > Screen.width ||
                Input.mousePosition.y < 0 || Input.mousePosition.y > Screen.height) return;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100f))
            {
                // 円形障害物の中心 (XZ 平面上)
                Vector3 obstacleCenter3D = hit.point;
                Vector2 obstacleCenter2D = new Vector2(obstacleCenter3D.x, obstacleCenter3D.z);

                // センサーの位置
                Vector3 sensorPos3D = (rayVisualizer.urg != null) ? rayVisualizer.urg.transform.position : Vector3.zero;
                Vector2 sensorPos2D = new Vector2(sensorPos3D.x, sensorPos3D.z);

                for (int i = 0; i < rayVisualizer.rayCount; i++)
                {
                    float angle = rayVisualizer.StepAngleRadians * i + rayVisualizer.OffsetRadians;
                    Vector3 direction3D = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle)).normalized;
                    Vector2 direction2D = new Vector2(direction3D.x, direction3D.z);

                    // センサー原点から円心までのベクトル
                    Vector2 toCircle = obstacleCenter2D - sensorPos2D;
                    float t = Vector2.Dot(toCircle, direction2D);
                    Vector2 closestPoint = sensorPos2D + t * direction2D;

                    // 円の中心との距離
                    float distToCircleCenter = Vector2.Distance(closestPoint, obstacleCenter2D);

                    // 円の半径以内なら交点がある
                    if (distToCircleCenter <= obstacleRadius)
                    {
                        float offset = Mathf.Sqrt(obstacleRadius * obstacleRadius - distToCircleCenter * distToCircleCenter);
                        float intersectionDistance = t - offset;

                        if (intersectionDistance >= 0 && intersectionDistance < distances[i])
                        {
                            distances[i] = intersectionDistance;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// シーン内のCollider (右クリック生成キューブなど) を Raycast で検知し距離を反映
        /// </summary>
        private void ApplyPhysicsObstacle(float[] distances)
        {
            Vector3 sensorPos = (rayVisualizer.urg != null) ? rayVisualizer.urg.transform.position : Vector3.zero;

            for (int i = 0; i < rayVisualizer.rayCount; i++)
            {
                float angle = rayVisualizer.StepAngleRadians * i + rayVisualizer.OffsetRadians;
                Vector3 direction = new Vector3(-Mathf.Sin(angle), 0, Mathf.Cos(angle)).normalized;

                if (Physics.Raycast(sensorPos, direction, out RaycastHit hit, maxRayDistance))
                {
                    float d = hit.distance;
                    if (d < distances[i])
                    {
                        distances[i] = d;
                    }
                }
            }
        }
    }
}
