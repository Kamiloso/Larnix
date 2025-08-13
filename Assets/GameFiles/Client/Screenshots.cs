using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Larnix.Menu.Worlds;

namespace Larnix.Client
{
    public class Screenshots : MonoBehaviour
    {
        [SerializeField] Camera ScreenshotCamera;

        private bool cameraReady = false;

        private void Awake()
        {
            References.Screenshots = this;
        }

        private void Start()
        {
            StartCoroutine(WaitForCameraReady());
        }

        IEnumerator WaitForCameraReady()
        {
            yield return new WaitForEndOfFrame();
            cameraReady = true;
        }

        public bool ScreenshotSave(Camera camera, string path, float ratio)
        {
            if (camera == null || !cameraReady)
            {
                UnityEngine.Debug.Log("Screenshot failied.");
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
            Object.Destroy(rt);
            Object.Destroy(tex);

            UnityEngine.Debug.Log($"Screenshot saved to: {path}");
            return true;
        }

        public void CaptureTitleImage()
        {
            UnityEngine.Debug.Log("Capturing title image...");
            ScreenshotSave(ScreenshotCamera, Path.Combine(WorldLoad.WorldDirectory, "last_image.png"), 16f / 9f);
        }

        public void RemoveTitleImage()
        {
            UnityEngine.Debug.Log("Removing title image...");
            string file = Path.Combine(WorldLoad.WorldDirectory, "last_image.png");
            if(File.Exists(file))
                File.Delete(file);
        }
    }
}
