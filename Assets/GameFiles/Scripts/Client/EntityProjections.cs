using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Socket.Commands;
using Larnix.Entities;
using System.Linq;

namespace Larnix.Client
{
    public class EntityProjections : MonoBehaviour
    {
        private uint LastKnown = 0;
        private Dictionary<ulong, EntityProjection> Projections = new Dictionary<ulong, EntityProjection>();

        private void Awake()
        {
            References.EntityProjections = this;
        }

        public void InterpretEntityBroadcast(EntityBroadcast msg)
        {
            if (msg.PacketIndex < LastKnown) return;
            LastKnown = msg.PacketIndex;

            Dictionary<ulong, EntityData> dict = msg.EntityTransforms;

            if (dict.ContainsKey(References.Client.MyUID))
                dict.Remove(References.Client.MyUID);

            // Remove unloaded projections
            foreach(var key in Projections.Keys.ToList())
            {
                if(!dict.ContainsKey(key))
                {
                    Destroy(Projections[key].gameObject);
                    Projections.Remove(key);
                }
            }

            // Handle loaded projections
            foreach(var kvp in dict)
            {
                if(Projections.ContainsKey(kvp.Key)) // update transform
                {
                    Projections[kvp.Key].UpdateTransform(kvp.Value);
                }
                else // create new projection
                {
                    Projections.Add(kvp.Key, CreateProjection(kvp.Value));
                }
            }
        }

        [SerializeField] GameObject PlayerPrefab;
        [SerializeField] GameObject UnknownPrefab;

        private EntityProjection CreateProjection(EntityData entityData)
        {
            GameObject prefab = null;

            switch(entityData.ID)
            {
                case EntityData.EntityID.Player: prefab = PlayerPrefab; break;
                default: prefab = UnknownPrefab; break;
            }

            EntityProjection projection = Instantiate(prefab).GetComponent<EntityProjection>();
            projection.UpdateTransform(entityData);
            return projection;
        }
    }
}
