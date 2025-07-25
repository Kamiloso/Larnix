using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Larnix.Client
{
    public class LoadingScreen : MonoBehaviour
    {
        [SerializeField] Image Background;
        [SerializeField] RectTransform Rotor;
        [SerializeField] TextMeshProUGUI Text;

        private const float ROTOR_SPEED = 360f;
        private const float OPACITY_SPEED = 2f;

        private bool active = true;
        private string info = "Loading...";
        private float opacity = 1f;

        private void Awake()
        {
            References.LoadingScreen = this;
        }

        private void Start()
        {
            transform.localPosition = Vector2.zero;
        }

        private void Update()
        {
            Text.text = info;
            Background.color = SetTransparency(Background.color, opacity);

            if (active)
            {
                Rotor.rotation = Quaternion.Euler(0f, 0f, Rotor.eulerAngles.z - ROTOR_SPEED * Time.deltaTime);
            }
            else
            {
                opacity -= OPACITY_SPEED * Time.deltaTime;
                if (opacity < 0f)
                    opacity = 0f;
            }
        }

        public void Enable(string info)
        {
            this.info = info;

            active = true;
            Rotor.rotation = Quaternion.Euler(0f, 0f, 0f);
            opacity = 1f;

            Rotor.gameObject.SetActive(true);
            Text.gameObject.SetActive(true);
        }

        public void Disable()
        {
            active = false;

            Rotor.gameObject.SetActive(false);
            Text.gameObject.SetActive(false);
        }

        private static Color SetTransparency(Color color, float transparency)
        {
            return new Color(color.r, color.g, color.b, transparency);
        }
    }
}
