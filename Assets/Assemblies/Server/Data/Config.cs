using System.IO;
using Larnix.Core.Files;
using Larnix.Core.Utils;
using SimpleJSON;

namespace Larnix.Server.Data
{
    internal class Config
    {
        public ushort ConfigVersion = 6;
        public ushort MaxPlayers = 10;
        public ushort Port = Common.LARNIX_PORT;
        public string Motd = "Welcome to Larnix server!";
        public int DataSavingPeriodFrames = 15 * 50;
        public int EntityBroadcastPeriodFrames = 2;
        public int ChunkSendingPeriodFrames = 8;
        public int ClientIdentityPrefixSizeIPv4 = 32;
        public int ClientIdentityPrefixSizeIPv6 = 56;
        public bool UseRelay = false;
        public string RelayAddress = Common.DEFAULT_RELAY_ADDRESS;

        private Config() { }

        public static Config Obtain(string path)
        {
            string data = FileManager.Read(path, "config.json");

            try
            {
                if (!string.IsNullOrEmpty(data))
                {
                    JSONNode json = JSON.Parse(data);
                    Config cfg = FromJson(json);
                    cfg.UpdateConfig();
                    return cfg;
                }
            }
            catch
            {
                Core.Debug.LogWarning(
                    "File " + Path.Combine(path, "config.json") + " was broken! Generating new..."
                );
            }

            Config newConfig = new Config();
            newConfig.Save(path);
            return newConfig;
        }

        public void Save(string path)
        {
            JSONNode json = ToJson();
            FileManager.Write(path, "config.json", json.ToString(2));
        }

        private static Config FromJson(JSONNode json)
        {
            Config cfg = new Config()
            {
                ConfigVersion = (ushort)json["ConfigVersion"].AsInt,
                MaxPlayers = (ushort)json["MaxPlayers"].AsInt,
                Port = (ushort)json["Port"].AsInt,
                Motd = json["Motd"],
                DataSavingPeriodFrames = json["DataSavingPeriodFrames"].AsInt,
                EntityBroadcastPeriodFrames = json["EntityBroadcastPeriodFrames"].AsInt,
                ClientIdentityPrefixSizeIPv4 = json["ClientIdentityPrefixSizeIPv4"].AsInt,
                ClientIdentityPrefixSizeIPv6 = json["ClientIdentityPrefixSizeIPv6"].AsInt,
                UseRelay = json["UseRelay"].AsBool,
                RelayAddress = json["RelayAddress"],
                ChunkSendingPeriodFrames = json["ChunkSendingPeriodFrames"].AsInt,
            };

            return cfg;
        }

        private JSONNode ToJson()
        {
            JSONObject json = new JSONObject();

            json["ConfigVersion"] = (int)ConfigVersion;
            json["MaxPlayers"] = (int)MaxPlayers;
            json["Port"] = (int)Port;
            json["Motd"] = Motd;
            json["DataSavingPeriodFrames"] = DataSavingPeriodFrames;
            json["EntityBroadcastPeriodFrames"] = EntityBroadcastPeriodFrames;
            json["ClientIdentityPrefixSizeIPv4"] = ClientIdentityPrefixSizeIPv4;
            json["ClientIdentityPrefixSizeIPv6"] = ClientIdentityPrefixSizeIPv6;
            json["UseRelay"] = UseRelay;
            json["RelayAddress"] = RelayAddress;
            json["ChunkSendingPeriodFrames"] = ChunkSendingPeriodFrames;

            return json;
        }

        private void UpdateConfig()
        {
            Config defaults = new Config();

            if (ConfigVersion < 2)
            {
                ClientIdentityPrefixSizeIPv4 = defaults.ClientIdentityPrefixSizeIPv4;
                ClientIdentityPrefixSizeIPv6 = defaults.ClientIdentityPrefixSizeIPv6;
            }
            if (ConfigVersion < 4)
            {
                UseRelay = defaults.UseRelay;
                RelayAddress = defaults.RelayAddress;
            }
            if (ConfigVersion < 5)
            {
                DataSavingPeriodFrames = defaults.DataSavingPeriodFrames;
                EntityBroadcastPeriodFrames = defaults.EntityBroadcastPeriodFrames;
            }
            if (ConfigVersion < 6)
            {
                ChunkSendingPeriodFrames = defaults.ChunkSendingPeriodFrames;
            }

            ConfigVersion = defaults.ConfigVersion;
        }
    }
}
