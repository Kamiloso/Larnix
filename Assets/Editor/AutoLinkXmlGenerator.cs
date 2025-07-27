using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class AutoLinkXmlGenerator : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        GenerateLinkXml();
    }

    private static void GenerateLinkXml()
    {
        string outputPath = Path.Combine(Application.dataPath, "link.xml");
        var assembly = Assembly.Load("Assembly-CSharp");

        var types = assembly.GetTypes()
            .Where(t => typeof(Larnix.Server.Terrain.BlockServer).IsAssignableFrom(t) && !t.IsAbstract)
            .ToList();

        using (StreamWriter writer = new StreamWriter(outputPath, false))
        {
            writer.WriteLine("<linker>");
            writer.WriteLine("  <assembly fullname=\"Assembly-CSharp\">");

            foreach (var type in types)
            {
                writer.WriteLine($"    <type fullname=\"{type.FullName}\" preserve=\"all\"/>");
            }

            writer.WriteLine("  </assembly>");
            writer.WriteLine("</linker>");
        }

        Debug.Log($"[link.xml] Generated {types.Count} BlockServer types in link.xml");
        AssetDatabase.Refresh();
    }
}
