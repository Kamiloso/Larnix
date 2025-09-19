using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using QuickNet;
using Larnix.Server.Data;
using Larnix.Core;
using QuickNet.Processing;
using System.IO;
using QuickNet.Backend;
using Larnix.Core.Physics;
using Larnix.Server.Entities;
using Larnix.Server.Terrain;
using Version = Larnix.Core.Version;
using Console = Larnix.Core.Console;
using System.Threading.Tasks;
using System.Linq;
using System.Net;
using QuickNet.Frontend;

namespace Larnix.Server
{
    public enum ServerType
    {
        Local, // pure singleplayer
        Host, // singleplayer published for remote hosts
        Remote, // multiplayer console server
    }

    public class Server
    {
        private readonly Locker Locker;
        private readonly Receiver Receiver;
        private readonly Commands Commands;
        private MetadataSGP? Mdata = null;

        public readonly ServerType Type;
        public readonly string WorldPath;
        public readonly string LocalAddress;
        public readonly string Authcode;

        public uint FixedFrame { get; private set; } = 0;
        private float saveCycleTimer = 0f;
        private float broadcastCycleTimer = 0f;

        public const float FIXED_TIME = Core.Common.FIXED_TIME;
        public readonly Action CloseServer;

        private bool _disposed = false;

        public Server(ServerType type, string worldPath, long? seedSuggestion, Action closeServer)
        {
            Type = type;
            WorldPath = worldPath;
            CloseServer = closeServer;

            // scripts
            Ref.Server = this;
            Ref.PhysicsManager = new PhysicsManager();
            Ref.EntityManager = new EntityManager();
            Ref.EntityDataManager = new EntityDataManager();
            Ref.PlayerManager = new PlayerManager();
            Ref.BlockDataManager = new BlockDataManager();
            Ref.ChunkLoading = new ChunkLoading();
            Ref.BlockSender = new BlockSender();

            // locker
            Locker = Locker.TryLock(WorldPath, "world_locker.lock");
            if (Locker != null)
            {
                // World metadata
                Mdata = MetadataSGP.ReadMetadataSGP(WorldPath, true);
                Mdata = new MetadataSGP(Version.Current, Mdata?.nickname);
                MetadataSGP.SaveMetadataSGP(WorldPath, (MetadataSGP)Mdata, true);

                // Satelite classes
                Ref.Config = Config.Obtain(WorldPath);
                Ref.Database = new Database(WorldPath, "database.sqlite");
                Ref.Generator = new Worldgen.Generator(Ref.Database.GetSeed(seedSuggestion));

                // Server configuration
                Ref.QuickServer = QuickServer.CreateServerSync(
                    port: Type == ServerType.Remote ? Ref.Config.Port : (ushort)0,
                    maxClients: Ref.Config.MaxPlayers,
                    isLoopback: Type == ServerType.Local,
                    dataPath: Path.Combine(WorldPath, "Socket"),
                    gameVersion: Version.Current.ID,
                    userText1: Validation.IsGoodText<String256>(Ref.Config.Motd) ? Ref.Config.Motd : "Wrong motd format :(", // motd
                    userText2: Type == ServerType.Remote ? "Player" : (Mdata?.nickname ?? "Player") // server owner ("Player" = detached)
                );
                Ref.QuickServer.ConfigureMasks(
                    Ref.Config.ClientIdentityPrefixSizeIPv4,
                    Ref.Config.ClientIdentityPrefixSizeIPv6
                    );

                // Readonly classes in this file
                Receiver = new Receiver(Ref.QuickServer);
                Commands = new Commands();

                // Configure for client
                LocalAddress = "localhost:" + Ref.QuickServer.Port;
                Authcode = Ref.QuickServer.Authcode;

                // Configure console
                if (Type == ServerType.Remote)
                {
                    ConfigureConsole();
                }
                else
                {
                    Core.Debug.Log("Port: " + Ref.QuickServer.Port + " | Authcode: " + Ref.QuickServer.Authcode);
                }

                // info success
                Core.Debug.LogSuccess("Server is ready!");

                // try establish relay
                if (Type == ServerType.Remote)
                {
                    if (Ref.Config.UseRelay)
                        _ = Task.Run(() => EstablishRelay(Ref.Config.RelayAddress));
                }
            }
            else throw new Exception("Trying to access world that is already open.");
        }

