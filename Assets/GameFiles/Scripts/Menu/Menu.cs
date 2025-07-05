using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Larnix.Menu
{
    public class Menu : MonoBehaviour
    {
        public void Awake()
        {
            bool isServerBuild =
                SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null &&
                Application.isBatchMode;

            if (isServerBuild)
                StartServer();
            else
                UnityEngine.Debug.Log("Menu loaded");
        }

        // Start client / start server locally
        public void StartSingleplayer()
        {
            WorldLoad.StartLocal();
        }

        // Start client / connect to server with given IP
        public void StartMultiplayer(string ip_address)
        {
            WorldLoad.StartRemote(ip_address);
        }

        // Start server alone
        public void StartServer()
        {
            WorldLoad.StartServer();
        }
    }
}
