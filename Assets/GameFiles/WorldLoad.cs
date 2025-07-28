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
        public static string ServerAddress = "";
        public static string WorldDirectory = "";
        public static string ScreenLoad = "MainMenu";
        public static long? SeedSuggestion = null;

        // Set on client start and reset on client exit, WARNING: null -> no SYN encryption
        public static byte[] RsaPublicKey = null;

        // Client data
        public static string Nickname = "";
        public static string Password = "";

        public static void StartLocal(string worldName)
        {
            LoadType = LoadTypes.Local;
            WorldDirectory = Path.Combine(Application.persistentDataPath, "Saves", worldName);
            ScreenLoad = "GameUI";

            // NEVER change Nickname / Password local settings (compatibility with older worlds)
            Nickname = "Player";
            Password = "SGP_PASSWORD";

            // Client will load the server on awake
            SceneManager.LoadScene("Client");
        }

        public static void StartRemote(string server_address, string nickname, string password, byte[] public_key)
        {
            LoadType = LoadTypes.Remote;
            ServerAddress = server_address;
            ScreenLoad = "GameUI";

            Nickname = nickname;
            Password = password;

            RsaPublicKey = public_key;

            // Client will try to connect to the server
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
