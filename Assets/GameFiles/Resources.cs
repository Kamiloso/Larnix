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
        public static GameObject CreateEntity(EntityID entityID)
        {
            GameObject prefab = GetPrefab("EntityPrefabs/" + entityID.ToString());
            if (prefab == null)
            {
                Larnix.Debug.LogWarning("Couldn't find '" + entityID + "' entity prefab!");
                prefab = GetPrefab("EntityPrefabs/None");
            }

            GameObject gobj = GameObject.Instantiate(prefab);
            gobj.transform.name = entityID.ToString();
            return gobj;
        }

        private static readonly Dictionary<string, GameObject> PrefabCache = new();
        private const int MAX_CACHE = 512;

        private static GameObject GetPrefab(string path)
        {
            if(!PrefabCache.TryGetValue(path, out GameObject prefab))
            {
                if(PrefabCache.Count >= MAX_CACHE)
                    PrefabCache.Clear();

                prefab = UnityEngine.Resources.Load<GameObject>(path);
                PrefabCache[path] = prefab;
            }

            if (prefab == null)
                return null;

            foreach(Transform trn in prefab.transform)
            {
                if (trn.name == "Client") // relict, to remove child
                    return trn.gameObject;
            }

            return null;
        }
    }
}
