using UnityEngine;
using System.Diagnostics;  // 外部プロセスを使用するために必要

public class RestartApp : MonoBehaviour
{
    public static void RestartApplication()
    {
        // 現在のアプリの実行ファイルパスを取得
        string appPath = Application.dataPath.Replace("_Data", ".exe");

        // 新しいプロセスでアプリを起動
        Process.Start(appPath);

        // アプリを終了
        Application.Quit();
    }
}
