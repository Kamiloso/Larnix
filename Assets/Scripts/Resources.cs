using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Entities;
using Larnix.Blocks;
using UnityEngine.Tilemaps;
using System.IO;

namespace Larnix
{
    public static class Prefabs
    {
        private static readonly Dictionary<string, GameObject> _prefabCache = new();
        private static readonly Dictionary<string, GameObject> _prefabChildCache = new();

        public static GameObject GetPrefab(string root, string path)
        {
            string fullPath = root + "/" + path;

            if (!_prefabCache.TryGetValue(fullPath, out GameObject prefab))
            {
                prefab = UnityEngine.Resources.Load<GameObject>(fullPath);
                _prefabCache[fullPath] = prefab;
            }

            return prefab;
        }

        public static GameObject CreateEntity(EntityID entityID)
        {
            GameObject prefab = GetPrefabChild("Entities/" + entityID.ToString());
            if (prefab == null)
            {
                Core.Debug.LogWarning("Couldn't find '" + entityID + "' entity prefab!");
                prefab = GetPrefabChild("Entities/None");
            }

            GameObject gobj = GameObject.Instantiate(prefab);
            gobj.transform.name = entityID.ToString();
            return gobj;
        }

        private static GameObject GetPrefabChild(string path)
        {
            if (!_prefabChildCache.TryGetValue(path, out GameObject prefab))
            {
                prefab = UnityEngine.Resources.Load<GameObject>(path);
                _prefabChildCache[path] = prefab;
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
    }
}
