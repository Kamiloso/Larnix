using UnityEditor;
using System.IO;
using System.Diagnostics;
using UnityEditor.Build.Reporting;
using System.Collections.Generic;

public class BuildAutomation
{
    private static string buildRoot = "Builds/";

    private static string[] clientScenes = new string[]
    {
        "Assets/GameFiles/Scenes/Menu.unity",
        "Assets/GameFiles/Scenes/Client.unity",
        "Assets/GameFiles/Scenes/Server.unity"
    };

    private static string[] serverScenes = new string[]
    {
        "Assets/GameFiles/Scenes/Server.unity"
    };

    private static List<string> successfulBuildFolders = new List<string>();
    private static List<string> failedBuilds = new List<string>();

    private static void BuildClient(BuildTarget target, string path)
    {
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = clientScenes,
            locationPathName = path,
            target = target,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        ProcessBuildReport(report);
    }

    private static void BuildServer(BuildTarget target, string path)
    {
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = serverScenes,
            locationPathName = path,
            target = target,
            subtarget = (int)StandaloneBuildSubtarget.Server,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        ProcessBuildReport(report);
    }

    private static void ProcessBuildReport(BuildReport report)
    {
        if (report.summary.result == BuildResult.Succeeded)
        {
            string folder = Path.GetDirectoryName(report.summary.outputPath);
            if (!successfulBuildFolders.Contains(folder))
                successfulBuildFolders.Add(folder);
        }
        else
        {
            failedBuilds.Add(report.summary.outputPath);
        }
    }

    private static void OpenAllSuccessfulFolders()
    {
        foreach (var folder in successfulBuildFolders)
        {
            if (Directory.Exists(folder))
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = folder,
                    UseShellExecute = true
                });
            }
        }
    }

    private static void ClearBuildData()
    {
        successfulBuildFolders.Clear();
        failedBuilds.Clear();
    }

    // --- Menu methods ---

    [MenuItem("Build/Client/Windows")]
    public static void BuildClientWindows()
    {
        string path = buildRoot + "Windows/Client/Larnix.exe";
        ClearBuildData();
        BuildClient(BuildTarget.StandaloneWindows64, path);
        LogBuildSummaryAndOpenFolders();
    }

    [MenuItem("Build/Client/Linux")]
    public static void BuildClientLinux()
    {
        string path = buildRoot + "Linux/Client/Larnix.x86_64";
        ClearBuildData();
        BuildClient(BuildTarget.StandaloneLinux64, path);
        LogBuildSummaryAndOpenFolders();
    }

    [MenuItem("Build/Client/Mac")]
    public static void BuildClientMac()
    {
        string path = buildRoot + "Mac/Client/Larnix.app";
        ClearBuildData();
        BuildClient(BuildTarget.StandaloneOSX, path);
        LogBuildSummaryAndOpenFolders();
    }

    [MenuItem("Build/Server/Windows")]
    public static void BuildServerWindows()
    {
        string path = buildRoot + "Windows/Server/LarnixServer.exe";
        ClearBuildData();
        BuildServer(BuildTarget.StandaloneWindows64, path);
        LogBuildSummaryAndOpenFolders();
    }

    [MenuItem("Build/Server/Linux")]
    public static void BuildServerLinux()
    {
        string path = buildRoot + "Linux/Server/LarnixServer.x86_64";
        ClearBuildData();
        BuildServer(BuildTarget.StandaloneLinux64, path);
        LogBuildSummaryAndOpenFolders();
    }

    [MenuItem("Build/Server/Mac")]
    public static void BuildServerMac()
    {
        string path = buildRoot + "Mac/Server/LarnixServer.app";
        ClearBuildData();
        BuildServer(BuildTarget.StandaloneOSX, path);
        LogBuildSummaryAndOpenFolders();
    }

    [MenuItem("Build/Client+Server/Windows")]
    public static void BuildClientServerWindows()
    {
        ClearBuildData();
        BuildClient(BuildTarget.StandaloneWindows64, buildRoot + "Windows/Client/Larnix.exe");
        BuildServer(BuildTarget.StandaloneWindows64, buildRoot + "Windows/Server/LarnixServer.exe");
        LogBuildSummaryAndOpenFolders();
    }

    [MenuItem("Build/Client+Server/Linux")]
    public static void BuildClientServerLinux()
    {
        ClearBuildData();
        BuildClient(BuildTarget.StandaloneLinux64, buildRoot + "Linux/Client/Larnix.x86_64");
        BuildServer(BuildTarget.StandaloneLinux64, buildRoot + "Linux/Server/LarnixServer.x86_64");
        LogBuildSummaryAndOpenFolders();
    }

    [MenuItem("Build/Client+Server/Mac")]
    public static void BuildClientServerMac()
    {
        ClearBuildData();
        BuildClient(BuildTarget.StandaloneOSX, buildRoot + "Mac/Client/Larnix.app");
        BuildServer(BuildTarget.StandaloneOSX, buildRoot + "Mac/Server/LarnixServer.app");
        LogBuildSummaryAndOpenFolders();
    }

    [MenuItem("Build/Build All")]
    public static void BuildAll()
    {
        ClearBuildData();

        BuildClient(BuildTarget.StandaloneWindows64, buildRoot + "Windows/Client/Larnix.exe");
        BuildServer(BuildTarget.StandaloneWindows64, buildRoot + "Windows/Server/LarnixServer.exe");

        BuildClient(BuildTarget.StandaloneLinux64, buildRoot + "Linux/Client/Larnix.x86_64");
        BuildServer(BuildTarget.StandaloneLinux64, buildRoot + "Linux/Server/LarnixServer.x86_64");

        BuildClient(BuildTarget.StandaloneOSX, buildRoot + "Mac/Client/Larnix.app");
        BuildServer(BuildTarget.StandaloneOSX, buildRoot + "Mac/Server/LarnixServer.app");

        LogBuildSummaryAndOpenFolders();
    }

    private static void LogBuildSummaryAndOpenFolders()
    {
        if (failedBuilds.Count == 0)
        {
            UnityEngine.Debug.Log("All builds succeeded.");
        }
        else
        {
            foreach (var failPath in failedBuilds)
            {
                UnityEngine.Debug.LogError($"Build failed: {failPath}");
            }
            UnityEngine.Debug.Log($"{failedBuilds.Count} builds failed, {successfulBuildFolders.Count} builds succeeded.");
        }

        OpenAllSuccessfulFolders();
    }
}
