using System.Collections;
using UnityEngine;
using System.IO;
using Larnix.Core;

namespace Larnix.Client
{
    public class Screenshots : MonoBehaviour
    {
        [SerializeField] Camera ScreenshotCamera;

        private Client Client => GlobRef.Get<Client>();
        private MainPlayer MainPlayer => GlobRef.Get<MainPlayer>();

        private bool _cameraReady = false;

        private void Awake()
        {
            GlobRef.Set(this);
        }

        private void Start()
        {
            StartCoroutine(WaitForCameraReady());
        }

        private IEnumerator WaitForCameraReady()
        {
            yield return new WaitForEndOfFrame();
            _cameraReady = true;
        }

        public bool TryCaptureTitleImage()
        {
            if (Client != null && Client.WorldPath != null && MainPlayer.Alive)
            {
                string filename = Path.Combine(WorldLoad.WorldPath, "last_image.png");
                return ScreenshotSave(ScreenshotCamera, filename, 16f / 9f);
            }
            return false;
        }

        private bool ScreenshotSave(Camera camera, string path, float ratio)
        {
            if (camera == null || !_cameraReady)
            {
                Echo.LogWarning("Screenshot failed!");
                return false;
            }

            int screenW = Screen.width;
            int screenH = Screen.height;

            // Calculate ratio intersect
            float currentRatio = (float)screenW / screenH;
            int targetW = screenW;
            int targetH = screenH;

            if (Mathf.Abs(currentRatio - ratio) > 0.001f)
            {
                if (currentRatio > ratio)
                    targetW = Mathf.RoundToInt(screenH * ratio); // width
                else
                    targetH = Mathf.RoundToInt(screenW / ratio); // height
            }

            // Render into texture
            RenderTexture rt = new RenderTexture(targetW, targetH, 24);
            camera.targetTexture = rt;
            Texture2D tex = new Texture2D(targetW, targetH, TextureFormat.RGB24, false);

            camera.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, targetW, targetH), 0, 0);
            tex.Apply();

            // Save PNG
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);

            // Cleaning
            camera.targetTexture = null;
            RenderTexture.active = null;
            Destroy(rt);
            Destroy(tex);

            return true;
        }
    }
}
