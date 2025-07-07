using Larnix.Socket.Data;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Larnix.Server.Data
{
    [System.Serializable]
    public class Config
    {
        /*
        Default remote server values are default class parameters.
        Local server values are constructed by modifying default values with a special method.
        Remote server can store its values in JSON file and load them on start.
        */

        public ushort MaxPlayers = 10;
        public ushort Port = 27682;
        public bool AllowRemoteClients = true;
        public string Motd = "Welcome to Larnix server!";

        public Config(bool local)
        {
            if(local)
            {
                MaxPlayers = 1;
                Port = 0;
                AllowRemoteClients = false;
            }
        }

        public static Config Obtain(bool defaultIsLocal)
        {
            string data = FileManager.Read(WorldLoad.WorldDirectory, "config.json");
            
            try
            {
                if (data != null)
                    return JsonUtility.FromJson<Config>(data);
            }
            catch
            {
                UnityEngine.Debug.LogWarning("File " + Path.Combine(WorldLoad.WorldDirectory, "config.json") + " was broken! Generating new...");
            }
            
            Config newConfig = new Config(defaultIsLocal);
            Save(newConfig);
            return newConfig;
        }

        public static void Save(Config config)
        {
            string data = JsonUtility.ToJson(config, true);
            FileManager.Write(WorldLoad.WorldDirectory, "config.json", data);
        }
    }
}
