using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Larnix.Files;
using Larnix.Socket;

namespace Larnix.Server.Data
{
    [System.Serializable]
    public class Config
    {
        /*
        Default remote server values are default class parameters.
        Local server values are constructed by modifying default values in constructor.
        Server (local / remote) can store its values into JSON file and load them on start.
        */

        public ushort ConfigVersion = 2;
        public ushort MaxPlayers = 10;
        public ushort Port = 27682;
        public bool AllowRemoteClients = true;
        public string Motd = "Welcome to Larnix server!";
        public float DataSavingPeriod = 15.00f;
        public float EntityBroadcastPeriod = 0.04f;
        public int ClientIdentityPrefixSizeIPv4 = 32;
        public int ClientIdentityPrefixSizeIPv6 = 56;

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
                {
                    Config readConfig = JsonUtility.FromJson<Config>(data);
                    UpdateConfig(readConfig, defaultIsLocal);
                    return readConfig;
                }
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

        private static void UpdateConfig(Config oldConfig, bool defaultIsLocal)
        {
            Config defaultConfig = new Config(defaultIsLocal);
            
            if(oldConfig.ConfigVersion < 1)
            {
                oldConfig.DataSavingPeriod = defaultConfig.DataSavingPeriod;
                oldConfig.EntityBroadcastPeriod = defaultConfig.EntityBroadcastPeriod;
            }
            if(oldConfig.ConfigVersion < 2)
            {
                oldConfig.ClientIdentityPrefixSizeIPv4 = defaultConfig.ClientIdentityPrefixSizeIPv4;
                oldConfig.ClientIdentityPrefixSizeIPv6 = defaultConfig.ClientIdentityPrefixSizeIPv6;
            }
            // if(oldConfig.ConfigVersion < 3)
            // {
            //     UPDATE MORE VARIABLES
            // }

            // Add more if(s) when updating config.

            oldConfig.ConfigVersion = defaultConfig.ConfigVersion;
        }
    }
}
