using UnityEditor;
using UnityEditor.Experimental.GraphView;
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
        public static string WorldDirectory { get; private set; } = string.Empty;
        public static string ServerAddress { get; private set; } = string.Empty;

        public static void StartLocal(string world_directory)
        {
            LoadType = LoadTypes.Local;
            WorldDirectory = world_directory;
            ServerAddress = "[::]:0"; // temporary empty address

            SceneManager.LoadScene("Client");
            // client will load server on awake
        }

        public static void GenerateLocalAddress()
        {
            Server.Server server = UnityEngine.Object.FindObjectsByType<Server.Server>(UnityEngine.FindObjectsSortMode.None)[0];
            ServerAddress = "localhost:" + server.GetRunningPort();
        }

        public static void StartRemote(string server_address)
        {
            LoadType = LoadTypes.Remote;
            WorldDirectory = string.Empty;
            ServerAddress = server_address;

            SceneManager.LoadScene("Client");
        }

        public static void StartServer(string world_directory)
        {
            LoadType = LoadTypes.Server;
            WorldDirectory = world_directory;
            ServerAddress = string.Empty;

            SceneManager.LoadScene("Server");
        }
    }
}
