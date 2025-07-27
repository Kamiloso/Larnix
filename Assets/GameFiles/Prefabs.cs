using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Entities;
using Larnix.Blocks;
using System;

namespace Larnix
{
    public static class Prefabs
    {
        public enum Mode { Default, Server, Client }
        public static GameObject CreateEntity(EntityID entityID, Mode mode)
        {
            GameObject prefab = GetPrefab("EntityPrefabs/" + entityID.ToString(), mode);
            if (prefab == null)
            {
                UnityEngine.Debug.LogWarning("Couldn't find '" + entityID + "' entity prefab!");
                prefab = GetPrefab("EntityPrefabs/None", mode);
            }

            GameObject gobj = GameObject.Instantiate(prefab);
            gobj.transform.name = entityID.ToString() + " <" + mode + ">";
            return gobj;
        }

        private static readonly Dictionary<string, GameObject> PrefabCache = new();
        private const int MAX_CACHE = 512;

        private static GameObject GetPrefab(string path, Mode mode = Mode.Default)
        {
            if(!PrefabCache.TryGetValue(path, out GameObject prefab))
            {
                if(PrefabCache.Count >= MAX_CACHE)
                    PrefabCache.Clear();

                prefab = Resources.Load<GameObject>(path);
                PrefabCache[path] = prefab;
            }

            if (prefab == null)
                return null;

            if(mode == Mode.Default)
                return prefab;

            foreach(Transform trn in prefab.transform)
            {
                if (trn.name == mode.ToString())
                    return trn.gameObject;
            }

            return null;
        }
    }
}
