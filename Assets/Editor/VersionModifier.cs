using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;

public class VersionModifier : EditorWindow
{
    private string currentVersion = "Searching...";
    private string newVersion = "";
    private string status = "";
    private string targetFilePath = "";

    private static readonly Regex VersionRegex = new(@"^\d{1,3}(\.\d{1,3}){0,3}$");
    private static readonly Regex CurrentLineRegex = new(@"public static readonly Version Current = new\(""([^""]+)""\);");

    [MenuItem("Automation/Tools/Version Updater")]
    public static void ShowWindow()
    {
        var window = GetWindow<VersionModifier>("Version Updater");
        window.minSize = new Vector2(350, 170);
        window.maxSize = new Vector2(350, 170);
        window.Show();
    }

    private void OnEnable()
    {
        RefreshCurrentVersion();
    }

    private void RefreshCurrentVersion()
    {
        string[] guids = AssetDatabase.FindAssets("Version t:Script");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string content = File.ReadAllText(path);

            if (!content.Contains("public readonly record struct Version"))
                continue;

            targetFilePath = path;
            Match match = CurrentLineRegex.Match(content);
            if (match.Success)
            {
                currentVersion = match.Groups[1].Value;

                if (string.IsNullOrEmpty(newVersion))
                {
                    newVersion = currentVersion;
                }
                status = "Ready for update.";
                return;
            }
        }

        status = "Error: Version struct file not found.";
        currentVersion = "No data";
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("Project Version Management", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox($"Current version in code: {currentVersion}", MessageType.None);
        GUILayout.Space(5);

        newVersion = EditorGUILayout.TextField("New version:", newVersion);
        GUILayout.Space(10);

        if (GUILayout.Button("Apply New Version", GUILayout.Height(30)))
        {
            ApplyVersion();
        }

        GUILayout.Space(10);

        if (!string.IsNullOrEmpty(status))
        {
            MessageType msgType = status.StartsWith("Error") || status.StartsWith("Invalid")
                ? MessageType.Error
                : MessageType.Info;

            EditorGUILayout.HelpBox(status, msgType);
        }
    }

    private void ApplyVersion()
    {
        if (string.IsNullOrEmpty(targetFilePath) || !File.Exists(targetFilePath))
        {
            status = "Error: Target file not found. Close and reopen the window.";
            return;
        }

        if (!IsValidVersion(newVersion))
        {
            status = "Error: Invalid format. Use e.g. 1.0, 1.2.3 (numbers 0-255).";
            return;
        }

        string content = File.ReadAllText(targetFilePath);
        string newContent = CurrentLineRegex.Replace(
            content,
            $"public static readonly Version Current = new(\"{newVersion}\");"
        );

        if (content != newContent)
        {
            File.WriteAllText(targetFilePath, newContent);

            AssetDatabase.ImportAsset(targetFilePath);

            status = $"Successfully updated to: {newVersion}";
            currentVersion = newVersion;

            GUI.FocusControl(null);
        }
        else
        {
            status = "The provided version is the same as the current one (no changes).";
        }
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
}