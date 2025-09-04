using UnityEngine;
using UnityEditor;

public class BlockTexturePostprocessor : AssetPostprocessor
{
    private static readonly string targetPath = "Assets/GameFiles/Client/Resources/BlockTextures/";

    static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        foreach (string assetPath in importedAssets)
        {
            if (!assetPath.StartsWith(targetPath))
                continue;

            var textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (textureImporter == null)
                continue;

            bool changed = false;

            if (textureImporter.textureCompression != TextureImporterCompression.Uncompressed)
            {
                textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
                changed = true;
            }

            if (textureImporter.filterMode != FilterMode.Point)
            {
                textureImporter.filterMode = FilterMode.Point;
                changed = true;
            }

            if (textureImporter.mipmapEnabled != false)
            {
                textureImporter.mipmapEnabled = false;
                changed = true;
            }

            if (changed)
            {
                textureImporter.SaveAndReimport();
                Debug.Log($"[BlockTexturePostprocessor] Updated settings: {assetPath}");
            }
        }
    }
}
