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
using Larnix.Core.References;
using Version = Larnix.Core.Version;
using Console = Larnix.Core.Console;
using Object = System.Object;

namespace Larnix.Server
{
    internal class Server : RefRoot
    {
        public ServerType Type { get; init; }
        public string WorldPath { get; init; }
        public string LocalAddress { get; init; }
        public string Authcode { get; init; }

        public long ServerTick { get; private set; } = 0;
        public uint FixedFrame { get; private set; } = 0;

        private WorldMeta _mdata;
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private double _lastTickTime = 0;

        private readonly QuickServer _quickServer;

        private Config Config => Ref<Config>();
        private Database Database => Ref<Database>();
        private EntityManager EntityManager => Ref<EntityManager>();
        private PlayerManager PlayerManager => Ref<PlayerManager>();
        private EntityDataManager EntityDataManager => Ref<EntityDataManager>();
        private BlockDataManager BlockDataManager => Ref<BlockDataManager>();
        private UserManager UserManager => Ref<UserManager>();
        private Receiver Receiver => Ref<Receiver>();

        private float _totalTimeElapsed = 0f;
        public float RealDeltaTime => _totalTimeElapsed;

        public readonly Action CloseServer;
        private bool _disposed = false;

        public Server(ServerType type, string worldPath, long? seedSuggestion, Action closeServer)
        {
            Type = type;
            WorldPath = worldPath;
            CloseServer = closeServer;

            // Self
            AddRef(this);

            // Other
            AddRef(new PhysicsManager());

            // Scripts
            AddRef(new ChunkLoading(this)); // must be 1st (execution order)
            AddRef(new EntityManager(this)); // must be 2nd (execution order)
            AddRef(new WorldAPI(this));
            AddRef(new EntityDataManager(this));
            AddRef(new PlayerManager(this));
            AddRef(new BlockDataManager(this));
            AddRef(new BlockSender(this));
            AddRef(new Commands(this));

            // Try to lock world
            Locker locker = Locker.TryLock(WorldPath, "world_locker.lock");
            if (locker != null)
            {
                // Establish locker
                AddRef(locker);

                // Satelite classes
                AddRef(Config.Obtain(WorldPath));
                AddRef(new Database(WorldPath, "database.sqlite"));
                AddRef(new Generator(Database.GetSeed(seedSuggestion)));

                // Server tick obtain
                ServerTick = Database.GetServerTick();

                // World metadata
                _mdata = WorldMeta.ReadData(WorldPath, true);
                _mdata = new WorldMeta(Version.Current, _mdata.nickname);
                WorldMeta.SaveData(WorldPath, _mdata, true);

                // QuickServer
                _quickServer = new QuickServer(new QuickConfig(
                    port: Type == ServerType.Remote ? Config.Port : (ushort)0,
                    maxClients: Config.MaxPlayers,
                    isLoopback: Type == ServerType.Local,
                    dataPath: Path.Combine(WorldPath, "Socket"),
                    userAPI: Database,
                    motd: Config.Motd,
                    hostUser: Type == ServerType.Remote ? "Player" : _mdata.nickname, // server owner ("Player" => detached)
                    maskIPv4: Config.ClientIdentityPrefixSizeIPv4,
                    maskIPv6: Config.ClientIdentityPrefixSizeIPv6
                    ));
                AddRef(_quickServer);

                // Around-server managers
                AddRef(_quickServer.UserManager);
                AddRef(new Receiver(this));
                AddRef(new Commands(this));

                // Configure data for client
                LocalAddress = "localhost:" + _quickServer.Config.Port;
                Authcode = _quickServer.Authcode;

                // Configure console
                if (Type == ServerType.Remote)
                {
                    ConfigureConsole();
                }
                else
                {
                    Core.Debug.Log("Port: " + _quickServer.Config.Port + " | Authcode: " + _quickServer.Authcode);
                }

                // Try establish relay
                if (Type == ServerType.Remote)
                {
                    if (Config.UseRelay)
                        _ = Task.Run(() => EstablishRelayAsync(Config.RelayAddress));
                }

                // Info success
                Core.Debug.LogSuccess("Server is ready!");
            }
            else throw new Exception("Trying to access world that is already open.");
        }

        private void ConfigureConsole()
        {
            // Title set
            Console.SetTitle("Larnix Server " + Version.Current);
            Core.Debug.LogRaw(new string('-', 60) + "\n");

            // Force change default password
            if (_mdata.nickname != "Player")
            {
                Core.Debug.LogRaw("This world was previously in use by " + _mdata.nickname + ".\n");
                Core.Debug.LogRaw("Choose a password for this player to start the server.\n");

                bool changeSuccess = false;
                do
                {
                    Core.Debug.LogRaw("> ");
                    string password = Console.GetInputSync();
                    if (Validation.IsGoodPassword(password))
                    {
                        UserManager.ChangePasswordSync(_mdata.nickname, password);
                        _mdata = new WorldMeta(Version.Current, "Player");
                        WorldMeta.SaveData(WorldPath, _mdata, true);
                        changeSuccess = true;
                    }
                    else
                    {
                        Core.Debug.LogRaw("Password should be 7-32 characters and not end with NULL (0x00).\n");
                    }
                    
                } while (!changeSuccess);

                Core.Debug.LogRaw("Password changed.\n");
                Core.Debug.LogRaw(new string('-', 60) + "\n");
            }

            // Socket information
            Core.Debug.LogRaw("Socket created on port " + _quickServer.Config.Port + "\n");
            Core.Debug.LogRaw("Authcode: " + _quickServer.Authcode + "\n");
            Core.Debug.LogRaw(new string('-', 60) + "\n");

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

        private float GetTimeElapsed()
        {
            double currentTime = _stopwatch.Elapsed.TotalSeconds;
            double elapsedTime = currentTime - _lastTickTime;
            _lastTickTime = currentTime;

            return (float)elapsedTime;
        }

        public void TickFixed()
        {
            ServerTick++;
            FixedFrame++;

            // Tick technical singletons

            _totalTimeElapsed = GetTimeElapsed();
            Receiver.Tick(_totalTimeElapsed); // for limits
            _quickServer.ServerTick(_totalTimeElapsed); // refresh & process packets

            // Process server logic

            for (int i = 1; i <= 5; i++)
            {
                foreach (Object obj in TakeRefSnapshot())
                {
                    if (obj is Singleton singleton)
                    {
                        if (i == 1) singleton.EarlyFrameUpdate();
                        if (i == 2) singleton.PostEarlyFrameUpdate();
                        if (i == 3) singleton.FrameUpdate();
                        if (i == 4) singleton.LateFrameUpdate();
                        if (i == 5) singleton.PostLateFrameUpdate();
                    }
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
                if (!emergency) SaveAllNow();

                LinkedList<Object> snapshot = TakeRefSnapshot();
                while (snapshot.Count > 0)
                {
                    Object obj = snapshot.Last.Value;
                    if (obj is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }

                    snapshot.RemoveLast();
                }

                Core.Debug.Log("Server closed");
            }
        }
    }
}
