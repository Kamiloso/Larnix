using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Entities
{
    public static class EntityPrefabs
    {
        public static GameObject CreateObject(EntityData.EntityID entityID, string mode)
        {
            if (mode != "Client" && mode != "Server")
                throw new System.ArgumentException("Mode string can be only 'Client' or 'Server'.");

            GameObject found = Resources.Load<GameObject>("Prefabs/" + entityID.ToString());
            if (found == null)
                return CreateObject(EntityData.EntityID.None, mode);
            
            foreach(Transform trn in found.transform)
            {
                if(trn.name == mode)
                {
                    GameObject prefab = Object.Instantiate(trn.gameObject);
                    prefab.transform.name = entityID.ToString() + " (" + mode + ")";
                    return prefab;
                }
            }

            return CreateObject(EntityData.EntityID.None, mode);
        }
    }
}
