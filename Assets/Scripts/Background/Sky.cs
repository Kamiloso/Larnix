using UnityEngine;
using Larnix.Worldgen.Biomes;
using Larnix.Core.Vectors;
using Larnix.Core;

namespace Larnix.Background
{
    public class Sky : MonoBehaviour
    {
        [SerializeField] Camera MainCamera;

        private void Awake()
        {
            GlobRef.Set(this);
        }

        public void UpdateSky(BiomeID biomeID, Col32 skyColor, Weather weather)
        {
            MainCamera.backgroundColor = skyColor.ToUnity();
        }
    }
}
