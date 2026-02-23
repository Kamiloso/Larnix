using System.Collections;
using System.Collections.Generic;
using System;
using Larnix.Server.Data;
using Larnix.Core;
using System.IO;
using Larnix.Core.Physics;
using Larnix.Server.Entities;
using Larnix.Server.Terrain;
using System.Threading.Tasks;
using System.Diagnostics;
using Larnix.Core.Files;
using Larnix.Socket.Backend;
using Larnix.Core.Utils;
using Larnix.Worldgen;
using Larnix.Server.Commands;
using Version = Larnix.Core.Version;
using Console = Larnix.Core.Console;
using Larnix.Blocks;
using RunSuggestions = Larnix.Server.ServerRunner.RunSuggestions;
using ServerAnswer = Larnix.Server.ServerRunner.ServerAnswer;

namespace Larnix.Server
{
    internal class Server
    {
        public ServerType Type { get; init; }
        public string WorldPath { get; init; }
        public String32 HostUser { get; init; }

        public ushort Port => _quickServer.Config.Port;
        public string LocalAddress => "localhost:" + Port;
        public string Authcode => _quickServer.Authcode;

        public long ServerTick { get; private set; }
        public uint FixedFrame { get; private set; }

        private readonly QuickServer _quickServer;
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private readonly IScript[] _scripts;
        public readonly Action CloseServer;

        private WorldMeta _mdata;

        private QuickServer QuickServer => GlobRef.Get<QuickServer>();
        private Locker Locker => GlobRef.Get<Locker>();
        private Config Config => GlobRef.Get<Config>();
        private Database Database => GlobRef.Get<Database>();
        private EntityManager EntityManager => GlobRef.Get<EntityManager>();
        private PlayerManager PlayerManager => GlobRef.Get<PlayerManager>();
        private EntityDataManager EntityDataManager => GlobRef.Get<EntityDataManager>();
        private BlockDataManager BlockDataManager => GlobRef.Get<BlockDataManager>();
        private UserManager UserManager => GlobRef.Get<UserManager>();
        private Receiver Receiver => GlobRef.Get<Receiver>();

        private double _lastTickTime = 0;
        private float _totalTimeElapsed = 0f;

        public float RealDeltaTime => _totalTimeElapsed > 0f ?
            _totalTimeElapsed : Common.FIXED_TIME;
        public float TPS => _totalTimeElapsed > 0f ?
            (1f / _totalTimeElapsed) : 0f;

        private bool _disposed = false;

        public Server(ServerType type, string worldPath, RunSuggestions suggestions,
            Action closeServer, out ServerAnswer answer)
        {
            if (GlobRef.Get<Server>() != null)
            {
                throw new InvalidOperationException("Cannot create server instance using constructor. " +
                    "Use ServerRunner class instead.");
            }

            // --- Constants ---
            Type = type;
            WorldPath = worldPath;
            CloseServer = closeServer;

            // --- Try lock the world ---
            Locker locker = Locker.TryLock(WorldPath, "world_locker.lock");
            if (locker == null)
            {
                throw new InvalidOperationException("Trying to access world that is already open.");
            }
            GlobRef.Set(locker);

            // --- Main singletons ---
            GlobRef.Set(this);
            GlobRef.Set(new PhysicsManager());
            GlobRef.Set(Config.Obtain(WorldPath));
            GlobRef.Set(new Database(WorldPath, "database.sqlite"));
            GlobRef.Set(new Generator(Database.GetSeed(suggestions.Seed)));

            // -- APIs ---
            GlobRef.Set<IWorldAPI>(new WorldAPI());

            // --- Scripts ---
            _scripts = new IScript[] // in execution order
            {
                GlobRef.Set(new AtomicChunks()),
                GlobRef.Set(new Chunks()), // 1st
                GlobRef.Set(new EntityManager()), // 2nd
                GlobRef.Set(new EntityDataManager()),
                GlobRef.Set(new PlayerManager()),
                GlobRef.Set(new BlockDataManager()),
                GlobRef.Set(new BlockSender()),
                GlobRef.Set(new CmdManager())
            };

            // --- Server tick obtain ---
            ServerTick = Database.GetServerTick();

            // --- World metadata ---
            _mdata = WorldMeta.ReadFromFolder(WorldPath);
            _mdata = new WorldMeta(Version.Current, _mdata.Nickname);
            WorldMeta.SaveToFolder(WorldPath, _mdata);

            // --- Host user ---
            HostUser = Type == ServerType.Remote ?
                (String32)Common.LOOPBACK_ONLY_NICKNAME :
                (String32)_mdata.Nickname;

            // --- QuickServer ---
            _quickServer = new QuickServer(
                new QuickConfig(
                    port: Type == ServerType.Remote ? Config.Port : (ushort)0,
                    maxClients: Config.MaxPlayers,
                    isLoopback: Type == ServerType.Local,
                    dataPath: Path.Combine(WorldPath, "Socket"),
                    userAPI: Database,
                    motd: (String256)Config.Motd,
                    hostUser: HostUser,
                    
                    // socket settings, will be moved in the future
                    maskIPv4: Config.ClientIdentityPrefixSizeIPv4,
                    maskIPv6: Config.ClientIdentityPrefixSizeIPv6,
                    allowRegistration: Config.AllowRegistration
                )
            );

            // --- Server singletons ---
            GlobRef.Set(_quickServer);
            GlobRef.Set(_quickServer.UserManager);
            GlobRef.Set(new Receiver());
            GlobRef.Set(new CmdManager());

            // Configure console
            if (Type == ServerType.Remote)
            {
                ConfigureConsole();
            }
            else Core.Debug.Log($"Port: {Port} | Authcode: {Authcode}");

            // Try establish relay
            Task<string> relayTask = RelayEstablishment(
                suggestions.RelayAddress
            );

            // Finalize
            Core.Debug.LogSuccess("Server is ready!");
            answer = new ServerAnswer(LocalAddress, Authcode, relayTask);
        }

