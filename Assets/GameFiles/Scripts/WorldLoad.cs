using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.Security.Cryptography;
using System.IO;
using UnityEngine;

namespace Larnix
{
    public static class WorldLoad
    {
        public enum LoadTypes
        {
            None, // nothing was set
            Local, // start client and server locally
            Remote, // start client and connect to remote server
            Server // start server without client
        }

        public static LoadTypes LoadType { get; private set; } = LoadTypes.None;
        public static string ServerAddress { get; set; } = string.Empty;
        public static string WorldDirectory { get; set; } = string.Empty;

        // Set on client start and reset on client exit, WARNING: null -> no SYN encryption
        public static byte[] RsaPublicKey { get; set; } = null;

        // Client data
        public static string Nickname { get; set; } = string.Empty;
        public static string Password { get; set; } = string.Empty;

        public static void StartLocal(string worldName)
        {
            LoadType = LoadTypes.Local;

            WorldDirectory = Path.Combine(Application.persistentDataPath, "Saves", worldName);

            // NEVER change Nickname / Password local settings (compatibility with older worlds)
            Nickname = "Player";
            Password = "SGP_PASSWORD";

            SceneManager.LoadScene("Client");
            // client will load server on awake
        }

        public static void StartRemote(string server_address)
        {
            LoadType = LoadTypes.Remote;
            ServerAddress = server_address;

            Nickname = "Player" + (new System.Random()).Next(0,100);
            Password = "TEMP_PASSWORD";

            SceneManager.LoadScene("Client");
        }

        // Executes directly from server scene
        public static void StartServer()
        {
            LoadType = LoadTypes.Server;

            WorldDirectory = Path.Combine(".", "World");
        }
    }
}
