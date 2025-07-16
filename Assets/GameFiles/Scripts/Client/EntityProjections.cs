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
            if (References.Client.MyUID == 0) return; // drop, too early to display

            if ((int)(msg.PacketFixedIndex - LastKnown) <= 0) return; // drop, older data
            LastKnown = msg.PacketFixedIndex;

            Dictionary<ulong, EntityData> dict = msg.EntityTransforms;

            if (dict.ContainsKey(References.Client.MyUID))
                dict.Remove(References.Client.MyUID);

            // Remove unloaded projections
            foreach (var key in Projections.Keys.ToList())
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
                    Projections[kvp.Key].UpdateTransform(kvp.Value, msg.PacketUpdateTime);
                }
                else // create new projection
                {
                    Projections.Add(kvp.Key, CreateProjection(kvp.Key, kvp.Value, msg.PacketUpdateTime));
                }
            }
        }

        private EntityProjection CreateProjection(ulong uid, EntityData entityData, double time)
        {
            GameObject gobj = EntityPrefabs.CreateObject(entityData.ID, "Client");
            gobj.transform.SetParent(transform, false);
            gobj.transform.name = entityData.ID.ToString() + " [" + uid + "]";
            EntityProjection projection = gobj.GetComponent<EntityProjection>();
            projection.UpdateTransform(entityData, time);
            return projection;
        }
    }
}
