using System.Collections.Generic;
using Larnix.Core;
using Larnix.Core.Vectors;
using Larnix.Client.Entities;
using Larnix.Client.Relativity;
using UnityEngine;
using System.Linq;
using Larnix.Blocks.Structs;
using Larnix.Blocks;

namespace Larnix.Client.Particles
{
    public class ParticleManager : MonoBehaviour
    {
        [SerializeField] int MaxParticles = 1024;

        private EntityProjections EntityProjections => Ref.EntityProjections;
        private HashSet<GameObject> ActiveParticles = new();

        private BlockData1 _optionBlock = new();
        private bool _optionFront = false;

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

            if (obj.TryGetComponent<ParticleControl>(out var particles))
            {
                if (particles.UsesBlockTexture())
                {
                    bool break_particles = BlockFactory.GetSlaveInstance<IBreakable>(_optionBlock.ID)?.HAS_BREAK_PARTICLES() == true;
                    bool place_particles = BlockFactory.GetSlaveInstance<IPlaceable>(_optionBlock.ID)?.HAS_PLACE_PARTICLES() == true;

                    if ((break_particles && id == ParticleID.BlockBreak) ||
                        (place_particles && id == ParticleID.BlockPlace))
                    {
                        particles.SetTextureFromBlock(
                            blockData: _optionBlock.DeepCopy(),
                            front: _optionFront
                        );
                    }
                    else
                    {
                        particles.DisableRenderer();
                    }
                }
            }

            ActiveParticles.Add(obj);
        }

        public void SpawnEntityParticles(ParticleID id, ulong uid, Vec2 localPosition)
        {
            GameObject prefab = GetParticlePrefab(id);
            if (prefab == null || ActiveParticles.Count >= MaxParticles) return;

            if (EntityProjections.TryGetProjection(uid, true, out var projection))
            {
                GameObject obj = GameObject.Instantiate(prefab);
                obj.transform.SetParent(projection.transform, false);
                obj.transform.localPosition = localPosition.ExtractPosition(Vec2.Zero);

                ActiveParticles.Add(obj);
            }
        }

        public void SpawnBlockParticles(BlockData1 blockData, Vec2Int POS, bool front, ParticleID id)
        {
            _optionBlock = blockData ?? new();
            _optionFront = front;

            SpawnGlobalParticles(id, POS.ToVec2());
        }

        private GameObject GetParticlePrefab(ParticleID id)
        {
            GameObject prefab;
            if ((prefab = Prefabs.GetPrefab("Particles", id.ToString())) != null)
                return prefab;
            
            return null;
        }
    }
}
