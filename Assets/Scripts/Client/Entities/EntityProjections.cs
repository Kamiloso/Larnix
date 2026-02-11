using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Diagnostics;
using Larnix.Packets;
using Larnix.Entities.Structs;

namespace Larnix.Client.Entities
{
    public class EntityProjections : MonoBehaviour
    {
        private record DelayedEntity(EntityData EntityData, double Time);

        private HashSet<ulong> _nearbyUIDs = new();
        private Dictionary<ulong, EntityProjection> _projections = new();
        private Dictionary<ulong, DelayedEntity> _delayedProjections = new();

        private Client Client => Ref.Client;
        private MainPlayer MainPlayer => Ref.MainPlayer;

        private uint? _startFixed = null;
        private uint? _nearbyFrameFixed = null; // can be a bit old, but only up to ~1/4 seconds

        private void Awake()
        {
            Ref.EntityProjections = this;
        }

        public void ChangeNearbyUIDs(NearbyEntities msg)
        {
            ulong[] add = msg.AddEntities;
            ulong[] remove = msg.RemoveEntities;

            foreach(ulong uid in add)
                _nearbyUIDs.Add(uid);

            foreach(ulong uid in remove)
                if(_nearbyUIDs.Contains(uid))
                    _nearbyUIDs.Remove(uid);

            _nearbyFrameFixed = msg.FixedFrame;
        }

        public void InterpretEntityBroadcast(EntityBroadcast msg)
        {
            if (MainPlayer.UID == 0) return; // drop, too early to display

            if (_startFixed == null)
                _startFixed = msg.PacketFixedIndex;

            uint relativeFixedFrame = msg.PacketFixedIndex - (uint)_startFixed;

            Dictionary<ulong, EntityData> dict = msg.EntityTransforms;
            Dictionary<ulong, uint> fixeds = msg.PlayerFixedIndexes;

            // Update data
            foreach(var kvp in dict)
            {
                ulong uid = kvp.Key;
                EntityData entity = kvp.Value;

                if (uid == MainPlayer.UID || !_nearbyUIDs.Contains(uid))
                    continue;

                double time_fixed = (double)(relativeFixedFrame * Time.fixedDeltaTime);

                if (fixeds.ContainsKey(uid)) // extra fixed (smoothing based on other client's info)
                {
                    time_fixed = (double)(fixeds[uid] * Time.fixedDeltaTime);
                }

                if (_projections.ContainsKey(uid)) // update transform
                {
                    EntityProjection projection = _projections[uid];
                    if (time_fixed > projection.LastTime)
                    {
                        projection.UpdateTransform(entity, time_fixed);
                    }
                }
                else // create new projection (delay or overwrite delayed)
                {
                    _delayedProjections.TryGetValue(uid, out DelayedEntity delayed);
                    if (delayed == null || time_fixed > delayed.Time)
                    {
                        _delayedProjections[uid] = new DelayedEntity(entity, time_fixed);
                    }
                }
            }
        }

        public void EarlyUpdate1()
        {
            // Remove no longer active projections

            List<ulong> active_uids = _projections.Keys.ToList();
            List<ulong> delayed_uids = _delayedProjections.Keys.ToList();

            foreach(ulong uid in active_uids)
                if (!_nearbyUIDs.Contains(uid))
                {
                    EntityProjection projection = _projections[uid];
                    Destroy(projection.gameObject);
                    _projections.Remove(uid);
                }

            foreach(ulong uid in delayed_uids)
                if(!_nearbyUIDs.Contains(uid))
                {
                    _delayedProjections.Remove(uid);
                }

            // Spawn delayed projections

            const double MAX_CREATION_MS = 3.0; // miliseconds
            Stopwatch timer = Stopwatch.StartNew();

            foreach (ulong uid in _delayedProjections.Keys.ToList())
            {
                if (timer.Elapsed.TotalMilliseconds >= MAX_CREATION_MS)
                    break;

                DelayedEntity delayedEntity = _delayedProjections[uid];
                _delayedProjections.Remove(uid);

                _projections[uid] = CreateProjection(uid, delayedEntity.EntityData, delayedEntity.Time);
            }

            timer.Stop();
        }

        public bool EverythingLoaded(uint atFrame)
        {
            const uint MIN_DELAY = 1; // should be 1, not 0
            const uint SUSPICIOUS_DELAY = 25;
            const uint CRITICAL_DELAY = 200; // 4 seconds at 50 TPS

            if (_nearbyFrameFixed == null)
                return false; // no messages yet

            int overtime = (int)((uint)_nearbyFrameFixed - atFrame);

            if (overtime < MIN_DELAY)
                return false; // no information yet

            else if (overtime >= MIN_DELAY && overtime < SUSPICIOUS_DELAY)
                return _projections.Count == _nearbyUIDs.Count; // everything loaded

            else if (overtime >= SUSPICIOUS_DELAY && overtime < CRITICAL_DELAY)
                return _projections.Count >= 0.9f * _nearbyUIDs.Count; // vast majority loaded

            else if (overtime >= CRITICAL_DELAY)
                return true; // waiting for too long, preventing deadlock

            return false; // impossible case
        }

        public bool TryGetProjection(ulong uid, bool includePlayer, out EntityProjection projection)
        {
            if (_projections.TryGetValue(uid, out projection))
                return true;

            if (includePlayer && MainPlayer.UID == uid)
            {
                projection = MainPlayer.GetEntityProjection();
                return true;
            }

            projection = default;
            return false;
        }

        private EntityProjection CreateProjection(ulong uid, EntityData entityData, double time)
        {
            GameObject gobj = Prefabs.CreateEntity(entityData.ID);
            gobj.transform.SetParent(transform, false);
            gobj.transform.name = entityData.ID + " [" + uid + "]";
            EntityProjection projection = gobj.GetComponent<EntityProjection>();
            projection.UpdateTransform(entityData, time);
            return projection;
        }
    }
}
