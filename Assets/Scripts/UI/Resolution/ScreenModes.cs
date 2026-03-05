using System.Collections;
using System.Collections.Generic;
using Larnix.Scoping;
using UnityEngine;

namespace Larnix.UI.Resolution
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
            if (MyInput.GetKeyDown(KeyCode.F11, ScopeID.All))
            {
                SetFullScreen(!Screen.fullScreen);
            }
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
