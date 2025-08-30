using UnityEditor;
using UnityEngine.SceneManagement;
using QuickNet.Backend;
using System.IO;
using Larnix.Menu.Worlds;

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

        // Universal data
        public static LoadTypes LoadType = LoadTypes.None;
        public static string ScreenLoad = "MainMenu";

        // Server data
        public static string WorldDirectory = "";
        public static bool IsHost = false;

        // Client data
        public static string Address = "";
        public static string Authcode = "";
        public static string Nickname = "";
        public static string Password = "";

        // Seed suggestion
        private static long? _SeedSuggestion;
        public static long? SeedSuggestion
        {
            get
            {
                long? temp = _SeedSuggestion;
                _SeedSuggestion = null;
                return temp;
            }
            set
            {
                _SeedSuggestion = value;
            }
        }

        public static void StartLocal(string world, string nickname)
        {
            LoadType = LoadTypes.Local;
            ScreenLoad = "GameUI";

            WorldDirectory = Path.Combine(WorldSelect.SavesPath, world);
            IsHost = false;

            Address = null; // server will set
            Authcode = null; // server will set
            Nickname = nickname;
            Password = QuickServer.LoopbackOnlyPassword;

            // Client will load the server on awake
            SceneManager.LoadScene("Client");
        }

        public static void StartRemote(string address, string authcode, string nickname, string password)
        {
            LoadType = LoadTypes.Remote;
            ScreenLoad = "GameUI";

            Address = address;
            Authcode = authcode;
            Nickname = nickname;
            Password = password;

            // Client will try to connect to the server
            SceneManager.LoadScene("Client");
        }

        // Executes directly from server scene
        public static void StartServer()
        {
            LoadType = LoadTypes.Server;
            ScreenLoad = "None";

            WorldDirectory = Path.Combine(".", "World");
            IsHost = true;
        }
    }
}
