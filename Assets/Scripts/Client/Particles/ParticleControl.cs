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
        [SerializeField] bool InheritsBlockTexture;

        private GameObject BackTilemapPrefab => Resources.GetPrefab("Tilemaps", "TilemapBack");
        private ParticleSystemRenderer Renderer;
        private Color FrontColor;
        private Color BackColor;

        private void Awake()
        {
            if (ParticleSystem == null)
            {
                throw new Exception($"ParticleControl on GameObject \"{name}\" is missing a reference to its ParticleSystem.");
            }

            Renderer = ParticleSystem.GetComponent<ParticleSystemRenderer>();

            FrontColor = Color.white;
            BackColor = BackTilemapPrefab.GetComponent<Tilemap>().color;

            Renderer.sortingLayerName = "FrontEffects"; // default for effects
        }

        public void DisableRenderer()
        {
            Renderer.enabled = false;
        }

        public bool UsesBlockTexture() => InheritsBlockTexture;
        public void SetTextureFromBlock(BlockData1 blockData, bool front)
        {
            if (!InheritsBlockTexture) return;

            Texture2D texture = Tiles.GetTexture(blockData, front);
            Renderer.material.mainTexture = texture;

            if (front)
            {
                Renderer.material.color = FrontColor;
                Renderer.sortingLayerName = "FrontEffects";
            }
            else
            {
                Renderer.material.color = BackColor;
                Renderer.sortingLayerName = "BackEffects";
            }
        }
    }
}
