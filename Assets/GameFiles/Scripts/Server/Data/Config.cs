using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Larnix.Files;

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
        public float DataSavingPeriod = 15.00f;
        public float EntityBroadcastPeriod = 0.05f;

        public Config(bool local)
        {
            if(local)
            {
                MaxPlayers = 1;
                Port = 0;
                AllowRemoteClients = false;
            }
        }

        public static Config Obtain(string path, bool defaultIsLocal)
        {
            string data = FileManager.Read(path, "config.json");
            
            try
            {
                if (data != null)
                    return JsonUtility.FromJson<Config>(data);
            }
            catch
            {
                UnityEngine.Debug.LogWarning("File " + Path.Combine(path, "config.json") + " was broken! Generating new...");
            }
            
            Config newConfig = new Config(defaultIsLocal);
            Save(path, newConfig);
            return newConfig;
        }

        public static void Save(string path, Config config)
        {
            string data = JsonUtility.ToJson(config, true);
            FileManager.Write(path, "config.json", data);
        }
    }
}