        private void ConfigureConsole()
        {
            // Print border function
            void PrintBorder() =>
                Core.Debug.LogRaw("------------------------------------------------------------\n");

            // Title set
            Console.SetTitle("Larnix Server " + Version.Current);
            PrintBorder();

            // Force change default password
            if (_mdata.Nickname != Common.LOOPBACK_ONLY_NICKNAME)
            {
                Core.Debug.LogRaw("This world was previously in use by " + _mdata.Nickname + ".\n");
                Core.Debug.LogRaw("Choose a password for this player to start the server.\n");

                bool changeSuccess = false;
                do
                {
                    Core.Debug.LogRaw("> ");
                    string password = Console.GetInputSync();

                    if (Validation.IsGoodPassword(password))
                    {
                        UserManager.ChangePasswordSync(_mdata.Nickname, password);
                        _mdata = new WorldMeta(Version.Current, Common.LOOPBACK_ONLY_NICKNAME);
                        WorldMeta.SaveToFolder(WorldPath, _mdata);
                        changeSuccess = true;
                    }
                    else
                    {
                        Core.Debug.LogRaw(Validation.WrongPasswordInfo + "\n");
                    }
                    
                } while (!changeSuccess);

                Core.Debug.LogRaw("Password changed.\n");
                PrintBorder();
            }

            // Socket information
            Core.Debug.LogRaw("Socket created on port " + _quickServer.Config.Port + "\n");
            Core.Debug.LogRaw("Authcode: " + _quickServer.Authcode + "\n");
            PrintBorder();

            // Input thread start
            Console.EnableInputThread();
        }

        private Task<string> RelayEstablishment(string relaySuggestion)
        {
            if (Type == ServerType.Remote)
            {
                if (Config.UseRelay)
                    return Task.Run(() => Establish(Config.RelayAddress));
            }
            else
            {
                if (relaySuggestion != null)
                    return Task.Run(() => Establish(relaySuggestion));
            }
            return null;

            async Task<string> Establish(string relayAddress)
            {
                ushort? relayPort = await _quickServer.ConfigureRelayAsync(relayAddress);
                
                if (relayPort != null)
                {
                    string address = Common.FormatUdpAddress(relayAddress, relayPort.Value);
                    Core.Debug.LogSuccess("Connected to relay!");
                    Core.Debug.Log("Address: " + address);
                    return address;
                }
                else
                {
                    Core.Debug.LogWarning("Cannot connect to relay!");
                    return null;
                }
            }
        }

        public void TickFixed()
        {
            ServerTick++;
            FixedFrame++;

            // Tick technical singletons
            float GetTimeElapsed()
            {
                double currentTime = _stopwatch.Elapsed.TotalSeconds;
                double elapsedTime = currentTime - _lastTickTime;
                _lastTickTime = currentTime;

                return (float)elapsedTime;
            };

            _totalTimeElapsed = GetTimeElapsed();
            Receiver.Tick(_totalTimeElapsed); // for limits
            _quickServer.ServerTick(_totalTimeElapsed); // refresh & process packets

            // Process server logic
            for (int i = 1; i <= 5; i++)
            {
                foreach (IScript singleton in _scripts)
                {
                    if (i == 1) singleton.EarlyFrameUpdate();
                    if (i == 2) singleton.PostEarlyFrameUpdate();
                    if (i == 3) singleton.FrameUpdate();
                    if (i == 4) singleton.LateFrameUpdate();
                    if (i == 5) singleton.PostLateFrameUpdate();
                }
            }

            // Cyclic actions
            if (FixedFrame % Config.EntityBroadcastPeriodFrames == 0)
            {
                EntityManager.SendEntityBroadcast();
                PlayerManager.SendFrameInfoBroadcast();
            }

            if (FixedFrame % Config.DataSavingPeriodFrames == 0)
            {
                SaveAllNow();
            }
        }

        private void SaveAllNow()
        {
            if (Database != null)
            {
                Database.BeginTransaction();
                try
                {
                    EntityDataManager.FlushIntoDatabase();
                    BlockDataManager.FlushIntoDatabase();
                    Database.SetServerTick(ServerTick);
                    Database.CommitTransaction();
                }
                catch
                {
                    Database.RollbackTransaction();
                    throw;
                }
            }

            Config.Save(WorldPath);
        }

        public void Dispose(bool emergency)
        {
            if (!_disposed)
            {
                _disposed = true;
                
                if (!emergency)
                {
                    SaveAllNow();
                    Core.Debug.Log("Data has been saved.");
                }

                QuickServer?.Dispose();
                Database?.Dispose();
                Locker?.Dispose();

                Core.Debug.Log(emergency ?
                    "Server has crashed!" :
                    "Server has been closed.");
            }
        }
    }
}
