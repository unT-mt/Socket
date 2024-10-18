using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class GlobalHotkey : MonoBehaviour
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll")]
    private static extern bool PeekMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);
    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref MSG lpMsg);
    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage(ref MSG lpMsg);

    private const int HOTKEY_ID = 1; // ホットキーのID
    private const uint MOD_ALT = 0x0001; // Altキー
    private const uint VK_F5 = 0x74; // F5キー

    private IntPtr windowHandle; // Unityのウィンドウハンドル

    private struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public System.Drawing.Point pt;
    }

    private const int WM_HOTKEY = 0x0312; // ホットキーが押されたときのメッセージ

    void Start()
    {
        // ウィンドウハンドルを取得
        windowHandle = GetUnityWindowHandle();
        Debug.Log($"Unity Window Handle: {windowHandle}");

        // Alt + F5 をホットキーとして登録
        if (!RegisterHotKey(windowHandle, HOTKEY_ID, MOD_ALT, VK_F5))
        {
            Debug.LogError("ホットキーの登録に失敗しました");
        }
        else
        {
            Debug.Log("ホットキーの登録に成功しました");
        }
    }

    void Update()
    {
        MSG msg;
        while (PeekMessage(out msg, IntPtr.Zero, 0, 0, 1))
        {
            if (msg.message == WM_HOTKEY && msg.wParam == (IntPtr)HOTKEY_ID)
            {
                Debug.Log("ホットキー Alt + F5 が押されました");
                RestartApp.RestartApplication();
            }
            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
        }
    }

    void OnDestroy()
    {
        // アプリ終了時にホットキーを解除
        UnregisterHotKey(windowHandle, HOTKEY_ID);
    }

    // FindWindow関数を使用してUnityウィンドウのハンドルを取得
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    private IntPtr GetUnityWindowHandle()
    {
        // ウィンドウタイトルを使ってUnityのウィンドウハンドルを取得する
        IntPtr handle = FindWindow(null, Application.productName);
        if (handle == IntPtr.Zero)
        {
            Debug.LogError("Unityのウィンドウハンドルが取得できませんでした");
        }
        return handle;
    }
}
