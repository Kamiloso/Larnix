using System;
using System.IO;
using System.Collections.Generic;

public static class ServerDllCopier
{
    private static string dllSegment = "Larnix_Data/Managed/Larnix.Server.dll";
    private static string macDllSegment = "Larnix.app/Contents/Resources/Data/Managed/Larnix.Server.dll";
    private static string targetDir = ".sbuild/LarnixServer/unity/";

    private static HashSet<string> noCopyDlls = new()
    {
        "netstandard",
        "mscorlib",
    };

    private static HashSet<string> alwaysCopy = new()
    {
        "Microsoft.Data.Sqlite",
    };

    public static void CopyServerWithDependencies(BuildMode sourceBuild)
    {
        string serverDllPath = sourceBuild switch
        {
            BuildMode.Windows => BuildAutomation.BuildRoot + "Windows/Client/" + dllSegment,
            BuildMode.Linux => BuildAutomation.BuildRoot + "Linux/Client/" + dllSegment,
            BuildMode.MacSilicon => BuildAutomation.BuildRoot + "Mac_Silicon/Client/" + macDllSegment,
            BuildMode.MacIntel => BuildAutomation.BuildRoot + "Mac_Intel/Client/" + macDllSegment,
            _ => throw new InvalidOperationException($"Copying DLLs from {sourceBuild} build is unsupported!")
        };

        if (!File.Exists(serverDllPath))
        {
            UnityEngine.Debug.LogError($"Root DLL not found: {serverDllPath}");
            return;
        }

        if (Directory.Exists(targetDir))
            Directory.Delete(targetDir, true);

        Directory.CreateDirectory(targetDir);

        var copied = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CopyAssemblyAndDependencies(serverDllPath, copied);

        UnityEngine.Debug.Log("Server DLL and dependencies copied successfully.");
    }

    private static void CopyAssemblyAndDependencies(string dllPath, HashSet<string> copied)
    {
        if (copied.Contains(dllPath))
            return;

        copied.Add(dllPath);

        string fileName = Path.GetFileName(dllPath);
        File.Copy(dllPath, Path.Combine(targetDir, fileName), true);

        byte[] dllBytes = File.ReadAllBytes(dllPath);
        var assembly = System.Reflection.Assembly.ReflectionOnlyLoad(dllBytes);

        foreach (var reference in assembly.GetReferencedAssemblies())
        {
            if (!alwaysCopy.Contains(reference.Name))
            {
                if (reference.Name.StartsWith("System") ||
                    reference.Name.StartsWith("Microsoft") ||
                    noCopyDlls.Contains(reference.Name))
                    continue;
            }

            string refPath = Path.Combine(Path.GetDirectoryName(dllPath), reference.Name + ".dll");
            if (File.Exists(refPath))
            {
                CopyAssemblyAndDependencies(refPath, copied);
            }
            else
            {
                UnityEngine.Debug.LogError($"Dependency not found: {reference.FullName}");
            }
        }
    }
}
