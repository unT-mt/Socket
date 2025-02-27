using UnityEngine;
using System;
using System.Runtime.InteropServices;

public class WindowControl : MonoBehaviour
{
    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    static extern bool IsWindowVisible(IntPtr hWnd);

    private static readonly IntPtr HWND_BOTTOM = new IntPtr(1); 
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;

    private IntPtr unityWindow;
    private bool isWindowAtBack = false; // ウィンドウがバックにあるかどうか

    void Start()
    {
        unityWindow = FindWindow(null, Application.productName);
        if (unityWindow != IntPtr.Zero)
        {
            SetWindowToBackground();
        }
        else
        {
            Debug.LogError("WindowControl:Unityのウィンドウハンドルが取得できませんでした");
        }
    }

    void Update()
    {
        // ウィンドウがフォアグラウンドに出た場合のみ、バックグラウンドに戻す
        if (unityWindow != IntPtr.Zero && !isWindowAtBack)
        {
            SetWindowToBackground();
        }
    }

    private void SetWindowToBackground()
    {
        // フォアグラウンドにいるかどうかを確認し、必要な場合のみ実行
        SetWindowPos(unityWindow, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        isWindowAtBack = true; // 一度バックに移動させたら、次の確認まで再度実行しない
    }
}
