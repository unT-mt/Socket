using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 4つのVector2 (右奥・左奥・左手前・右手前) を入力して
/// センサー中心を基準に赤い四角を描画するクラスの例
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class DebugRectangleDrawer : MonoBehaviour
{
    [Header("Sensor Transform")]
    [Tooltip("Rayを発するセンサー(中心)のTransformをアサインする")]
    public Transform sensorTransform;

    [Header("Input Fields (8 total)")]
    public InputField frontRightX; // 右手前 X
    public InputField frontRightY; // 右手前 Y

    public InputField frontLeftX;  // 左手前 X
    public InputField frontLeftY;  // 左手前 Y

    public InputField backLeftX;   // 左奥 X
    public InputField backLeftY;   // 左奥 Y

    public InputField backRightX;  // 右奥 X
    public InputField backRightY;  // 右奥 Y

    [Header("Line Settings")]
    [Tooltip("線の太さ (start/end width) を指定")]
    public float lineWidth = 0.02f;

    private LineRenderer lineRenderer;

    // 4点のローカル座標 (X,Z平面に投影するとして、Vector2をX=横, Y=奥行きとみなす)
    private Vector2 frontRight;
    private Vector2 frontLeft;
    private Vector2 backLeft;
    private Vector2 backRight;

    // PlayerPrefs に使用するキー名
    private const string KEY_FRX = "FRX";
    private const string KEY_FRY = "FRY";
    private const string KEY_FLX = "FLX";
    private const string KEY_FLY = "FLY";
    private const string KEY_BLX = "BLX";
    private const string KEY_BLY = "BLY";
    private const string KEY_BRX = "BRX";
    private const string KEY_BRY = "BRY";

    private void Awake()
    {
        // このGameObjectにLineRendererが無ければ追加する
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        // 線の見た目を設定 (デフォルトで赤)
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.loop = true;   // 4つの点をループさせて閉じた形にする

        // マテリアルをUnlit/Colorなどにして赤くするとシンプル
        // ※任意のマテリアルに差し替えてください
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = Color.red;
        lineRenderer.material = mat;

        // ワールド座標系で描画するかどうか
        //   true : センサーが動くと線が追従しない(絶対座標)
        //   false: このLineRendererオブジェクトがセンサーの子要素ならローカル座標系で描画
        lineRenderer.useWorldSpace = false;

        // ▼ PlayerPrefs から値を読み込み (存在しなければデフォルト値)
        float frx = PlayerPrefs.GetFloat(KEY_FRX, 0.96f);
        float fry = PlayerPrefs.GetFloat(KEY_FRY, 1.28f);
        float flx = PlayerPrefs.GetFloat(KEY_FLX, -0.96f);
        float fly = PlayerPrefs.GetFloat(KEY_FLY, 1.28f);
        float blx = PlayerPrefs.GetFloat(KEY_BLX, -0.96f);
        float bly = PlayerPrefs.GetFloat(KEY_BLY, 0.20f);
        float brx = PlayerPrefs.GetFloat(KEY_BRX, 0.96f);
        float bry = PlayerPrefs.GetFloat(KEY_BRY, 0.20f);

        frontRight = new Vector2(frx, fry);
        frontLeft  = new Vector2(flx, fly);
        backLeft   = new Vector2(blx, bly);
        backRight  = new Vector2(brx, bry);
    }

    private void Start()
    {
        // UIのInputFieldに読み込んだ値を反映
        frontRightX.text = frontRight.x.ToString("F2");
        frontRightY.text = frontRight.y.ToString("F2");

        frontLeftX.text = frontLeft.x.ToString("F2");
        frontLeftY.text = frontLeft.y.ToString("F2");

        backLeftX.text = backLeft.x.ToString("F2");
        backLeftY.text = backLeft.y.ToString("F2");

        backRightX.text = backRight.x.ToString("F2");
        backRightY.text = backRight.y.ToString("F2");

        // 初期描画
        UpdateLinePositions();
    }

    /// <summary>
    /// InputField の値が変わったら呼び出す (OnValueChanged か OnEndEdit で)
    /// </summary>
    public void OnInputChanged()
    {
        // それぞれのテキストを float に変換できるなら変換し、構造体に反映
        float frx, fry, flx, fly, blx, bly, brx, bry;

        // 失敗した場合は無視するようにTryParse
        if (float.TryParse(frontRightX.text, out frx)) frontRight.x = frx;
        if (float.TryParse(frontRightY.text, out fry)) frontRight.y = fry;

        if (float.TryParse(frontLeftX.text, out flx)) frontLeft.x = flx;
        if (float.TryParse(frontLeftY.text, out fly)) frontLeft.y = fly;

        if (float.TryParse(backLeftX.text, out blx)) backLeft.x = blx;
        if (float.TryParse(backLeftY.text, out bly)) backLeft.y = bly;

        if (float.TryParse(backRightX.text, out brx)) backRight.x = brx;
        if (float.TryParse(backRightY.text, out bry)) backRight.y = bry;

        // PlayerPrefs への保存
        PlayerPrefs.SetFloat(KEY_FRX, frontRight.x);
        PlayerPrefs.SetFloat(KEY_FRY, frontRight.y);

        PlayerPrefs.SetFloat(KEY_FLX, frontLeft.x);
        PlayerPrefs.SetFloat(KEY_FLY, frontLeft.y);

        PlayerPrefs.SetFloat(KEY_BLX, backLeft.x);
        PlayerPrefs.SetFloat(KEY_BLY, backLeft.y);

        PlayerPrefs.SetFloat(KEY_BRX, backRight.x);
        PlayerPrefs.SetFloat(KEY_BRY, backRight.y);

        PlayerPrefs.Save();

        // ラインを再描画
        UpdateLinePositions();
    }

    /// <summary>
    /// 4つのローカル座標(ベクトル2)をLineRenderer用に変換し、赤い四角を描画
    /// </summary>
    private void UpdateLinePositions()
    {
        // LineRendererに渡すのは Vector3[]
        //   x => local x
        //   y => local z  (仮に高さは0とし、XZ平面で見た四角にする)
        // センサーのローカル座標に合わせるなら useWorldSpace = false 推奨
        // ※ transform自体がsensorTransformの子になっていると仮定

        Vector3[] corners = new Vector3[4];
        corners[0] = new Vector3(frontRight.x, 0f, frontRight.y); // 右手前
        corners[1] = new Vector3(frontLeft.x,  0f, frontLeft.y);  // 左手前
        corners[2] = new Vector3(backLeft.x,   0f, backLeft.y);   // 左奥
        corners[3] = new Vector3(backRight.x,  0f, backRight.y);  // 右奥

        lineRenderer.positionCount = corners.Length;
        lineRenderer.SetPositions(corners);
    }
}
