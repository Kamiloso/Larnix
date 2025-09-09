using UnityEditor;
using UnityEngine.SceneManagement;
using QuickNet.Backend;
using System.IO;
using Larnix.Menu.Worlds;
using Larnix.ServerRun;
using ServerType = Larnix.Server.ServerType;
using System;

namespace Larnix
{
    public static class WorldLoad
    {
        // Useful info
        public static string ScreenLoad = "MainMenu";
        public static string WorldPath = null;
        public static bool IsMultiplayer = false;
        public static bool PlayedAlready = false;

        // Client data 1
        public static string Address = "";
        public static string Authcode = "";
        public static string Nickname = "";
        public static string Password = "";

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

            Address = address;
            Authcode = authcode;
            Nickname = nickname;
            Password = password;

            // server is running remotely...

            // Load client
            SceneManager.LoadScene("Client");
        }
    }
}
