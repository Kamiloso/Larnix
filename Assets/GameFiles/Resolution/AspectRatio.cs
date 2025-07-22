using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Larnix.Resolution
{
    public class AspectRatio : MonoBehaviour
    {
        private const float targetaspect = 16.0f / 9.0f;
        private readonly List<Camera> cameras = new List<Camera>();
        private readonly List<CanvasScaler> canvasScalers = new List<CanvasScaler>();
        private float last_scaleheight = -1f;

        private void Start()
        {
            foreach (Camera cam in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if(cam.gameObject.scene == gameObject.scene)
                    cameras.Add(cam);
            }

            foreach (CanvasScaler canv in FindObjectsByType<CanvasScaler>(FindObjectsSortMode.None))
            {
                if(canv.gameObject.scene == gameObject.scene)
                    canvasScalers.Add(canv);
            }
        }

        private void Update()
        {
            float windowaspect = (float)Screen.width / (float)Screen.height;
            float scaleheight = windowaspect / targetaspect;

            if(scaleheight != last_scaleheight)
            {
                foreach (Camera cam in cameras)
                    MatchCamera(cam, scaleheight);

                foreach (CanvasScaler canv in canvasScalers)
                    MatchCanvas(canv, scaleheight);

                last_scaleheight = scaleheight;
            }
        }

        private void MatchCamera(Camera camera, float scaleheight)
        {
            if (scaleheight < 1.0f)
            {
                Rect rect = camera.rect;

                rect.width = 1.0f;
                rect.height = scaleheight;
                rect.x = 0;
                rect.y = (1.0f - scaleheight) / 2.0f;

                camera.rect = rect;
            }
            else
            {
                float scalewidth = 1.0f / scaleheight;

                Rect rect = camera.rect;

                rect.width = scalewidth;
                rect.height = 1.0f;
                rect.x = (1.0f - scalewidth) / 2.0f;
                rect.y = 0;

                camera.rect = rect;
            }
        }

        private void MatchCanvas(CanvasScaler canvasScaler, float scaleheight)
        {
            // Set all your canvases to any 16:9 reference resolution + scale with screen size
            canvasScaler.matchWidthOrHeight = scaleheight > 1.0f ? 1f : 0f;
        }
    }
}