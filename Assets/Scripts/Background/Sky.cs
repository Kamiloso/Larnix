using UnityEngine;
using Larnix.Core.Vectors;
using Larnix.Core;
using Larnix.GameCore.Enums;

namespace Larnix.Background
{
    public class Sky : MonoBehaviour
    {
        [SerializeField] Camera MainCamera;
        [SerializeField] float transitionSpeed;

        private bool _colorInitialized = false;
        public Color TargetColor { get; private set; } = Color.black;
        public Color CurrentColor { get; private set; } = Color.black;

        private void Awake()
        {
            GlobRef.Set(this);
        }

        private void LateUpdate()
        {
            Color vec1 = TargetColor - CurrentColor;
            CurrentColor += vec1 * transitionSpeed * Time.deltaTime;
            Color vec2 = TargetColor - CurrentColor;

            if (vec1.r * vec2.r +
                vec1.g * vec2.g +
                vec1.b * vec2.b +
                vec1.a * vec2.a <= 0) // dot product
            {
                CurrentColor = TargetColor;
            }

            MainCamera.backgroundColor = CurrentColor;
        }

        public void UpdateSky(BiomeID biomeID, Col32 skyColor, WeatherID weather)
        {
            TargetColor = skyColor.ToUnity();

            if (!_colorInitialized)
            {
                CurrentColor = TargetColor;
                _colorInitialized = true;
            }
        }
    }
}
