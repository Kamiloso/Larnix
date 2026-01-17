using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
using Larnix.Socket;
using Version = Larnix.Core.Version;
using Console = Larnix.Core.Console;
using Object = System.Object;
using Larnix.Server.References;

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
        private WorldMeta? Mdata = null;

        public readonly ServerType Type;
        public readonly string WorldPath;
        public readonly string LocalAddress;
        public readonly string Authcode;
        public uint FixedFrame { get; private set; } = 0;

        private readonly LinkedList<Type> _refOrder = new();
        private readonly Dictionary<Type, Object> _registeredRefs = new();

        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private double _lastTickTime = 0;

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

                // World metadata
                Mdata = WorldMeta.ReadData(WorldPath, true);
                Mdata = new WorldMeta(Version.Current, Mdata?.nickname);
                WorldMeta.SaveData(WorldPath, (WorldMeta)Mdata, true);

                // Satelite classes
                AddRef(Config.Obtain(WorldPath));
                AddRef(new Database(WorldPath, "database.sqlite"));
                AddRef(new Worldgen.Generator(Ref<Database>().GetSeed(seedSuggestion)));

                AddRef(new QuickServer(new QuickServerConfig(
                    port: Type == ServerType.Remote ? Ref<Config>().Port : (ushort)0,
                    maxClients: Ref<Config>().MaxPlayers,
                    isLoopback: Type == ServerType.Local,
                    dataPath: Path.Combine(WorldPath, "Socket"),
                    userAPI: Ref<Database>(),
                    motd: Ref<Config>().Motd,
                    hostUser: Type == ServerType.Remote ? "Player" : (Mdata?.nickname ?? "Player"), // server owner ("Player" = detached)
                    maskIPv4: Ref<Config>().ClientIdentityPrefixSizeIPv4,
                    maskIPv6: Ref<Config>().ClientIdentityPrefixSizeIPv6
                    )));

                // Readonly classes in this file
                AddRef(new Receiver(this));
                AddRef(new Commands(this));

                // Configure for client
                LocalAddress = "localhost:" + Ref<QuickServer>().Config.Port;
                Authcode = Ref<QuickServer>().Authcode;

                // Configure console
                if (Type == ServerType.Remote)
                {
                    ConfigureConsole();
                }
                else
                {
                    Core.Debug.Log("Port: " + Ref<QuickServer>().Config.Port + " | Authcode: " + Ref<QuickServer>().Authcode);
                }

                // Try establish relay
                if (Type == ServerType.Remote)
                {
                    if (Ref<Config>().UseRelay)
                        _ = Task.Run(() => EstablishRelay(Ref<Config>().RelayAddress));
                }

                // Info success
                Core.Debug.LogSuccess("Server is ready!");
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

                bool changeSuccess = false;
                do
                {
                    Core.Debug.LogRaw("> ");
                    string password = Console.GetInputSync();
                    if (Validation.IsGoodPassword(password))
                    {
                        Ref<QuickServer>().UserManager.ChangePasswordSync(sgpNickname, password);
                        Mdata = new WorldMeta(Version.Current, "Player");
                        WorldMeta.SaveData(WorldPath, (WorldMeta)Mdata, true);
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

            // socket information
            Core.Debug.LogRaw("Socket created on port " + Ref<QuickServer>().Config.Port + "\n");
            Core.Debug.LogRaw("Authcode: " + Ref<QuickServer>().Authcode + "\n");
            Core.Debug.LogRaw(new string('-', 60) + "\n");

            // input thread start
            Console.StartInputThread();
        }

        public async Task<string> EstablishRelay(string relayAddress)
        {
            ushort? relayPort = await Ref<QuickServer>().ConfigureRelay(relayAddress);
            if (_disposed) return null;

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

        public void AddRef(Object obj)
        {
            Type type = obj.GetType();
            if (!_registeredRefs.ContainsKey(type))
            {
                _registeredRefs[type] = obj;
                _refOrder.AddLast(type);
            }
        }

        public T Ref<T>() where T : class
        {
            return _registeredRefs.TryGetValue(typeof(T), out Object obj) ? (T)obj : null;
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
            FixedFrame++;

            // Refresh QuickServer & process packets (with callbacks)

            float realTimeElapsed = GetTimeElapsed();
            Ref<QuickServer>().ServerTick(realTimeElapsed);

            // Process server logic

            for (int i = 1; i <= 5; i++)
            {
                foreach (Type t in _refOrder)
                {
                    Object obj = _registeredRefs[t];
                    if (obj is ServerSingleton singleton)
                    {
                        if (i == 1) singleton.EarlyFrameUpdate();
                        if (i == 2) singleton.TechEarlyFrameUpdate();
                        if (i == 3) singleton.FrameUpdate();
                        if (i == 4) singleton.TechLateFrameUpdate();
                        if (i == 5) singleton.LateFrameUpdate();
                    }
                }
            }

            // Cyclic actions

            if (FixedFrame % Ref<Config>().EntityBroadcastPeriodFrames == 0)
                Ref<EntityManager>().SendEntityBroadcast();

            if (FixedFrame % Ref<Config>().DataSavingPeriodFrames == 0)
                SaveAllNow();
        }

        private void SaveAllNow()
        {
            if (Ref<Database>() != null)
            {
                Ref<Database>().BeginTransaction();
                try
                {
                    Ref<EntityDataManager>().FlushIntoDatabase();
                    Ref<BlockDataManager>().FlushIntoDatabase();
                    Ref<Database>().CommitTransaction();
                }
                catch
                {
                    Ref<Database>().RollbackTransaction();
                    throw;
                }
            }

            Ref<Config>().Save(WorldPath);
        }

        public void Dispose(bool emergency)
        {
            if (!_disposed)
            {
                _disposed = true;

                if (!emergency) SaveAllNow();

                while (_refOrder.Count > 0)
                {
                    Type type = _refOrder.Last.Value;
                    _refOrder.RemoveLast();

                    Object obj = _registeredRefs[type];
                    if (obj is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }

                _registeredRefs.Clear();

                Core.Debug.Log("Server closed");
            }
        }
    }
}
