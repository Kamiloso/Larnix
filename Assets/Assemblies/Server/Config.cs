using System.IO;
using Larnix.Core.Files;
using Larnix.Core.Utils;
using SimpleJSON;

namespace Larnix.Server
{
    internal class Config
    {
        public int ConfigVersion { get; set; } = 11;

        // Server settings
        public ushort MaxPlayers { get; set; } = 10;
        public ushort Port { get; set; } = Common.LARNIX_PORT;
        public string Motd { get; set; } = "Welcome to Larnix server!";

        // Atomic / Electric contraption settings
        public int MaxElectricContraptionChunks { get; set; } = 128;
        public bool ElectricContraptionSizeWarningSuppress { get; set; } = false;

        // Periodic task settings
        private int _dataSavingPeriodFrames = 15 * 50;
        public int DataSavingPeriodFrames
        {
            get => _dataSavingPeriodFrames;
            set => _dataSavingPeriodFrames = value >= 1 ? value : 1;
        }

        private int _entityBroadcastPeriodFrames = 2;
        public int EntityBroadcastPeriodFrames
        {
            get => _entityBroadcastPeriodFrames;
            set => _entityBroadcastPeriodFrames = value >= 1 ? value : 1;
        }

        private int _chunkSendingPeriodFrames = 2;
        public int ChunkSendingPeriodFrames
        {
            get => _chunkSendingPeriodFrames;
            set => _chunkSendingPeriodFrames = value >= 1 ? value : 1;
        }

        // Network settings
        public int ClientIdentityPrefixSizeIPv4 { get; set; } = 32;
        public int ClientIdentityPrefixSizeIPv6 { get; set; } = 56;
        public bool AllowRegistration { get; set; } = true;
        public bool UseRelay { get; set; } = false;
        public string RelayAddress { get; set; } = Common.DefaultRelayAddress;

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
                ConfigVersion = json["ConfigVersion"].AsInt,
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
                MaxElectricContraptionChunks = json["MaxElectricContraptionChunks"].AsInt,
                ElectricContraptionSizeWarningSuppress = json["ElectricContraptionSizeWarningSuppress"].AsBool,
                AllowRegistration = json["AllowRegistration"].AsBool,
            };

            return cfg;
        }

        private JSONNode ToJson()
        {
            JSONObject json = new JSONObject();

            json["ConfigVersion"] = ConfigVersion;
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
            json["MaxElectricContraptionChunks"] = MaxElectricContraptionChunks;
            json["ElectricContraptionSizeWarningSuppress"] = ElectricContraptionSizeWarningSuppress;
            json["AllowRegistration"] = AllowRegistration;

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
            if (ConfigVersion < 8)
            {
                ChunkSendingPeriodFrames = defaults.ChunkSendingPeriodFrames;
            }
            if (ConfigVersion < 10)
            {
                MaxElectricContraptionChunks = defaults.MaxElectricContraptionChunks;
                ElectricContraptionSizeWarningSuppress = defaults.ElectricContraptionSizeWarningSuppress;
            }
            if (ConfigVersion < 11)
            {
                AllowRegistration = defaults.AllowRegistration;
            }

            ConfigVersion = defaults.ConfigVersion;
        }
    }
}
