using Larnix.Socket;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading;
using UnityEditor;
using Larnix.Socket.Commands;
using System.Linq;
using System;

namespace Larnix.Menu
{
    public class Menu : MonoBehaviour
    {
        public void Awake()
        {
            Application.runInBackground = true;

            UnityEngine.Debug.Log("Menu loaded");
        }

        // Start client / start server locally
        public void StartSingleplayer(string worldName)
        {
            WorldLoad.StartLocal(worldName);
        }

        // Quit
        public void Quit()
        {
            Application.Quit();
        }
    }
}
