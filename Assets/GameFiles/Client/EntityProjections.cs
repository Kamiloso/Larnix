using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Socket.Commands;
using Larnix.Entities;
using System.Linq;
using System.Diagnostics;

namespace Larnix.Client
{
    public class EntityProjections : MonoBehaviour
    {
        public bool ReceivedSomething { get; private set; } = false;
        public uint LastKnown { get; private set; } = 0;

        private Dictionary<ulong, EntityProjection> Projections = new();
        private Dictionary<ulong, DelayedEntity> DelayedProjections = new();

        private const double CREATION_TIME_PER_FRAME = 5.0; // miliseconds

        private class DelayedEntity
        {
            public EntityData entityData;
            public double time;

            public DelayedEntity(EntityData entityData, double time)
            {
                this.entityData = entityData;
                this.time = time;
            }
        }

        private bool AlreadyStarted = false;
        private uint StartedFixed = 0;

        private void Awake()
        {
            References.EntityProjections = this;
        }

        public void InterpretEntityBroadcast(EntityBroadcast msg)
        {
            if (References.Client.MyUID == 0) return; // drop, too early to display

            if (ReceivedSomething && (int)(msg.PacketFixedIndex - LastKnown) <= 0) return; // drop, older data
            ReceivedSomething = true;
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
            HashSet<ulong> uids = Projections.Keys.ToHashSet();
            uids.UnionWith(DelayedProjections.Keys.ToHashSet());
            foreach (var key in uids)
            {
                if(!dict.ContainsKey(key))
                {
                    if (Projections.ContainsKey(key))
                    {
                        Destroy(Projections[key].gameObject);
                        Projections.Remove(key);
                    }

                    if (DelayedProjections.ContainsKey(key))
                    {
                        DelayedProjections.Remove(key);
                    }
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
                else // create new projection (delay or overwrite delayed)
                {
                    DelayedProjections[uid] = new DelayedEntity(entity, time_fixed);
                }
            }
        }

        public void SpawnProjectionsAfterBroadcast()
        {
            Stopwatch timer = Stopwatch.StartNew();

            foreach (ulong uid in DelayedProjections.Keys.ToList())
            {
                if (timer.Elapsed.TotalMilliseconds >= CREATION_TIME_PER_FRAME)
                    break;

                DelayedEntity delayedEntity = DelayedProjections[uid];
                DelayedProjections.Remove(uid);

                Projections[uid] = CreateProjection(uid, delayedEntity.entityData, delayedEntity.time);
            }

            timer.Stop();
        }

        public int GetDelayedEntities()
        {
            return DelayedProjections.Count;
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
