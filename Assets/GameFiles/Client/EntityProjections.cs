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

        private bool AlreadyStarted = false;
        private uint StartedFixed = 0;

        private void Awake()
        {
            References.EntityProjections = this;
        }

        public void InterpretEntityBroadcast(EntityBroadcast msg)
        {
            if (References.Client.MyUID == 0) return; // drop, too early to display

            if ((int)(msg.PacketFixedIndex - LastKnown) <= 0) return; // drop, older data
            LastKnown = msg.PacketFixedIndex;

            if(!AlreadyStarted)
            {
                StartedFixed = msg.PacketFixedIndex;
                AlreadyStarted = true;
            }

            Dictionary<ulong, EntityData> dict = msg.EntityTransforms;
            Dictionary<ulong, uint> fixeds = msg.PlayerFixedIndexes;

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
                ulong uid = kvp.Key;
                EntityData entity = kvp.Value;

                double time_update = msg.PacketUpdateTime;
                double time_fixed = (double)((msg.PacketFixedIndex - StartedFixed) * Time.fixedDeltaTime);

                if (fixeds.ContainsKey(uid)) // extra fixed (smoothing based on other client's info)
                    time_fixed = (double)(fixeds[uid] * Time.fixedDeltaTime);

                if (Projections.ContainsKey(uid)) // update transform
                {
                    Projections[uid].UpdateTransform(entity, time_fixed);
                }
                else // create new projection
                {
                    Projections.Add(uid, CreateProjection(uid, entity, time_fixed));
                }
            }
        }

        private EntityProjection CreateProjection(ulong uid, EntityData entityData, double time)
        {
            GameObject gobj = Prefabs.CreateEntity(entityData.ID, Prefabs.Mode.Client);
            gobj.transform.SetParent(transform, false);
            gobj.transform.name = entityData.ID.ToString() + " [" + uid + "]";
            EntityProjection projection = gobj.GetComponent<EntityProjection>();
            projection.UpdateTransform(entityData, time);
            return projection;
        }
    }
}
