using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using QuickNet;
using Larnix.Server.Data;
using Larnix.Entities;
using Larnix.Menu.Worlds;
using QuickNet.Processing;
using System.IO;
using QuickNet.Backend;
using Larnix.Core.Physics;
using Larnix.Server.Entities;
using Larnix.Server.Terrain;
using Version = Larnix.Core.Version;

namespace Larnix.Server
{
    public class Server : IDisposable
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

        public Server(ServerType type, string worldPath, long? seedSuggestion)
        {
            Type = type;
            WorldPath = worldPath;

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
                // world metadata
                Mdata = WorldSelect.ReadMetadataSGP(WorldPath, true);
                Mdata = new MetadataSGP(Version.Current, Mdata?.nickname);
                WorldSelect.SaveMetadataSGP(WorldPath, (MetadataSGP)Mdata, true);

                // satelite classes
                Ref.Config = Config.Obtain(WorldPath);
                Ref.Database = new Database(WorldPath, "database.sqlite");
                Ref.Generator = new Worldgen.Generator(Ref.Database.GetSeed(seedSuggestion));

                // server configuration
                Ref.QuickServer = new QuickServer(
                    Type == ServerType.Remote ? Ref.Config.Port : (ushort)0,
                    Ref.Config.MaxPlayers,
                    Type == ServerType.Local,
                    Path.Combine(WorldPath, "Socket"),
                    Version.Current.ID,
                    Validation.IsGoodText<String256>(Ref.Config.Motd) ? Ref.Config.Motd : "Wrong motd format :(",
                    Mdata?.nickname ?? "Player"
                );

                Ref.QuickServer.ConfigureMasks(
                    Ref.Config.ClientIdentityPrefixSizeIPv4,
                    Ref.Config.ClientIdentityPrefixSizeIPv6
                    );

                Receiver = new Receiver(Ref.QuickServer);
                Commands = new Commands();

                // Configure for client
                LocalAddress = "localhost:" + Ref.QuickServer.Port;
                Authcode = Ref.QuickServer.Authcode;

                // title set
                Console.SetTitle("Larnix Server " + Version.Current);
                Larnix.Debug.LogRawConsole(new string('-', 60) + "\n");

                // force change default password
                if (Type == ServerType.Remote && Mdata?.nickname != "Player")
                {
                    string sgpNickname = Mdata?.nickname;

                    Larnix.Debug.LogRawConsole("This world was previously in use by " + sgpNickname + ".\n");
                    Larnix.Debug.LogRawConsole("Choose a password for this player to start the server.\n");

                password_ask:
                    {
                        Larnix.Debug.LogRawConsole("> ");
                        Larnix.Debug.FlushLogs(); // allowed temporarily
                        string input = Console.GetInputSync();
                        if (Validation.IsGoodPassword(input))
                        {
                            Ref.QuickServer.UserManager.ChangePassword(sgpNickname, Hasher.HashPassword(input));
                            Mdata = new MetadataSGP(Version.Current, "Player");
                            WorldSelect.SaveMetadataSGP(WorldPath, (MetadataSGP)Mdata, true);
                        }
                        else
                        {
                            Larnix.Debug.LogRawConsole("Password should be 7-32 characters and not end with NULL (0x00).\n");
                            goto password_ask;
                        }
                    }

                    Larnix.Debug.LogRawConsole("Password changed.\n");
                    Larnix.Debug.LogRawConsole(new string('-', 60) + "\n");
                }

                // socket information
                Larnix.Debug.LogNoDate("Socket created on port " + Ref.QuickServer.Port);
                Larnix.Debug.LogNoDate("Authcode: " + Ref.QuickServer.Authcode);
                Larnix.Debug.LogRawConsole(new string('-', 60) + "\n");

                // additional initialization
                if (Type == ServerType.Remote) Console.StartInputThread();
                Larnix.Debug.LogSuccess("Server is ready!");
            }
            else throw new Exception("Trying to access world that is already open.");
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

        public void CloseServer()
        {
            ServerInstancer.Instance.StopFlag = true;
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

        public void Dispose()
        {
            SaveAllNow();

            Ref.QuickServer?.Dispose();
            Ref.Database?.Dispose();
            Locker?.Dispose();

            Larnix.Debug.Log("Server close");
        }
    }
}
