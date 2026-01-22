using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Diagnostics;
using Larnix.Socket.Packets.Game;
using Larnix.Entities.Structs;

namespace Larnix.Client.Entities
{
    public class EntityProjections : MonoBehaviour
    {
        private Dictionary<ulong, EntityProjection> Projections = new();
        private Dictionary<ulong, DelayedEntity> DelayedProjections = new();

        private HashSet<ulong> NearbyUIDs = new HashSet<ulong>();

        private uint? StartedFixed = null;
        private uint? NearbyFrameFixed = null; // Can be a bit old, but only up to ~1/4 seconds

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

        private void Awake()
        {
            Ref.EntityProjections = this;
        }

        public void ChangeNearbyUIDs(NearbyEntities msg)
        {
            ulong[] add = msg.AddEntities;
            ulong[] remove = msg.RemoveEntities;

            foreach(ulong uid in add)
                NearbyUIDs.Add(uid);

            foreach(ulong uid in remove)
                if(NearbyUIDs.Contains(uid))
                    NearbyUIDs.Remove(uid);

            NearbyFrameFixed = msg.FixedFrame;
        }

        public void InterpretEntityBroadcast(EntityBroadcast msg)
        {
            if (Ref.Client.MyUID == 0) return; // drop, too early to display

            if (StartedFixed == null)
                StartedFixed = msg.PacketFixedIndex;

            uint RelativeFixedFrame = msg.PacketFixedIndex - (uint)StartedFixed;

            Dictionary<ulong, EntityData> dict = msg.EntityTransforms;
            Dictionary<ulong, uint> fixeds = msg.PlayerFixedIndexes;

            // Update data
            foreach(var kvp in dict)
            {
                ulong uid = kvp.Key;
                EntityData entity = kvp.Value;

                if (uid == Ref.Client.MyUID || !NearbyUIDs.Contains(uid))
                    continue;

                double time_fixed = (double)(RelativeFixedFrame * Time.fixedDeltaTime);

                if (fixeds.ContainsKey(uid)) // extra fixed (smoothing based on other client's info)
                    time_fixed = (double)(fixeds[uid] * Time.fixedDeltaTime);

                if (Projections.ContainsKey(uid)) // update transform
                {
                    EntityProjection projection = Projections[uid];
                    if (time_fixed > projection.LastTime)
                    {
                        projection.UpdateTransform(entity, time_fixed);
                    }
                }
                else // create new projection (delay or overwrite delayed)
                {
                    DelayedProjections.TryGetValue(uid, out DelayedEntity delayed);
                    if (delayed == null || time_fixed > delayed.time)
                    {
                        DelayedProjections[uid] = new DelayedEntity(entity, time_fixed);
                    }
                }
            }
        }

        public void EarlyUpdate1()
        {
            // Remove no longer active projections

            List<ulong> active_uids = Projections.Keys.ToList();
            List<ulong> delayed_uids = DelayedProjections.Keys.ToList();

            foreach(ulong uid in active_uids)
                if (!NearbyUIDs.Contains(uid))
                {
                    EntityProjection projection = Projections[uid];
                    Destroy(projection.gameObject);
                    Projections.Remove(uid);
                }

            foreach(ulong uid in delayed_uids)
                if(!NearbyUIDs.Contains(uid))
                {
                    DelayedProjections.Remove(uid);
                }

            // Spawn delayed projections

            const double MAX_CREATION_MS = 3.0; // miliseconds
            Stopwatch timer = Stopwatch.StartNew();

            foreach (ulong uid in DelayedProjections.Keys.ToList())
            {
                if (timer.Elapsed.TotalMilliseconds >= MAX_CREATION_MS)
                    break;

                DelayedEntity delayedEntity = DelayedProjections[uid];
                DelayedProjections.Remove(uid);

                Projections[uid] = CreateProjection(uid, delayedEntity.entityData, delayedEntity.time);
            }

            timer.Stop();
        }

        public bool EverythingLoaded(uint atFrame)
        {
            const uint MIN_DELAY = 1; // should be 1, not 0
            const uint SUSPICIOUS_DELAY = 25;
            const uint CRITICAL_DELAY = 200; // 4 seconds at 50 TPS

            if (NearbyFrameFixed == null)
                return false; // no messages yet

            int overtime = (int)((uint)NearbyFrameFixed - atFrame);

            if (overtime < MIN_DELAY)
                return false; // no information yet

            else if (overtime >= MIN_DELAY && overtime < SUSPICIOUS_DELAY)
                return Projections.Count == NearbyUIDs.Count; // everything loaded

            else if (overtime >= SUSPICIOUS_DELAY && overtime < CRITICAL_DELAY)
                return Projections.Count >= 0.9f * NearbyUIDs.Count; // vast majority loaded

            else if (overtime >= CRITICAL_DELAY)
                return true; // waiting for too long, preventing deadlock

            else throw new System.Exception("This exception will never be thrown.");
        }

        private EntityProjection CreateProjection(ulong uid, EntityData entityData, double time)
        {
            GameObject gobj = Resources.CreateEntity(entityData.ID);
            gobj.transform.SetParent(transform, false);
            gobj.transform.name = entityData.ID.ToString() + " [" + uid + "]";
            EntityProjection projection = gobj.GetComponent<EntityProjection>();
            projection.UpdateTransform(entityData, time);
            return projection;
        }
    }
}
