using System.Collections;
using System.Collections.Generic;
using System.IO;
using Larnix.Core.Files;
using Larnix.Core.Utils;
using UnityEngine;

namespace Larnix.Server.Data
{
    [System.Serializable]
    internal class Config
    {
        /*
        Default remote server values are default class parameters.
        Local server values are constructed by modifying default values in a constructor.
        Server (local / host / remote) can store its values into JSON file and load them on start.
        */

        public ushort ConfigVersion = 5;
        public ushort MaxPlayers = 10;
        public ushort Port = Common.LarnixPort;
        public string Motd = "Welcome to Larnix server!";
        public int DataSavingPeriodFrames = 15 * 50;
        public int EntityBroadcastPeriodFrames = 2;
        public int ClientIdentityPrefixSizeIPv4 = 32;
        public int ClientIdentityPrefixSizeIPv6 = 56;
        public bool UseRelay = false;
        public string RelayAddress = "relay-1.se3.page";

        private Config() { }

        public static Config Obtain(string path)
        {
            string data = FileManager.Read(path, "config.json");
            
            try
            {
                if (data != null)
                {
                    Config readConfig = JsonUtility.FromJson<Config>(data);
                    readConfig.UpdateConfig();
                    return readConfig;
                }
            }
            catch
            {
                Core.Debug.LogWarning("File " + Path.Combine(path, "config.json") + " was broken! Generating new...");
            }
            
            Config newConfig = new Config();
            newConfig.Save(path);
            return newConfig;
        }

        public void Save(string path)
        {
            string data = JsonUtility.ToJson(this, true);
            FileManager.Write(path, "config.json", data);
        }

        private void UpdateConfig()
        {
            Config defaultConfig = new Config();
            
            if (ConfigVersion < 2)
            {
                ClientIdentityPrefixSizeIPv4 = defaultConfig.ClientIdentityPrefixSizeIPv4;
                ClientIdentityPrefixSizeIPv6 = defaultConfig.ClientIdentityPrefixSizeIPv6;
            }
            if (ConfigVersion < 4)
            {
                UseRelay = defaultConfig.UseRelay;
                RelayAddress = defaultConfig.RelayAddress;
            }
            if (ConfigVersion < 5)
            {
                DataSavingPeriodFrames = defaultConfig.DataSavingPeriodFrames;
                EntityBroadcastPeriodFrames = defaultConfig.EntityBroadcastPeriodFrames;
            }
            // if(oldConfig.ConfigVersion < 6)
            // {
            //     UPDATE MORE VARIABLES
            // }

            // Add more if(s) when updating config.

            ConfigVersion = defaultConfig.ConfigVersion;
        }
    }
}
