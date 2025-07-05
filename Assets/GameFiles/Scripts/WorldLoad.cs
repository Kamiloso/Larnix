using UnityEditor;
using UnityEngine.SceneManagement;
using System;

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
        public static string ServerAddress { get; private set; } = string.Empty;

        public static void StartLocal()
        {
            LoadType = LoadTypes.Local;
            ServerAddress = "[::]:0"; // temporary empty address

            SceneManager.LoadScene("Client");
            // client will load server on awake
        }

        public static void GenerateLocalAddress()
        {
            Server.Server[] servers = UnityEngine.Object.FindObjectsByType<Server.Server>(UnityEngine.FindObjectsSortMode.None);
            if (servers.Length > 0)
                ServerAddress = "localhost:" + servers[0].RealPort;
            else
                throw new Exception("Couldn't find the local server!");
        }

        public static void StartRemote(string server_address)
        {
            LoadType = LoadTypes.Remote;
            ServerAddress = server_address;

            SceneManager.LoadScene("Client");
        }

        public static void StartServer()
        {
            LoadType = LoadTypes.Server;
            ServerAddress = string.Empty;

            SceneManager.LoadScene("Server");
        }
    }
}
