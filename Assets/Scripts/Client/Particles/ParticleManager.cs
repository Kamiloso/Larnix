using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Client.Entities;
using Larnix.Client.Relativity;
using UnityEngine;
using System.Linq;
using Larnix.Model.Blocks;
using Larnix.Model.Blocks.All;
using Larnix.Core;
using Larnix.Model.Enums;
using Larnix.Model.Blocks.Structs;

namespace Larnix.Client.Particles
{
    public class ParticleManager : MonoBehaviour
    {
        [SerializeField] int MaxParticles = 1024;

        private EntityProjections EntityProjections => GlobRef.Get<EntityProjections>();
        private HashSet<GameObject> ActiveParticles = new();

        private BlockHeader1 _optionBlock = BlockHeader1.Air;
        private bool _optionFront = false;

        private void Awake()
        {
            GlobRef.Set(this);
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

            Transform trn = Instantiate(prefab).transform.Relativise(position);
            trn.SetParent(transform, false);

            if (trn.TryGetComponent<ParticleControl>(out var particles))
            {
                if (particles.UsesBlockTexture())
                {
                    bool break_particles = BlockFactory.GetSlaveInstance<IBreakable>(_optionBlock.Id)?.HAS_BREAK_PARTICLES() == true;
                    bool place_particles = BlockFactory.GetSlaveInstance<IPlaceable>(_optionBlock.Id)?.HAS_PLACE_PARTICLES() == true;

                    if ((break_particles && id == ParticleID.BlockBreak) ||
                        (place_particles && id == ParticleID.BlockPlace))
                    {
                        particles.SetTextureFromBlock(
                            blockData: _optionBlock,
                            front: _optionFront
                        );
                    }
                    else
                    {
                        particles.DisableRenderer();
                    }
                }
            }

            ActiveParticles.Add(trn.gameObject);
        }

        public void SpawnEntityParticles(ParticleID id, ulong uid, Vec2 localPosition)
        {
            GameObject prefab = GetParticlePrefab(id);
            if (prefab == null || ActiveParticles.Count >= MaxParticles) return;

            if (EntityProjections.TryGetProjection(uid, true, out var projection))
            {
                GameObject obj = Instantiate(prefab);
                obj.transform.SetParent(projection.transform, false);
                obj.transform.localPosition = localPosition.ExtractPosition(Vec2.Zero);

                ActiveParticles.Add(obj);
            }
        }

        public void SpawnBlockParticles(BlockHeader1 blockData, Vec2Int POS, bool front, ParticleID id)
        {
            _optionBlock = blockData;
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
