using System;
using Larnix.Blocks;
using Larnix.Blocks.Structs;
using Larnix.Client.Terrain;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Larnix.Client.Particles
{
    public class ParticleControl : MonoBehaviour
    {
        [SerializeField] ParticleSystem ParticleSystem;
        [SerializeField] GameObject BackTilemapPrefab;
        [SerializeField] bool InheritsBlockTexture;

        private ParticleSystemRenderer Renderer;
        private Color BackColor;

        private void Awake()
        {
            Renderer = ParticleSystem.GetComponent<ParticleSystemRenderer>();
            Tilemap tilemap = BackTilemapPrefab.GetComponent<Tilemap>();
            BackColor = tilemap != null ? tilemap.color : Color.white;
        }

        public bool TryTextureFromBlock(BlockData1 blockData, bool front)
        {
            if (!InheritsBlockTexture) return false;

            Texture2D texture = Tiles.GetTexture(blockData, front);
            Renderer.material.mainTexture = texture;
            if (!front)
            {
                Renderer.material.color = BackColor;
            }
            
            return true;
        }
    }
}
