using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ObstacleTouchManager : MonoBehaviour
{
    [Header("UI Settings")]
    public Button uiButton; // 操作対象のUIButton
    public ObstacleReceiver obstacleReceiver; // 障害物データを受け取るスクリプト
    public float maxDistance = 10f; // 最大距離

    private int? touchId = null; // 現在のタッチ位置ID
    private Vector2 touchPosition; // タッチの画面座標
    private float obstacleSize; // 障害物サイズ（円の半径）

    void Update()
    {
        // センサー情報からタッチを計算
        CalculateTouchFromObstacles();

        // タッチが検出されたらUIButtonを操作
        if (touchId.HasValue)
        {
            SimulateTouchOnButton();
        }
    }

    void CalculateTouchFromObstacles()
    {
        // ObstacleReceiverからデータを取得
        Dictionary<int, float> obstacleData = obstacleReceiver.GetObstacleData();

        // 障害物がない場合はリセット
        if (obstacleData == null || obstacleData.Count == 0)
        {
            touchId = null;
            return;
        }

        // 障害物がある箇所（距離が10mより小さいID）を取得
        List<int> obstacleIds = new List<int>();
        foreach (var entry in obstacleData)
        {
            if (entry.Value < maxDistance)
            {
                obstacleIds.Add(entry.Key);
            }
        }

        if (obstacleIds.Count == 0)
        {
            touchId = null;
            return;
        }

        // 最小ID、最大ID、中間IDを取得
        int minId = obstacleIds[0];
        int maxId = obstacleIds[obstacleIds.Count - 1];
        int midId = (minId + maxId) / 2;

        // タッチ位置（中間IDの角度を利用して算出）
        touchPosition = CalculateTouchPosition(midId);
        
        // 障害物のサイズを計算（ID範囲の広さから半径を推定）
        obstacleSize = CalculateObstacleSize(minId, maxId);

        // タッチIDを更新
        touchId = midId;
    }

    Vector2 CalculateTouchPosition(int id)
    {
        // 光線の角度を計算 (ObstacleReceiverと同じ計算方法)
        float stepAngleDegrees = 0.25f;
        float offsetDegrees = -135f;
        float angleDegrees = stepAngleDegrees * id + offsetDegrees;
        float angleRadians = angleDegrees * Mathf.Deg2Rad;

        // 画面上のタッチ位置を算出（ここでは簡易的にスクリーン座標に変換）
        float screenX = Screen.width * (0.5f + Mathf.Sin(angleRadians) * 0.5f);
        float screenY = Screen.height * (0.5f + Mathf.Cos(angleRadians) * 0.5f);
        return new Vector2(screenX, screenY);
    }

    float CalculateObstacleSize(int minId, int maxId)
    {
        // ID範囲からサイズ（円の半径）を計算
        float stepAngleDegrees = 0.25f;
        float angleRange = (maxId - minId) * stepAngleDegrees;
        return angleRange / 2f; // 簡易的な半径計算
    }

    void SimulateTouchOnButton()
    {
        // タッチ位置がボタン領域内か確認し、操作をシミュレート
        RectTransform buttonRect = uiButton.GetComponent<RectTransform>();
        if (RectTransformUtility.RectangleContainsScreenPoint(buttonRect, touchPosition))
        {
            Debug.Log("UIButtonがタッチされました！");
            uiButton.onClick.Invoke();
        }
    }
}
