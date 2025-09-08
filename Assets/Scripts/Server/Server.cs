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
using Version = Larnix.Core.Version;

namespace Larnix.Server
{
    public class Server : MonoBehaviour
    {
        private Locker Locker = null;
        private Receiver Receiver = null;
        public MetadataSGP? Mdata = null;
        public Config ServerConfig = null;
        public Database Database = null;

        public string WorldDir { get; private set; } = "";
        private bool IsLocal, IsHost;

        private uint FixedFrame = 0;
        private float saveCycleTimer = 0f;
        private float broadcastCycleTimer = 0f;

        private void Awake()
        {
            if(WorldLoad.LoadType == WorldLoad.LoadTypes.None)
                WorldLoad.StartServer();

            WorldDir = WorldLoad.WorldDirectory;
            IsLocal = WorldLoad.LoadType == WorldLoad.LoadTypes.Local;
            IsHost = WorldLoad.IsHost;

            EarlyUpdateInjector.InjectEarlyUpdate(this.EarlyUpdate);

            Ref.Server = this;
            Ref.PhysicsManager = new PhysicsManager();

            // world metadata
            Mdata = WorldSelect.ReadMetadataSGP(WorldLoad.WorldDirectory, true);
            Mdata = new MetadataSGP(Version.Current, Mdata?.nickname);
            WorldSelect.SaveMetadataSGP(WorldLoad.WorldDirectory, (MetadataSGP)Mdata, true);

            // LOCKER --> 1
            Locker = Locker.TryLock(WorldDir, "world_locker.lock");
            if (Locker == null)
                throw new Exception("Trying to access world that is already open.");

            // CONFIG --> 2
            ServerConfig = Config.Obtain(WorldDir);

            // DATABASE --> 3
            Database = new Database(WorldDir, "database.sqlite");

            // Generator (3.5)
            Ref.Generator = new Worldgen.Generator(Database.GetSeed(WorldLoad.SeedSuggestion));

            // SERVER --> 4
            Ref.QuickServer = new QuickServer(
                IsHost ? ServerConfig.Port : (ushort)0,
                ServerConfig.MaxPlayers,
                !IsHost,
                Path.Combine(WorldDir, "Socket"),
                Version.Current.ID,
                Validation.IsGoodText<String256>(ServerConfig.Motd) ? ServerConfig.Motd : "Wrong motd format :(",
                Mdata?.nickname ?? "Player"
            );

            Ref.QuickServer.ConfigureMasks(
                ServerConfig.ClientIdentityPrefixSizeIPv4,
                ServerConfig.ClientIdentityPrefixSizeIPv6
                );

            if (!IsLocal)
                Ref.QuickServer.ReserveNickname("Player");

            Receiver = new Receiver(Ref.QuickServer);

            // Configure for client
            WorldLoad.Address = "localhost:" + Ref.QuickServer.Port;
            WorldLoad.Authcode = Ref.QuickServer.Authcode;
        }

        private void Start()
        {
            // title set
            Console.SetTitle("Larnix Server " + Version.Current);
            Larnix.Debug.LogRawConsole(new string('-', 60) + "\n");

            // force change default password
            if (!IsLocal && Mdata?.nickname != "Player")
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
                        WorldSelect.SaveMetadataSGP(WorldLoad.WorldDirectory, (MetadataSGP)Mdata, true);
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
            if (!IsLocal) Console.StartInputThread();
            Larnix.Debug.LogSuccess("Server is ready!");
        }

        public uint GetFixedFrame()
        {
            return FixedFrame;
        }

        private void FixedUpdate()
        {
            FixedFrame++;

            Ref.ChunkLoading.FromFixedUpdate(); // FIX-1
            Ref.EntityManager.FromFixedUpdate(); // FIX-2
        }

        private void EarlyUpdate() // Executes BEFORE default Update() time
        {
            Ref.QuickServer.ServerTick(Time.deltaTime);

            Ref.ChunkLoading.FromEarlyUpdate(); // 1
            Ref.EntityManager.FromEarlyUpdate(); // 2

            if (IsLocal && Client.Ref.Debug.SpawnWildpigsWithZ) // WILDPIG TEST SPAWNING
            {
                if (Input.GetKeyDown(KeyCode.Z))
                {
                    Ref.EntityManager.SummonEntity(new EntityData
                    {
                        ID = EntityID.Wildpig,
                        Position = Ref.EntityManager.GetPlayerController(Mdata?.nickname).EntityData.Position,
                    });
                }
            }

            Commands.ExecuteFrame();
        }

        private void LateUpdate()
        {
            Ref.BlockSender.BroadcastInfo(); // must be in LateUpdate()

            broadcastCycleTimer += Time.deltaTime;
            if(ServerConfig.EntityBroadcastPeriod > 0f && broadcastCycleTimer > ServerConfig.EntityBroadcastPeriod)
            {
                broadcastCycleTimer %= ServerConfig.EntityBroadcastPeriod;
                Ref.EntityManager.SendEntityBroadcast(); // must be in LateUpdate()
            }

            saveCycleTimer += Time.deltaTime;
            if (ServerConfig.DataSavingPeriod > 0f && saveCycleTimer > ServerConfig.DataSavingPeriod)
            {
                saveCycleTimer %= ServerConfig.DataSavingPeriod;
                SaveAllNow();
            }
        }

        public void CloseServer()
        {
            // when the main player is kicked he will return to menu and the local server will close
            if (!IsLocal) Application.Quit();
            else Ref.QuickServer.FinishConnection(Mdata?.nickname);
        }

        public void SaveAllNow()
        {
            Database.BeginTransaction();
            try
            {
                Ref.EntityDataManager.FlushIntoDatabase();
                Ref.BlockDataManager.FlushIntoDatabase();
                Database.CommitTransaction();
            }
            catch
            {
                Database.RollbackTransaction();
                throw;
            }

            ServerConfig.Save(WorldDir);
        }

        private void OnDestroy()
        {
            SaveAllNow();

            // 4 --> SERVER
            Ref.QuickServer?.Dispose();

            // 3 ---> DATABASE
            Database?.Dispose();

            // 2 ---> CONFIG
            // (non-disposable)

            // 1 --> LOCKER
            Locker?.Dispose();

            EarlyUpdateInjector.ClearEarlyUpdate();
            Larnix.Debug.Log("Server close");
        }
    }
}
