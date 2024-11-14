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
        string clientPath = @"C:\Users\m-takashima\UnityBuild\Socket\Client\Client.exe";
        string serverPath = @"C:\Users\m-takashima\UnityBuild\Socket\Server\Server.exe";

        // Clientのビルド
        PlayerSettings.productName = "Client";
        BuildPipeline.BuildPlayer(new[] { scenes[0] }, clientPath, BuildTarget.StandaloneWindows64, BuildOptions.None);

        // Serverのビルド
        PlayerSettings.productName = "Server";
        BuildPipeline.BuildPlayer(new[] { scenes[1] }, serverPath, BuildTarget.StandaloneWindows64, BuildOptions.None);       
    }
    
    // 1. Build URGSenderSample メニューアイテム
    [MenuItem("Custom Build/Build URGSenderSample")]
    public static void BuildURGSenderSample()
    {
        PlayerSettings.productName = "URGSenderSample";

        // 解像度とウィンドウサイズの設定
        PlayerSettings.defaultScreenWidth = 960;
        PlayerSettings.defaultScreenHeight = 540;
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
        PlayerSettings.productName = "URGReceiverSample";
        // 解像度設定（後で変えられる想定）
        PlayerSettings.defaultScreenWidth = 960;
        PlayerSettings.defaultScreenHeight = 540;
        PlayerSettings.resizableWindow = true;

        // ビルドパス
        string receiverPath = @"C:\Users\m-takashima\UnityBuild\Socket\URGReceiverSample\URGReceiverSample.exe";

        // ビルド実行
        BuildPipeline.BuildPlayer(new[] { urgReceiverSampleScene[0] }, receiverPath, BuildTarget.StandaloneWindows64, BuildOptions.None);       
    }
}
