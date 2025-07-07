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
            bool isServerBuild =
                SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null &&
                Application.isBatchMode;

            if (isServerBuild)
                StartServer();
            else
                UnityEngine.Debug.Log("Menu loaded");
        }

        // Start client / start server locally
        public void StartSingleplayer(string worldName)
        {
            WorldLoad.StartLocal(worldName);
        }

        // Start client / connect to server with given IP
        public void StartMultiplayer(string ip_address)
        {
            A_ServerInfo answer = Resolver.downloadServerInfo(ip_address, "Player");
            if (answer != null)
            {
                byte[] modulus = answer.PublicKeyModulus;
                byte[] exponent = answer.PublicKeyExponent;

                WorldLoad.RsaPublicKey = modulus.Concat(exponent).ToArray();
                WorldLoad.StartRemote(ip_address);
            }
        }

        // Start server alone
        public void StartServer()
        {
            WorldLoad.StartServer();
        }
    }
}
