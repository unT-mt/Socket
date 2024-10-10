using UnityEditor;
using System.IO;

public class BuildScript
{
    static string[] scenes = { "Assets/Scenes/Client.unity", "Assets/Scenes/Server.unity" };
    static string[] urgSenderSampleScene = {"Assets/Scenes/URGSenderSample.unity"};
    static string[] urgReceiverSampleScene = {"Assets/Scenes/URGReceiverSample.unity"};

    [MenuItem("Custom Build/Build Client and Server")]
    public static void BuildClientAndServer()
    {
        // 出力先のパス
        string clientPath = @"C:\Users\m-takashima\UnityBuild\Socket\Client\Client.exe";
        string serverPath = @"C:\Users\m-takashima\UnityBuild\Socket\Server\Server.exe";

        // Clientのビルド
        BuildPipeline.BuildPlayer(new[] { scenes[0] }, clientPath, BuildTarget.StandaloneWindows64, BuildOptions.None);

        // Serverのビルド
        BuildPipeline.BuildPlayer(new[] { scenes[1] }, serverPath, BuildTarget.StandaloneWindows64, BuildOptions.None);
    }
    
    // 1. Build URGSenderSample メニューアイテム
    [MenuItem("Custom Build/Build URGSenderSample")]
    public static void BuildURGSenderSample()
    {
        // 解像度とウィンドウサイズの設定
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.resizableWindow = true;

        // ビルドパス
        string senderPath = @"C:\Users\m-takashima\UnityBuild\Socket\URGSenderSample\URGSenderSample.exe";
        
        // ビルド実行
        BuildPipeline.BuildPlayer(new[] { urgSenderSampleScene[0] }, senderPath, BuildTarget.StandaloneWindows64, BuildOptions.None);
    }

    // 2. Build URGReceiverSample メニューアイテム
    [MenuItem("Custom Build/Build URGReceiverSample")]
    public static void BuildURGReceiverSample()
    {
        // 解像度設定（後で変えられる想定）
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.resizableWindow = false; // resizableWindowをオフにする

        // ビルドパス
        string receiverPath = @"C:\Users\m-takashima\UnityBuild\Socket\URGReceiverSample\URGReceiverSample.exe";

        // ビルド実行
        BuildPipeline.BuildPlayer(new[] { urgReceiverSampleScene[0] }, receiverPath, BuildTarget.StandaloneWindows64, BuildOptions.None);
    }
}
