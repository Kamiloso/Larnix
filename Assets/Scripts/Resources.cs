using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Entities;
using Larnix.Blocks;
using UnityEngine.Tilemaps;
using System;

namespace Larnix
{
    public static class Resources
    {
        private static readonly Dictionary<string, GameObject> PrefabChildCache = new();
        private static readonly Dictionary<string, Tile> TileCache = new();

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

        public static Tile GetTile(BlockID ID, byte variant)
        {
            string path = variant == 0 ? ("Blocks/" + ID.ToString()) : ("Blocks/" + ID.ToString() + "-" + variant + ".png");
            string fallbackPath = "Blocks/" + ID.ToString() + ".png";

            if (!TileCache.TryGetValue(path, out Tile tile))
            {
                Texture2D texture = StreamingTextureLoader.Instance.LoadTextureSync(path);
                if (texture == null) texture = StreamingTextureLoader.Instance.LoadTextureSync(fallbackPath);
                if (texture == null) texture = StreamingTextureLoader.PinkTexture;

                Sprite sprite = Sprite.Create(
                    texture: texture,
                    rect: new Rect(0, 0, texture.width, texture.height),
                    pivot: new Vector2(0.5f, 0.5f),
                    pixelsPerUnit: System.Math.Max(texture.width, texture.height),
                    extrude: 0,
                    meshType: SpriteMeshType.FullRect
                );

                tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.color = Color.white;
                tile.colliderType = Tile.ColliderType.None;

                TileCache.Add(path, tile);
            }
            return tile;
        }
    }
}
