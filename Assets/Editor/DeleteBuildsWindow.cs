using UnityEditor;
using UnityEngine;
using System.IO;

public class DeleteBuildsWindow : EditorWindow
{
    // GLOBAL
    private bool all = true;

    // WINDOWS
    private bool winAll = true;
    private bool winClient = true;
    private bool winServer = true;

    // LINUX
    private bool linuxAll = true;
    private bool linuxClient = true;
    private bool linuxServer = true;

    // MAC INTEL
    private bool macIntelAll = true;
    private bool macIntelClient = true;
    private bool macIntelServer = true;

    // MAC SILICON
    private bool macSiliconAll = true;
    private bool macSiliconClient = true;
    private bool macSiliconServer = true;

    public static void ShowWindow()
    {
        var window = GetWindow<DeleteBuildsWindow>(true, "Delete Builds", true);
        window.minSize = new Vector2(350, 380);
    }

    private void OnGUI()
    {
        GUILayout.Label("Select builds to delete", EditorStyles.boldLabel);
        GUILayout.Space(5);

        EditorGUI.BeginChangeCheck();
        all = EditorGUILayout.ToggleLeft("ALL", all, EditorStyles.boldLabel);
        if (EditorGUI.EndChangeCheck())
        {
            SetAll(all);
        }

        GUILayout.Space(10);

        DrawPlatform("Windows", ref winAll, ref winClient, ref winServer);
        DrawPlatform("Linux", ref linuxAll, ref linuxClient, ref linuxServer);
        DrawPlatform("Mac Intel", ref macIntelAll, ref macIntelClient, ref macIntelServer);
        DrawPlatform("Mac Silicon", ref macSiliconAll, ref macSiliconClient, ref macSiliconServer);

        GUILayout.FlexibleSpace();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Delete", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog(
                "Confirm deletion",
                "This will permanently delete selected builds.\n\nNo undo. No mercy.",
                "Delete",
                "Cancel"))
            {
                DeleteSelected();
                Close();
            }
        }

        if (GUILayout.Button("Cancel", GUILayout.Height(30)))
        {
            Close();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawPlatform(string name, ref bool platformAll, ref bool client, ref bool server)
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUI.BeginChangeCheck();
        platformAll = EditorGUILayout.ToggleLeft(name, platformAll, EditorStyles.boldLabel);
        if (EditorGUI.EndChangeCheck())
        {
            client = platformAll;
            server = platformAll;
            UpdateAllState();
        }

        EditorGUI.BeginChangeCheck();
        client = EditorGUILayout.ToggleLeft("Client", client);
        server = EditorGUILayout.ToggleLeft("Server", server);
        if (EditorGUI.EndChangeCheck())
        {
            platformAll = client && server;
            UpdateAllState();
        }

        EditorGUILayout.EndVertical();
    }

    private void SetAll(bool value)
    {
        winAll = linuxAll = macIntelAll = macSiliconAll = value;

        winClient = winServer = value;
        linuxClient = linuxServer = value;
        macIntelClient = macIntelServer = value;
        macSiliconClient = macSiliconServer = value;
    }

    private void UpdateAllState()
    {
        winAll = winClient && winServer;
        linuxAll = linuxClient && linuxServer;
        macIntelAll = macIntelClient && macIntelServer;
        macSiliconAll = macSiliconClient && macSiliconServer;

        all = winAll && linuxAll && macIntelAll && macSiliconAll;
    }

    private void DeleteSelected()
    {
        TryDelete(winClient, "Windows/Client");
        TryDelete(winServer, "Windows/Server");

        TryDelete(linuxClient, "Linux/Client");
        TryDelete(linuxServer, "Linux/Server");

        TryDelete(macIntelClient, "Mac_Intel/Client");
        TryDelete(macIntelServer, "Mac_Intel/Server");

        TryDelete(macSiliconClient, "Mac_Silicon/Client");
        TryDelete(macSiliconServer, "Mac_Silicon/Server");

        DeleteEmptyDirectories(BuildAutomation.BuildRoot);

        AssetDatabase.Refresh();
        Debug.Log("Builds deleted. Empty folders purged. The universe is slightly cleaner.");
    }

    private void TryDelete(bool condition, string relativePath)
    {
        if (!condition) return;

        string fullPath = Path.Combine(BuildAutomation.BuildRoot, relativePath);
        if (Directory.Exists(fullPath))
        {
            Directory.Delete(fullPath, true);
        }
    }

    public static void DeleteEmptyDirectories(string rootPath)
    {
        if (!Directory.Exists(rootPath))
            return;

        foreach (var dir in Directory.GetDirectories(rootPath))
        {
            DeleteEmptyDirectories(dir);

            if (Directory.GetFiles(dir).Length == 0 &&
                Directory.GetDirectories(dir).Length == 0)
            {
                Directory.Delete(dir);
            }
        }
    }
}
