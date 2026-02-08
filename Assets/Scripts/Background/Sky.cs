using UnityEngine;
using Larnix.Worldgen;
using Larnix.Core.Vectors;
using Larnix.Core;

namespace Larnix.Background
{
    public class Sky : MonoBehaviour
    {
        [SerializeField] Camera MainCamera;

        private void Awake()
        {
            Ref.Sky = this;
        }

        public void UpdateSky(BiomeID biomeID, Col32 skyColor, Weather weather)
        {
            MainCamera.backgroundColor = skyColor.ToUnity();
        }
    }
}
