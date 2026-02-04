using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Entities;
using Larnix.Blocks;
using UnityEngine.Tilemaps;

namespace Larnix
{
    public static class Resources

    {
        private static readonly Dictionary<string, GameObject> PrefabChildCache = new();

        public static GameObject CreateEntity(EntityID entityID)
        {
            GameObject prefab = GetPrefabChild("EntityPrefabs/" + entityID.ToString());
            if (prefab == null)
            {
                Core.Debug.LogWarning("Couldn't find '" + entityID + "' entity prefab!");
                prefab = GetPrefabChild("EntityPrefabs/None");
            }

            GameObject gobj = GameObject.Instantiate(prefab);
            gobj.transform.name = entityID.ToString();
            return gobj;
        }

        private static GameObject GetPrefabChild(string path)
        {
            if (!PrefabChildCache.TryGetValue(path, out GameObject prefab))
            {
                prefab = UnityEngine.Resources.Load<GameObject>(path);
                PrefabChildCache[path] = prefab;
            }

            if (prefab == null)
                return null;

            foreach (Transform trn in prefab.transform)
            {
                if (trn.name == "Client") // relict, don't touch!
                    return trn.gameObject;
            }

            return null;
        }

        public static Tile CreateTileObject(BlockID ID, byte variant)
        {
            string path = variant == 0 ? ("Blocks/" + ID.ToString()) : ("Blocks/" + ID.ToString() + "-" + variant + ".png");
            string fallbackPath = "Blocks/" + ID.ToString() + ".png";

            Texture2D texture = StreamingTextureLoader.Instance.LoadTextureSync(path);
            if (texture == null) texture = StreamingTextureLoader.Instance.LoadTextureSync(fallbackPath);
            if (texture == null) texture = StreamingTextureLoader.PinkTexture;

            return MakeTileFromTexture(texture);
        }

        public static Tile CreateBorderTileObject(byte borderByte, byte alphaByte)
        {
            const int SIZE = 16;

            Texture2D texture = new Texture2D(SIZE, SIZE, TextureFormat.RGBA32, false, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.mipMapBias = 0;
            texture.anisoLevel = 0;

            Color transparent = new Color(0, 0, 0, 0);
            Color borderColor = new Color(0, 0, 0, alphaByte / 255f);

            // clear texture
            Color[] pixels = new Color[SIZE * SIZE];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = transparent;

            bool HasBit(int bit) => (borderByte & (1 << bit)) != 0;

            // up edge
            if (!HasBit(1))
                for (int x = 0; x < SIZE; x++)
                    pixels[(SIZE - 1) * SIZE + x] = borderColor;

            // down edge
            if (!HasBit(6))
                for (int x = 0; x < SIZE; x++)
                    pixels[x] = borderColor;

            // left edge
            if (!HasBit(3))
                for (int y = 0; y < SIZE; y++)
                    pixels[y * SIZE] = borderColor;

            // right edge
            if (!HasBit(4))
                for (int y = 0; y < SIZE; y++)
                    pixels[y * SIZE + (SIZE - 1)] = borderColor;

            // corners
            if (!HasBit(0)) pixels[(SIZE - 1) * SIZE] = borderColor;                 // top-left
            if (!HasBit(2)) pixels[(SIZE - 1) * SIZE + (SIZE - 1)] = borderColor;    // top-right
            if (!HasBit(5)) pixels[0] = borderColor;                                 // bottom-left
            if (!HasBit(7)) pixels[SIZE - 1] = borderColor;                          // bottom-right

            texture.SetPixels(pixels);
            texture.Apply();

            return MakeTileFromTexture(texture);
        }

        private static Tile MakeTileFromTexture(Texture2D texture)
        {
            Sprite sprite = Sprite.Create(
                texture: texture,
                rect: new Rect(0, 0, texture.width, texture.height),
                pivot: new Vector2(0.5f, 0.5f),
                pixelsPerUnit: System.Math.Max(texture.width, texture.height),
                extrude: 0,
                meshType: SpriteMeshType.FullRect
            );

            Tile tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.color = Color.white;
            tile.colliderType = Tile.ColliderType.None;

            return tile;
        }
    }
}
