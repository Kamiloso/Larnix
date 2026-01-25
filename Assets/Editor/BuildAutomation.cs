using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using UnityEngine;

public enum BuildMode { Windows, Linux, MacIntel, MacSilicon }

public class BuildAutomation
{
    public static string BuildRoot => "Builds/";
    public static string ServerProjectPath => ".sbuild/LarnixServer/";

    private static string _batchBuild = ServerProjectPath + "build.bat";
    private static string _batchMove = ServerProjectPath + "move.bat";

    private static string[] clientScenes =
    {
        "Assets/Scenes/Menu.unity",
        "Assets/Scenes/Client.unity"
    };

    private static Queue<Action> delayedActions = new();

    // ---------------- BUILD CORE ----------------

    private static bool BuildClient(BuildTarget target, string path, bool silent = false)
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, target))
        {
            if (!silent) delayedActions.Enqueue(() =>
            {
                UnityEngine.Debug.LogError("Client build unsupported: " + path);
            });
            return false;
        }

        var options = new BuildPlayerOptions
        {
            scenes = clientScenes,
            locationPathName = path,
            target = target,
            subtarget = (int)StandaloneBuildSubtarget.Player,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result == BuildResult.Succeeded)
        {
            if (!silent) delayedActions.Enqueue(() =>
            {
                UnityEngine.Debug.Log("Client build success: " + path);
            });
            return true;
        }
        else
        {
            if (!silent) delayedActions.Enqueue(() =>
            {
                UnityEngine.Debug.LogError("Client build failed: " + path);
            });
            return false;
        }
    }

    private static string GetArgument(BuildMode mode)
    {
        return mode switch
        {
            BuildMode.Windows => "windows",
            BuildMode.Linux => "linux",
            BuildMode.MacSilicon => "mac-silicon",
            BuildMode.MacIntel => "mac-intel",
            _ => throw new InvalidOperationException("Unknown platform: " + mode)
        };
    }

    private static bool BuildServer(BuildMode mode)
    {
        if (RunBatchScript(_batchBuild, GetArgument(mode)) &&
            RunBatchScript(_batchMove, GetArgument(mode)))
        {
            UnityEngine.Debug.Log("Server build for platform '" + mode + "' complete.");
            return true;
        }

        return false;
    }

    private static bool RunBatchScript(string scriptPath, string arguments)
    {
        string fullPath = Path.GetFullPath(scriptPath);
        string workingDirectory = Path.GetDirectoryName(fullPath);
        string fileName = Path.GetFileName(fullPath);

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = fullPath,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        try
        {
            using (Process process = Process.Start(psi))
            {
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    return true;
                }
                else
                {
                    UnityEngine.Debug.LogError($"Script {scriptPath} finished with an error. Exit code: {process.ExitCode}");
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Failed to run process: {ex.Message}");
            return false;
        }
    }

    // ---------------- BUILD EXECUTION ----------------
    private static bool ExecuteClientBuild(BuildMode mode)
    {
        switch (mode)
        {
            case BuildMode.Windows: return BuildClient(BuildTarget.StandaloneWindows64, BuildRoot + "Windows/Client/Larnix.exe");
            case BuildMode.Linux: return BuildClient(BuildTarget.StandaloneLinux64, BuildRoot + "Linux/Client/Larnix.x86_64");
            case BuildMode.MacIntel: return BuildClient(BuildTarget.StandaloneOSX, BuildRoot + "Mac_Intel/Client/Larnix.app");
            case BuildMode.MacSilicon: return BuildClient(BuildTarget.StandaloneOSX, BuildRoot + "Mac_Silicon/Client/Larnix.app");
        }

        throw new ArgumentException("Unsupported client build: " + mode);
    }

    // ---------------- MENU ----------------

    [MenuItem("Automation/Build/Client/Windows")]
    public static void BuildClientWindows()
    {
        ExecuteClientBuild(BuildMode.Windows);
        ShowClientSummary();
        RevealBuilds();
    }

    [MenuItem("Automation/Build/Client/Linux")]
    public static void BuildClientLinux()
    {
        ExecuteClientBuild(BuildMode.Linux);
        ShowClientSummary();
        RevealBuilds();
    }

    [MenuItem("Automation/Build/Client/Mac/Intel")]
    public static void BuildClientMacIntel()
    {
        ExecuteClientBuild(BuildMode.MacIntel);
        ShowClientSummary();
        RevealBuilds();
    }

    [MenuItem("Automation/Build/Client/Mac/Silicon")]
    public static void BuildClientMacSilicon()
    {
        ExecuteClientBuild(BuildMode.MacSilicon);
        ShowClientSummary();
        RevealBuilds();
    }

    [MenuItem("Automation/Build/Client + Server/Windows")]
    public static void BuildClientServerWindows()
    {
        if (Application.platform != RuntimePlatform.WindowsEditor)
        {
            ShowPlatformWarning();
            return;
        }

        bool win = ExecuteClientBuild(BuildMode.Windows);
        ShowClientSummary();

        if (win)
        {
            ServerDllCopier.CopyServerWithDependencies(BuildMode.Windows);
            BuildServer(BuildMode.Windows);
        }
        else
        {
            UnityEngine.Debug.LogWarning("Cannot build server when there is no client present.");
        }

        RevealBuilds();
    }

    [MenuItem("Automation/Build/Client + Server/Linux")]
    public static void BuildClientServerLinux()
    {
        if (Application.platform != RuntimePlatform.WindowsEditor)
        {
            ShowPlatformWarning();
            return;
        }

        bool lin = ExecuteClientBuild(BuildMode.Linux);
        ShowClientSummary();

        if (lin)
        {
            ServerDllCopier.CopyServerWithDependencies(BuildMode.Linux);
            BuildServer(BuildMode.Linux);
        }
        else
        {
            UnityEngine.Debug.LogWarning("Cannot build server when there is no client present.");
        }

        RevealBuilds();
    }

    [MenuItem("Automation/Build/Client + Server/Mac/Intel")]
    public static void BuildClientServerMacIntel()
    {
        if (Application.platform != RuntimePlatform.WindowsEditor)
        {
            ShowPlatformWarning();
            return;
        }

        bool mac = ExecuteClientBuild(BuildMode.MacIntel);
        ShowClientSummary();

        if (mac)
        {
            ServerDllCopier.CopyServerWithDependencies(BuildMode.MacIntel);
            BuildServer(BuildMode.MacIntel);
        }
        else
        {
            UnityEngine.Debug.LogWarning("Cannot build server when there is no client present.");
        }

        RevealBuilds();
    }

    [MenuItem("Automation/Build/Client + Server/Mac/Silicon")]
    public static void BuildClientServerMacSilicon()
    {
        if (Application.platform != RuntimePlatform.WindowsEditor)
        {
            ShowPlatformWarning();
            return;
        }

        bool mac = ExecuteClientBuild(BuildMode.MacSilicon);
        ShowClientSummary();

        if (mac)
        {
            ServerDllCopier.CopyServerWithDependencies(BuildMode.MacSilicon);
            BuildServer(BuildMode.MacSilicon);
        }
        else
        {
            UnityEngine.Debug.LogWarning("Cannot build server when there is no client present.");
        }

        RevealBuilds();
    }

    [MenuItem("Automation/Build/All")]
    public static void BuildAll()
    {
        if (Application.platform != RuntimePlatform.WindowsEditor)
        {
            ShowPlatformWarning();
            return;
        }

        bool win = ExecuteClientBuild(BuildMode.Windows);
        bool lin = ExecuteClientBuild(BuildMode.Linux);
        bool maci = ExecuteClientBuild(BuildMode.MacIntel);
        bool macs = ExecuteClientBuild(BuildMode.MacSilicon);
        ShowClientSummary();

        if (win || lin || maci || macs)
        {
            ServerDllCopier.CopyServerWithDependencies(win ? BuildMode.Windows : (lin ? BuildMode.Linux : (macs ? BuildMode.MacSilicon : BuildMode.MacIntel)));
            BuildServer(BuildMode.Windows);
            BuildServer(BuildMode.Linux);
            BuildServer(BuildMode.MacIntel);
            BuildServer(BuildMode.MacSilicon);
        }
        else
        {
            UnityEngine.Debug.LogWarning("Cannot build server when there is no client present.");
        }

        RevealBuilds();
    }

    [MenuItem("Automation/Tools/Open Build Folder")]
    public static void OpenBuildFolder()
    {
        UnityEditor.EditorUtility.RevealInFinder(BuildRoot + "any-file");
    }

    public static void RemoveBurstDebugDirectories(string rootPath)
    {
        if (!Directory.Exists(rootPath))
        {
            UnityEngine.Debug.LogError($"Directory does not exist: {rootPath}");
            return;
        }

        foreach (var dir in Directory.EnumerateDirectories(
                     rootPath,
                     "Larnix_BurstDebugInformation_DoNotShip",
                     SearchOption.AllDirectories))
        {
            try
            {
                Directory.Delete(dir, true);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to remove {dir}\n{ex}");
            }
        }
    }

    public static void ShowPlatformWarning()
    {
        EditorUtility.DisplayDialog(
            "Wrong Platform",
            "This project requires Windows to perform server builds (dependency on batch scripts).\n\nPlease switch to a Windows machine.",
            "OK"
        );
    }

    private static void ShowClientSummary()
    {
        while (delayedActions.Count > 0)
        {
            Action action = delayedActions.Dequeue();
            action();
        }
    }

    private static void RevealBuilds()
    {
        RemoveBurstDebugDirectories(BuildRoot);
        OpenBuildFolder();
    }
}
