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

namespace Larnix.Server
{
    internal class Server
    {
        public ServerType Type { get; init; }
        public string WorldPath { get; init; }

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

        private QuickServer QuickServer => Ref.QuickServer;
        private Locker Locker => Ref.Locker;
        private Config Config => Ref.Config;
        private Database Database => Ref.Database;
        private EntityManager EntityManager => Ref.EntityManager;
        private PlayerManager PlayerManager => Ref.PlayerManager;
        private EntityDataManager EntityDataManager => Ref.EntityDataManager;
        private BlockDataManager BlockDataManager => Ref.BlockDataManager;
        private UserManager UserManager => Ref.UserManager;
        private Receiver Receiver => Ref.Receiver;

        private double _lastTickTime = 0;
        private float _totalTimeElapsed = 0f;

        public float RealDeltaTime => _totalTimeElapsed > 0f ?
            _totalTimeElapsed : Common.FIXED_TIME;
        public float TPS => _totalTimeElapsed > 0f ?
            (1f / _totalTimeElapsed) : 0f;

        private bool _disposed = false;

        public Server(ServerType type, string worldPath, long? seedSuggestion, Action closeServer)
        {
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

            // --- [ MAIN SINGLETONS ] ---
            GlobRef.Set(this);
            GlobRef.Set(new PhysicsManager());
            GlobRef.Set<IWorldAPI>(new WorldAPI());

            // --- [ SCRIPTS ] ---
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

            // --- [ SATELITE CLASSES ] ---
            GlobRef.Set(Config.Obtain(WorldPath));
            GlobRef.Set(new Database(WorldPath, "database.sqlite"));
            GlobRef.Set(new Generator(Database.GetSeed(seedSuggestion)));

            // --- Server tick obtain ---
            ServerTick = Database.GetServerTick();

            // --- World metadata ---
            _mdata = WorldMeta.ReadFromFolder(WorldPath);
            _mdata = new WorldMeta(Version.Current, _mdata.Nickname);
            WorldMeta.SaveToFolder(WorldPath, _mdata);

            // --- QuickServer ---
            _quickServer = new QuickServer(
                new QuickConfig(
                    port: Type == ServerType.Remote ?
                        Config.Port : (ushort)0,

                    maxClients: Config.MaxPlayers,
                    isLoopback: Type == ServerType.Local,
                    dataPath: Path.Combine(WorldPath, "Socket"),
                    userAPI: Database,
                    motd: (String256)Config.Motd,

                    hostUser: Type == ServerType.Remote ?
                        (String32)Common.LOOPBACK_ONLY_NICKNAME :
                        (String32)_mdata.Nickname,
                    
                    maskIPv4: Config.ClientIdentityPrefixSizeIPv4,
                    maskIPv6: Config.ClientIdentityPrefixSizeIPv6,
                    allowRegistration: Config.AllowRegistration
                )
            );

            // --- [ SERVER SINGLETONS ] ---
            GlobRef.Set(_quickServer);
            GlobRef.Set(_quickServer.UserManager);
            GlobRef.Set(new Receiver());
            GlobRef.Set(new CmdManager());

            // --- Configure console ---
            if (Type == ServerType.Remote)
            {
                ConfigureConsole();
            }
            else
            {
                Core.Debug.Log($"Port: {Port} | Authcode: {Authcode}");
            }

            // --- Try establish relay ---
            if (Type == ServerType.Remote)
            {
                if (Config.UseRelay)
                    _ = Task.Run(() => EstablishRelayAsync(Config.RelayAddress));
            }

            // --- Info success ---
            Core.Debug.LogSuccess("Server is ready!");
        }

        private void ConfigureConsole()
        {
            const string BORDER_STR = "------------------------------------------------------------";

            // Title set
            Console.SetTitle("Larnix Server " + Version.Current);
            Core.Debug.LogRaw(BORDER_STR + "\n");

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
                Core.Debug.LogRaw(BORDER_STR + "\n");
            }

            // Socket information
            Core.Debug.LogRaw("Socket created on port " + _quickServer.Config.Port + "\n");
            Core.Debug.LogRaw("Authcode: " + _quickServer.Authcode + "\n");
            Core.Debug.LogRaw(BORDER_STR + "\n");

            // Input thread start
            Console.EnableInputThread();
        }

        public async Task<string> EstablishRelayAsync(string relayAddress)
        {
            ushort? relayPort = await _quickServer.ConfigureRelayAsync(relayAddress);

            if (relayPort != null)
            {
                var uri = new UriBuilder("udp://" + relayAddress) { Port = relayPort.Value };
                string address = uri.ToString().Replace("udp://", "").Replace("/", "");

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
