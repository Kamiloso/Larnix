using System;
using Larnix.Model.Utils;
using System.Reflection;
using System.Collections.Generic;
using Larnix.Model.Json;
using Larnix.Model;

namespace Larnix.Server.Data;

internal class ServerConfig : Config
{
    public int ConfigVersion { get; set; } = 15;

    // --- Main ---
    public ushort MaxPlayers { get; set; } = 10;
    public ushort Port { get; set; } = GameInfo.DefaultPort;
    public string Motd { get; set; } = "Welcome to Larnix server!";

    // --- Administration ---
    public bool ElevateHostToAdmin { get; set; } = false;
    public List<string> Administration_Admins { get; init; } = new();
    public List<string> Administration_Banned { get; init; } = new();

    // --- Electric contraptions ---
    public int Electricity_MaxContraptionChunks { get; set; } = 64;
    public bool Electricity_SizeWarningSuppress { get; set; } = false;

    // --- Periodic tasks ---
    private int _periodicTasks_DataSavingPeriodFrames = 15 * GameInfo.TargetTPS;
    public int PeriodicTasks_DataSavingPeriodFrames
    {
        get => _periodicTasks_DataSavingPeriodFrames;
        set => _periodicTasks_DataSavingPeriodFrames = Math.Max(1, value);
    }

    private int _periodicTasks_EntityBroadcastPeriodFrames = 2;
    public int PeriodicTasks_EntityBroadcastPeriodFrames
    {
        get => _periodicTasks_EntityBroadcastPeriodFrames;
        set => _periodicTasks_EntityBroadcastPeriodFrames = Math.Max(1, value);
    }

    // --- Network ---
    public int Network_ClientIdentityPrefixSizeIPv4 { get; set; } = 32;
    public int Network_ClientIdentityPrefixSizeIPv6 { get; set; } = 56;
    public bool Network_AllowRegistration { get; set; } = true;
    public bool Network_UseRelay { get; set; } = false;
    public string Network_RelayAddress { get; set; } = GameInfo.DefaultRelayAddress;

    public override void Update()
    {
        ServerConfig defaults = new();

        if (ConfigVersion < 12) // fundamental refactor
        {
            List<PropertyInfo> props = AllProperties<ServerConfig>();
            foreach (PropertyInfo prop in props)
            {
                prop.SetValue(this, prop.GetValue(defaults));
            }
        }

        if (ConfigVersion < 13) // added: ElevateHostToAdmin
        {
            ElevateHostToAdmin = defaults.ElevateHostToAdmin;
        }

        if (ConfigVersion < 14) // updated: Electricity_MaxContraptionChunks: 128 -> 64
        {
            Electricity_MaxContraptionChunks = defaults.Electricity_MaxContraptionChunks;
        }

        if (ConfigVersion < 15) // removed: PeriodicTasks_ChunkSendingPeriodFrames
        {
            ;
        }

        ConfigVersion = defaults.ConfigVersion;
    }
}