        private void ConfigureConsole()
        {
            // title set
            Console.SetTitle("Larnix Server " + Version.Current);
            Core.Debug.LogRaw(new string('-', 60) + "\n");

            // force change default password
            if (Mdata?.nickname != "Player")
            {
                string sgpNickname = Mdata?.nickname;

                Core.Debug.LogRaw("This world was previously in use by " + sgpNickname + ".\n");
                Core.Debug.LogRaw("Choose a password for this player to start the server.\n");

            password_ask:
                {
                    Core.Debug.LogRaw("> ");
                    string input = Console.GetInputSync();
                    if (Validation.IsGoodPassword(input))
                    {
                        Ref.QuickServer.UserManager.ChangePassword(sgpNickname, Hasher.HashPassword(input));
                        Mdata = new MetadataSGP(Version.Current, "Player");
                        MetadataSGP.SaveMetadataSGP(WorldPath, (MetadataSGP)Mdata, true);
                    }
                    else
                    {
                        Core.Debug.LogRaw("Password should be 7-32 characters and not end with NULL (0x00).\n");
                        goto password_ask;
                    }
                }

                Core.Debug.LogRaw("Password changed.\n");
                Core.Debug.LogRaw(new string('-', 60) + "\n");
            }

            // socket information
            Core.Debug.LogRaw("Socket created on port " + Ref.QuickServer.Port + "\n");
            Core.Debug.LogRaw("Authcode: " + Ref.QuickServer.Authcode + "\n");
            Core.Debug.LogRaw(new string('-', 60) + "\n");

            // input thread start
            Console.StartInputThread();
        }

        public async Task<string> EstablishRelay(string relayAddress)
        {
            ushort? relayPort = await Ref.QuickServer.ConfigureRelay(relayAddress);
            if (_disposed) return null;

            if (relayPort != null)
            {
                var uri = new UriBuilder("udp://" + relayAddress) { Port = relayPort.Value };
                string connectAddress = uri.ToString().Replace("udp://", "").Replace("/", "");

                Core.Debug.LogSuccess("Connected to relay!");
                Core.Debug.Log("Address: " + connectAddress);

                return connectAddress;
            }
            else
            {
                Core.Debug.LogWarning("Cannot connect to relay!");
                return null;
            }
        }

        public void TickFixed()
        {
            FixedFrame++;

            Ref.QuickServer.ServerTick(FIXED_TIME);

            Ref.ChunkLoading.FromEarlyUpdate(); // 1
            Ref.EntityManager.FromEarlyUpdate(); // 2

            Commands.ExecuteFrame();

            Ref.ChunkLoading.FromFixedUpdate(); // FIX-1
            Ref.EntityManager.FromFixedUpdate(); // FIX-2

            Ref.BlockSender.BroadcastInfo();

            broadcastCycleTimer += FIXED_TIME;
            if (Ref.Config.EntityBroadcastPeriod > 0f && broadcastCycleTimer > Ref.Config.EntityBroadcastPeriod)
            {
                broadcastCycleTimer %= Ref.Config.EntityBroadcastPeriod;
                Ref.EntityManager.SendEntityBroadcast();
            }

            saveCycleTimer += FIXED_TIME;
            if (Ref.Config.DataSavingPeriod > 0f && saveCycleTimer > Ref.Config.DataSavingPeriod)
            {
                saveCycleTimer %= Ref.Config.DataSavingPeriod;
                SaveAllNow();
            }
        }

        private void SaveAllNow()
        {
            if (Ref.Database != null)
            {
                Ref.Database.BeginTransaction();
                try
                {
                    Ref.EntityDataManager.FlushIntoDatabase();
                    Ref.BlockDataManager.FlushIntoDatabase();
                    Ref.Database.CommitTransaction();
                }
                catch
                {
                    Ref.Database.RollbackTransaction();
                    throw;
                }
            }

            Ref.Config?.Save(WorldPath);
        }

        public void Dispose(bool emergency)
        {
            if (!_disposed)
            {
                _disposed = true;

                if (!emergency)
                {
                    SaveAllNow();
                }

                Ref.QuickServer?.Dispose();
                Ref.Database?.Dispose();
                Locker?.Dispose();

                Core.Debug.Log("Server closed");
            }
        }
    }
}
