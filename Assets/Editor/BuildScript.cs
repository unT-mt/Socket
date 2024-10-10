using UnityEditor;
using System.IO;

public class BuildScript
{
    static string[] scenes = { "Assets/Scenes/Client.unity", "Assets/Scenes/Server.unity" };

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
}
