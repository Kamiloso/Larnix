using System.Collections.Generic;
using Larnix.Core;
using Larnix.Core.Vectors;
using Larnix.Client.Entities;
using Larnix.Client.Relativity;
using UnityEngine;
using System.Linq;

namespace Larnix.Client
{
    public class ParticleManager : MonoBehaviour
    {
        [SerializeField] int MaxParticles = 1024;

        private EntityProjections EntityProjections => Ref.EntityProjections;
        private HashSet<GameObject> ActiveParticles = new();

        private void Awake()
        {
            Ref.ParticleManager = this;
        }

        private void Update()
        {
            List<GameObject> toRemove = ActiveParticles
                .Where(p => p == null)
                .ToList();
            
            ActiveParticles.ExceptWith(toRemove);
        }

        public void SpawnGlobalParticles(ParticleID id, Vec2 position)
        {
            GameObject prefab = GetParticlePrefab(id);
            if (prefab == null || ActiveParticles.Count >= MaxParticles) return;

            GameObject obj = GameObject.Instantiate(prefab).Relativise(position);
            obj.transform.SetParent(this.transform, false);
            ActiveParticles.Add(obj);
        }

        public void SpawnEntityParticles(ParticleID id, ulong uid, Vec2 localPosition)
        {
            GameObject prefab = GetParticlePrefab(id);
            if (prefab == null || ActiveParticles.Count >= MaxParticles) return;

            if (EntityProjections.TryGetProjection(uid, out var projection))
            {
                GameObject obj = GameObject.Instantiate(prefab);
                obj.transform.SetParent(projection.transform, false);
                obj.transform.localPosition = localPosition.ExtractPosition(Vec2.Zero);
                ActiveParticles.Add(obj);
            }
        }

        private GameObject GetParticlePrefab(ParticleID id)
        {
            GameObject prefab;
            if ((prefab = Resources.GetPrefab("Particles", id.ToString())) != null)
                return prefab;
            
            return null;
        }
    }
}
