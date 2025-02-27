using UnityEngine;
using WindowsInput;
using WindowsInput.Native;

public class GlobalHotkeyManager : MonoBehaviour
{
    private InputSimulator inputSimulator;

    void Start()
    {
        inputSimulator = new InputSimulator();

        // アプリ起動時にホットキー Alt + F5 を設定
        RegisterHotkey();
    }

    void Update()
    {
        // ホットキーが押されたか確認
        if (IsAltF5Pressed())
        {
            Debug.Log("Alt + F5 が押されました");
            RestartApp.RestartApplication();
        }
    }

    private bool IsAltF5Pressed()
    {
        // Alt + F5 が押されたかチェック
        return inputSimulator.InputDeviceState.IsKeyDown(VirtualKeyCode.MENU) && 
               inputSimulator.InputDeviceState.IsKeyDown(VirtualKeyCode.F5);
    }

    private void RegisterHotkey()
    {
        Debug.Log("Alt + F5 のホットキーを登録しました");
    }
}
