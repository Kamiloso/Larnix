using UnityEditor;
using UnityEngine.SceneManagement;
using System.IO;
using Larnix.Menu.Worlds;
using Larnix.ServerRun;
using Larnix.Socket.Backend;
using ServerType = Larnix.Server.ServerType;

namespace Larnix
{
    public static class WorldLoad
    {
        // Client data
        public static string ScreenLoad { get; private set; } = "MainMenu";
        public static string WorldPath { get; private set; } = null;
        public static bool IsMultiplayer { get; private set; } = false;
        public static bool PlayedAlready { get; private set; } = false;

        // Login data
        public static string Address { get; private set; } = "";
        public static string Authcode { get; private set; } = "";
        public static string Nickname { get; private set; } = "";
        public static string Password { get; private set; } = "";

        public static void StartLocal(string world, string nickname, long? seedSuggestion = null)
        {
            ScreenLoad = "GameUI";
            WorldPath = Path.Combine(WorldSelect.SavesPath, world);
            IsMultiplayer = false;
            PlayedAlready = true;

            // Load server
            var tuple = ServerRunner.Instance.StartServer(ServerType.Local, WorldPath, seedSuggestion);

            // Configure client
            Address = tuple.address;
            Authcode = tuple.authcode;
            Nickname = nickname;
            Password = QuickServer.LoopbackOnlyPassword;

            // Load client
            SceneManager.LoadScene("Client");
        }

        public static void StartHost(string world, string nickname, (string address, string authcode) serverTuple)
        {
            ScreenLoad = "GameUI";
            WorldPath = Path.Combine(WorldSelect.SavesPath, world);
            IsMultiplayer = false;
            PlayedAlready = true;

            // server is running locally...

            // Configure client
            Address = serverTuple.address;
            Authcode = serverTuple.authcode;
            Nickname = nickname;
            Password = QuickServer.LoopbackOnlyPassword;

            // Load client
            SceneManager.LoadScene("Client");
        }

        public static void StartRemote(string address, string authcode, string nickname, string password)
        {
            ScreenLoad = "GameUI";
            WorldPath = null;
            IsMultiplayer = true;
            PlayedAlready = true;

            // server is running remotely...

            Address = address;
            Authcode = authcode;
            Nickname = nickname;
            Password = password;

            // Load client
            SceneManager.LoadScene("Client");
        }

        public static void SetStartingScreen(string screenName)
        {
            ScreenLoad = screenName;
        }
    }
}
