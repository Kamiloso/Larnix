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
        public enum Mode { Server, Client }
        public static GameObject CreateEntity(EntityID entityID, Mode mode)
        {
            GameObject prefab = GetPrefab("Entities/" + entityID.ToString(), mode);
            if (prefab == null)
            {
                UnityEngine.Debug.LogWarning("Couldn't find '" + entityID + "' entity prefab!");
                prefab = GetPrefab("Entities/None", mode);
            }

            GameObject gobj = GameObject.Instantiate(prefab);
            gobj.transform.name = entityID.ToString() + " <" + mode + ">";
            return gobj;
        }

        public static GameObject CreateChunk(Mode mode)
        {
            GameObject prefab = GetPrefab("Chunk", mode);
            if (prefab == null)
                throw new NotImplementedException("No chunk prefab!");

            GameObject gobj = GameObject.Instantiate(prefab);
            gobj.transform.name = "Chunk <" + mode + ">";
            return gobj;
        }

        public static GameObject CreateBlock(BlockID blockID, Mode mode)
        {
            GameObject prefab = GetPrefab("Blocks/" + blockID.ToString(), mode);
            if (prefab == null)
            {
                UnityEngine.Debug.LogWarning("Couldn't find '" + blockID + "' block prefab!");
                prefab = GetPrefab("Blocks/Air", mode);
            }

            GameObject gobj = GameObject.Instantiate(prefab);
            gobj.transform.name = blockID.ToString() + " <" + mode + ">";
            return gobj;
        }

        private static GameObject GetPrefab(string path, Mode mode)
        {
            GameObject prefab = Resources.Load<GameObject>(path);

            if (prefab == null)
                return null;

            foreach(Transform trn in prefab.transform)
            {
                if (trn.name == mode.ToString())
                    return trn.gameObject;
            }

            return null;
        }
    }
}
