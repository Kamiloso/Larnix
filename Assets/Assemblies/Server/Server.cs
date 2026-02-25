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
using Larnix.Core.Files;
using Larnix.Socket.Backend;
using Larnix.Core.Utils;
using Larnix.Worldgen;
using Larnix.Server.Commands;
using Larnix.Blocks;
using Larnix.Server.APIs;
using Larnix.Server.Transmission;
using Version = Larnix.Core.Version;
using Console = Larnix.Core.Console;
using RunSuggestions = Larnix.Server.ServerRunner.RunSuggestions;
using ServerAnswer = Larnix.Server.ServerRunner.ServerAnswer;

namespace Larnix.Server
{
    internal class Server : IDisposable2, ITickable
    {
        public ServerType Type { get; init; }
        public string WorldPath { get; init; }

        public ushort Port => QuickServer.Config.Port;
        public string LocalAddress => "localhost:" + Port;
        public string Authcode => QuickServer.Authcode;

        private readonly Locker _locker;
        private readonly IScript[] _scripts;
        public readonly Action CloseServer;

        private QuickServer QuickServer => GlobRef.Get<QuickServer>();
        private Config Config => GlobRef.Get<Config>();
        private Database Database => GlobRef.Get<Database>();
        private Receiver Receiver => GlobRef.Get<Receiver>();
        private Clock Clock => GlobRef.Get<Clock>();
        private DataSaver DataSaver => GlobRef.Get<DataSaver>();

        private bool _startExecuted = false;
        private bool _disposed = false;

        private static void PrintBorder() =>
            Core.Debug.LogRaw("------------------------------------------------------------\n");
        
        internal Server(ServerType type, string worldPath, RunSuggestions suggestions,
            Action closeServer, out ServerAnswer answer)
        {   
            Type = type;
            WorldPath = worldPath;
            CloseServer = closeServer;

            if (Type == ServerType.Remote)
                Core.Debug.LogRaw("Starting the server...\n");

            _locker = Locker.LockOrException(WorldPath, "world_locker.lock",
                () => new IOException($"Trying to access world at {WorldPath} that is already open."));

            // --- Main singletons ---
            GlobRef.Set(this);
            GlobRef.Set(new PhysicsManager());
            GlobRef.Set(new DataSaver(WorldPath));
            GlobRef.Set(new Generator(Database.GetSeed(suggestions.Seed)));
            GlobRef.Set(new Clock(Database.GetServerTick()));

            // -- APIs ---
            GlobRef.Set<IWorldAPI>(new WorldAPI());

            // --- Scripts ---
            _scripts = new IScript[] // in execution order
            {
                GlobRef.Set(new AtomicChunks()),
                GlobRef.Set(new Chunks()), // 1st
                GlobRef.Set(new EntityManager()), // 2nd
                GlobRef.Set(new PlayerActions()),
                GlobRef.Set(new BlockSender()),
                GlobRef.Set(new CmdManager())
            };

            // --- QuickServer ---
            GlobRef.Set(new QuickServer(
                new QuickConfig(
                    port: Type == ServerType.Remote ?
                        Config.Port : (ushort)0,
                    
                    maxClients: Config.MaxPlayers,
                    isLoopback: Type == ServerType.Local,
                    dataPath: Path.Combine(WorldPath, "Socket"),
                    userAPI: Database,
                    motd: (String256)Config.Motd,

                    hostUser: Type == ServerType.Remote ?
                        (String32)Common.ReservedNickname :
                        DataSaver.HostNickname,
                    
                    // socket settings, will be moved in the future
                    maskIPv4: Config.ClientIdentityPrefixSizeIPv4,
                    maskIPv6: Config.ClientIdentityPrefixSizeIPv6,
                    allowRegistration: Config.AllowRegistration
                )
            ));
            GlobRef.Set(QuickServer.UserManager);
            GlobRef.Set(new Receiver());
            GlobRef.Set(new CmdManager());

            // --- Configure console ---
            if (Type == ServerType.Remote)
            {
                Console.SetTitle("Larnix Server " + Version.Current);
                PrintBorder();

                Core.Debug.LogRaw($"Socket created on port: {Port}\n");
                Core.Debug.LogRaw($"Authcode: {Authcode}\n");
                PrintBorder();

                // --- Check if the world is detached ---
                if (Type == ServerType.Remote)
                {
                    if (DataSaver.EnsureDetachedServer())
                        PrintBorder();
                }
            }
            else
            {
                Core.Debug.Log($"Port: {Port} | Authcode: {Authcode}");
            }

            // --- Try establish relay ---
            TryEstablishRelay(suggestions.RelayAddress, out var relayTask);
            answer = new ServerAnswer(LocalAddress, Authcode, relayTask);

            // --- Finalize ---
            Core.Debug.LogSuccess("Server is ready!");
        }

        private bool TryEstablishRelay(string relaySuggestion, out Task<string> relayTask)
        {
            // should be accessed from the main thread
            QuickServer quickServer = QuickServer;

            if (Type == ServerType.Remote)
            {
                if (Config.UseRelay)
                {
                    relayTask = Task.Run(() => quickServer.EstablishRelayAsync(Config.RelayAddress));
                    return true;
                }
            }
            else
            {
                if (relaySuggestion != null)
                {
                    relayTask = Task.Run(() => quickServer.EstablishRelayAsync(relaySuggestion));
                    return true;
                }
            }

            relayTask = null;
            return false;
        }

        public void Tick(float deltaTime)
        {
            Clock.Tick(deltaTime);
            
            // Server ticks
            Receiver.Tick(Clock.DeltaTime); // for limits
            QuickServer.Tick(Clock.DeltaTime); // refresh & process packets

            // Process server logic
            for (int i = 0; i <= 5; i++)
            {
                foreach (IScript singleton in _scripts)
                {
                    if (i == 0 && !_startExecuted)
                    {
                        singleton.Start();
                    }

                    if (i == 1) singleton.EarlyFrameUpdate();
                    if (i == 2) singleton.PostEarlyFrameUpdate();
                    if (i == 3) singleton.FrameUpdate();
                    if (i == 4) singleton.LateFrameUpdate();
                    if (i == 5) singleton.PostLateFrameUpdate();
                }
            }
            _startExecuted = true;

            // Tick data saving
            DataSaver.Tick(Clock.DeltaTime);
        }

        public void Dispose(bool emergency)
        {
            if (!_disposed)
            {
                _disposed = true;

                QuickServer?.Dispose();
                DataSaver?.Dispose(emergency);
                
                _locker?.Dispose();

                Core.Debug.Log(emergency ?
                    "Server has crashed!" :
                    "Server has been closed.");
            }
        }
    }
}
