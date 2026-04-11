using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;

public class VersionModifier : EditorWindow
{
    private string gameInfoFilePath;
    private string currentVersion;

    private const string VersionRegexPattern = @"(public static Version Version => new\("")(.*?)(""\);)";

    [MenuItem("Automation/Tools/Version Modifier")]
    public static void ShowWindow()
    {
        var window = GetWindow<VersionModifier>("Version Modifier");
        window.minSize = new Vector2(500, 150);
        window.FindAndLoadVersion();
    }

    private void OnEnable()
    {
        FindAndLoadVersion();
    }

    private void FindAndLoadVersion()
    {
        string[] scriptGuids = AssetDatabase.FindAssets("t:MonoScript");

        foreach (string guid in scriptGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string scriptContent = File.ReadAllText(path);

            if (scriptContent.Contains("namespace Larnix.Model") &&
                scriptContent.Contains("public static class GameInfo"))
            {
                Match match = Regex.Match(scriptContent, VersionRegexPattern);
                if (match.Success)
                {
                    gameInfoFilePath = path;
                    currentVersion = match.Groups[2].Value;
                    return;
                }
            }
        }

        gameInfoFilePath = null;
        currentVersion = string.Empty;
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Version Manager (GameInfo)", EditorStyles.boldLabel);
        GUILayout.Space(5);

        if (string.IsNullOrEmpty(gameInfoFilePath))
        {
            EditorGUILayout.HelpBox("No GameInfo file with the correct signature found in the project.", MessageType.Error);
            if (GUILayout.Button("Rescan"))
            {
                FindAndLoadVersion();
            }
            return;
        }

        GUI.enabled = false;
        EditorGUILayout.TextField("File Path:", gameInfoFilePath);
        GUI.enabled = true;

        GUILayout.Space(10);

        currentVersion = EditorGUILayout.TextField("Current Version:", currentVersion);

        bool isValid = IsValidVersionString(currentVersion);

        if (!isValid)
        {
            EditorGUILayout.HelpBox("Invalid format! The version must consist of 1 to 4 numbers (0-255) separated by dots (e.g., 1.2.3.4).", MessageType.Warning);
        }

        GUILayout.Space(15);

        GUI.enabled = isValid;
        if (GUILayout.Button("Save new version", GUILayout.Height(30)))
        {
            SaveVersion();
        }
        GUI.enabled = true;
    }

    private void SaveVersion()
    {
        if (!IsValidVersionString(currentVersion))
        {
            Debug.LogWarning("[GameInfo] Attempted to save an invalid version string.");
            return;
        }

        string fileContent = File.ReadAllText(gameInfoFilePath);
        string updatedContent = Regex.Replace(fileContent, VersionRegexPattern, $"${{1}}{currentVersion}${{3}}");

        if (fileContent != updatedContent)
        {
            File.WriteAllText(gameInfoFilePath, updatedContent);
            AssetDatabase.Refresh();
            Debug.Log($"[GameInfo] Game version updated to: <b>{currentVersion}</b>");
        }
        else
        {
            Debug.Log("[GameInfo] Version is already up to date. No changes made.");
        }
    }

    private bool IsValidVersionString(string versionStr)
    {
        if (string.IsNullOrWhiteSpace(versionStr))
            return false;

        string[] segments = versionStr.Split('.');

        if (segments.Length > 4)
            return false;

        foreach (string segment in segments)
        {
            if (!byte.TryParse(segment, out _))
            {
                return false;
            }
        }

        return true;
    }
}
