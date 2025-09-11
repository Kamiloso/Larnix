using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Entities;
using Larnix.Blocks;
using System;

namespace Larnix
{
    public static class Resources
    {
        private static readonly Dictionary<string, GameObject> PrefabChildCache = new();
        private static readonly Dictionary<string, Texture2D> TextureCache = new();

        public static GameObject CreateEntity(EntityID entityID)
        {
            GameObject prefab = GetPrefabChild("EntityPrefabs/" + entityID.ToString());
            if (prefab == null)
            {
                Larnix.Debug.LogWarning("Couldn't find '" + entityID + "' entity prefab!");
                prefab = GetPrefabChild("EntityPrefabs/None");
            }

            GameObject gobj = GameObject.Instantiate(prefab);
            gobj.transform.name = entityID.ToString();
            return gobj;
        }

        public static Texture2D GetTileTexture(BlockID ID, byte variant)
        {
            string name = variant == 0 ? ID.ToString() : ID.ToString() + "-" + variant;
            if (!TextureCache.TryGetValue(name, out Texture2D tex))
            {
                tex = UnityEngine.Resources.Load<Texture2D>("Textures/Blocks/" + name);

                if (tex == null && variant != 0) // variant fallback
                    tex = GetTileTexture(ID, 0);

                if (tex == null) // ID fallback
                    tex = UnityEngine.Resources.Load<Texture2D>("Textures/Blocks/Unknown");

                if (tex == null)
                    throw new KeyNotFoundException("Cannot fallback to the Unknown texture! Is it missing?");

                TextureCache.Add(name, tex);
            }
            return tex;
        }

        public static Texture2D GenerateBorderTexture(int size, byte mask)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color clear = Color.clear;
            Color black = Color.black;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool drawPixel = false;

                    // Borders
                    if ((mask & 0b0000_0001) != 0 && x == 0) drawPixel = true;       // left
                    if ((mask & 0b0000_0010) != 0 && x == size - 1) drawPixel = true; // right
                    if ((mask & 0b0000_0100) != 0 && y == size - 1) drawPixel = true; // top
                    if ((mask & 0b0000_1000) != 0 && y == 0) drawPixel = true;       // down

                    // Corners
                    if ((mask & 0b0001_0000) != 0 && x == 0 && y == size - 1) drawPixel = true; // left top
                    if ((mask & 0b0010_0000) != 0 && x == size - 1 && y == size - 1) drawPixel = true; // right top
                    if ((mask & 0b0100_0000) != 0 && x == 0 && y == 0) drawPixel = true; // left down
                    if ((mask & 0b1000_0000) != 0 && x == size - 1 && y == 0) drawPixel = true; // right down

                    texture.SetPixel(x, y, drawPixel ? black : clear);
                }
            }

            texture.Apply();
            return texture;
        }

        private static GameObject GetPrefabChild(string path)
        {
            if(!PrefabChildCache.TryGetValue(path, out GameObject prefab))
            {
                prefab = UnityEngine.Resources.Load<GameObject>(path);
                PrefabChildCache[path] = prefab;
            }

            if (prefab == null)
                return null;

            foreach(Transform trn in prefab.transform)
            {
                if (trn.name == "Client") // relict, don't touch!
                    return trn.gameObject;
            }

            return null;
        }
    }
}
