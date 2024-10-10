using UnityEngine;

public class URGSenderCameraController : MonoBehaviour
{
    [Header("Rotation Settings")]
    public KeyCode rotateKey = KeyCode.Space; // 回転をトリガーするキー
    public float rotationAngle = 180f; // 回転角度

    [Header("Zoom Settings")]
    public float zoomSpeed = 5f; // ズーム速度
    public float minY = 5f; // 最小Y座標
    public float maxY = 20f; // 最大Y座標
    public float pinchZoomSpeed = 0.1f; // ピンチズーム速度

    [Header("Panning Settings")]
    public float panSpeed = 0.5f; // パニング速度
    public float panSensitivity = 1f; // マウスドラッグの感度

    private float targetY; // 現在のターゲットY座標
    private Quaternion targetRotation; // 現在のターゲット回転

    private Vector3 lastMousePosition; // 前回のマウス位置

    private bool isUpsideDown=false;

    void Start()
    {
        // 初期設定
        targetY = transform.position.y;
        targetRotation = transform.rotation;
    }

    void Update()
    {
        HandleRotation();
        HandleZoom();
        HandlePanning();
    }

    /// <summary>
    /// スペースキー押下時にカメラのZ軸回転を180度増やす
    /// </summary>
    void HandleRotation()
    {
        if (Input.GetKeyDown(rotateKey))
        {
            // 現在の回転に180度を加算
            targetRotation *= Quaternion.Euler(0, 0, rotationAngle);
            isUpsideDown = !isUpsideDown;
        }

        // スムーズに回転を適用
        transform.rotation = targetRotation;
    }

    /// <summary>
    /// マウスホイールやタッチ入力によるズームイン・ズームアウト
    /// </summary>
    void HandleZoom()
    {
        // マウスホイールによるズーム
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0f)
        {
            targetY -= scrollInput * zoomSpeed;
        }

        // ターゲットYをクランプ
        targetY = Mathf.Clamp(targetY, minY, maxY);

        // スムーズにY座標を適用
        Vector3 newPosition = transform.position;
        newPosition.y = Mathf.Lerp(transform.position.y, targetY, Time.deltaTime * 5f);
        transform.position = newPosition;
    }

    /// <summary>
    /// マウス中ボタン押下時のドラッグでカメラをXZ方向に移動
    /// </summary>
    void HandlePanning()
    {
        if (Input.GetMouseButtonDown(2) || (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButtonDown(0))) // 中ボタン押下
        {
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButton(2)|| (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(0))) // 中ボタンを押しながらドラッグ
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            lastMousePosition = Input.mousePosition;

            // カメラのローカル座標系での移動
            Vector3 move = new Vector3(-delta.x * panSpeed * Time.deltaTime * panSensitivity, 0, -delta.y * panSpeed * Time.deltaTime * panSensitivity);
            if(isUpsideDown) transform.Translate(-move, Space.World);
            if(!isUpsideDown) transform.Translate(move, Space.World);
        }
    }
}
