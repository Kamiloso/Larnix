using Larnix.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Larnix.Patches;

namespace Larnix
{
    public class StreamingTextureLoader : MonoBehaviour, IGlobalUnitySingleton
    {
        public static StreamingTextureLoader Instance { get; private set; }
        private Dictionary<string, Texture2D> _cache = new Dictionary<string, Texture2D>();

        private static Texture2D _pinkTexture = null;
        public static Texture2D PinkTexture
        {
            get
            {
                if (_pinkTexture != null)
                    return _pinkTexture;

                Texture2D tex = CreateFullColorTexture(new Color(1f, 0f, 1f, 1f));
                _pinkTexture = tex;
                return tex;
            }
        }

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(this);
        }

        private static Texture2D CreateFullColorTexture(Color color)
        {
            Texture2D tex = new Texture2D(2, 2);
            Color[] pixels = new Color[4] { color, color, color, color };
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private string NormalizeKey(string relativePath)
        {
            return relativePath.Replace("\\", "/").ToLowerInvariant();
        }

        public Texture2D LoadTextureSync(string relativePath)
        {
            string key = NormalizeKey(relativePath);

            if (_cache.TryGetValue(key, out Texture2D tex))
                return tex;

            foreach (var pack in ResourcePackManager.Packs)
            {
                string filePath = pack.GetFilePath(relativePath);

                if (File.Exists(filePath))
                {
                    try
                    {
                        byte[] fileData = File.ReadAllBytes(filePath);

                        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
                        texture.filterMode = FilterMode.Point;
                        texture.wrapMode = TextureWrapMode.Clamp;
                        texture.mipMapBias = 0;
                        texture.anisoLevel = 0;

                        if (texture.LoadImage(fileData, false))
                        {
                            texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
                            _cache[key] = texture;
                            return texture;
                        }
                        else
                        {
                            goto return_null;
                        }
                    }
                    catch (Exception)
                    {
                        goto return_null;
                    }
                }
            }

        return_null:
            _cache[key] = null;
            return null;
        }

        // --- Nested Classes ---

        [Serializable]
        private class ResourcePackManifest
        {
            public string name;
            public int priority;
        }

        private class ResourcePack
        {
            public string Name { get; private set; }
            public string Path { get; private set; }
            public int Priority { get; private set; }

            public ResourcePack(string path, string name, int priority)
            {
                Path = path;
                Name = name;
                Priority = priority;
            }

            public string GetFilePath(string relativePath)
            {
                return System.IO.Path.Combine(Path, relativePath);
            }
        }

        private class ResourcePackManager
        {
            private static List<ResourcePack> _packs;

            public static List<ResourcePack> Packs
            {
                get
                {
                    if (_packs == null)
                        LoadPacks();
                    return _packs;
                }
            }

            public static void LoadPacks()
            {
                _packs = new List<ResourcePack>();
                string packsRoot = Path.Combine(Application.streamingAssetsPath, "Resources");

                if (Directory.Exists(packsRoot))
                {
                    foreach (string dir in Directory.GetDirectories(packsRoot))
                    {
                        string manifestPath = Path.Combine(dir, "manifest.json");
                        ResourcePackManifest manifest = null;
                        if (File.Exists(manifestPath))
                        {
                            string json = File.ReadAllText(manifestPath);
                            manifest = JsonUtility.FromJson<ResourcePackManifest>(json);
                        }
                        string name = manifest != null ? manifest.name : new DirectoryInfo(dir).Name;
                        int priority = manifest != null ? manifest.priority : 0;
                        _packs.Add(new ResourcePack(dir, name, priority));
                    }

                    _packs.Sort((a, b) => b.Priority.CompareTo(a.Priority));
                }
            }
        }
    }
}
