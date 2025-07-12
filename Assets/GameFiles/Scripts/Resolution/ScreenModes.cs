using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Resolution
{
    public class ScreenModes : MonoBehaviour
    {
        private static bool initialized = false;

        private void Start()
        {
            if (!initialized)
            {
                initialized = true;
                SetFullScreen(true);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F11))
                SetFullScreen(!Screen.fullScreen);
        }

        private void SetFullScreen(bool full)
        {
            if (full)
            {
                int nativeWidth = Screen.currentResolution.width;
                int nativeHeight = Screen.currentResolution.height;
                Screen.SetResolution(nativeWidth, nativeHeight, true);
            }
            else
            {
                Screen.SetResolution(1280, 720, false);
            }
        }
    }
}
