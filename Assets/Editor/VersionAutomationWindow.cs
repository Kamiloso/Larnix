/*using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;

public class VersionAutomationWindow : EditorWindow
{
    private string newVersion = "1.0.0";
    private string status = "";

    private static readonly Regex VersionRegex =
        new Regex(@"^\d{1,3}(\.\d{1,3}){0,3}$");

    private static readonly Regex CurrentLineRegex =
        new Regex(@"public static readonly Version Current = new\(""([^""]+)""\);");

    [MenuItem("Tools/Automation/Version Updater")]
    public static void ShowWindow()
    {
        GetWindow<VersionAutomationWindow>("Version Automation");
    }

    private void OnGUI()
    {
        GUILayout.Label("Version Updater", EditorStyles.boldLabel);

        newVersion = EditorGUILayout.TextField("New Version", newVersion);

        if (GUILayout.Button("Apply Version"))
        {
            ApplyVersion();
        }

        if (!string.IsNullOrEmpty(status))
        {
            EditorGUILayout.HelpBox(status, MessageType.Info);
        }
    }

    private void ApplyVersion()
    {
        if (!IsValidVersion(newVersion))
        {
            status = "Invalid version format. Use: 1, 1.2, 1.2.3, 1.2.3.4 (0–255 each)";
            return;
        }

        string[] guids = AssetDatabase.FindAssets("Version t:Script");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            string content = File.ReadAllText(path);

            if (!content.Contains("public readonly struct Version"))
                continue;

            string newContent = CurrentLineRegex.Replace(
                content,
                $"public static readonly Version Current = new(\"{newVersion}\");"
            );

            if (content != newContent)
            {
                File.WriteAllText(path, newContent);
                AssetDatabase.Refresh();

                status = $"Updated version in: {path}";
                return;
            }
        }

        status = "Version struct not found. Either you renamed it or the universe is against you.";
    }

    private bool IsValidVersion(string v)
    {
        if (!VersionRegex.IsMatch(v))
            return false;

        var parts = v.Split('.');
        foreach (var p in parts)
        {
            if (!byte.TryParse(p, out _))
                return false;
        }

        return true;
    }
}*/