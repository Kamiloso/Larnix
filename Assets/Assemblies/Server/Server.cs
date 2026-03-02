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
using Larnix.Worldgen;
using Larnix.Server.Commands;
using Larnix.Blocks;
using Larnix.Server.APIs;
using Larnix.Server.Transmission;
using Larnix.Server.Configuration;
using Larnix.Server.Data.SQLite;
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
        public Action CloseServer { get; init; }

        public ushort Port => QuickServer.Port;
        public string LocalAddress => "localhost:" + Port;
        public string Authcode => QuickServer.Authcode;
        public string SocketPath => Path.Combine(WorldPath, "Socket");

        private QuickServer QuickServer => GlobRef.Get<QuickServer>();
        private ServerConfig ServerConfig => GlobRef.Get<ServerConfig>();
        private QuickSettings QuickSettings => GlobRef.Get<QuickSettings>();
        private Database Database => GlobRef.Get<Database>();
        private Receiver Receiver => GlobRef.Get<Receiver>();
        private Clock Clock => GlobRef.Get<Clock>();
        private DataSaver DataSaver => GlobRef.Get<DataSaver>();
        private Scripts Scripts => GlobRef.Get<Scripts>();

        private readonly Locker _locker;
        private bool _disposed = false;

        internal Server(ServerType type, string worldPath, RunSuggestions suggestions,
            Action closeServer, out ServerAnswer answer)
        {   
            Type = type;
            WorldPath = worldPath;
            CloseServer = closeServer;

            if (Type == ServerType.Remote)
            {
                Core.Debug.LogRaw("Starting the server...\n");
            }

            IOException MakeLockException() =>
                new IOException($"Trying to access world at {WorldPath} that is already open.");
            
            _locker = Locker.LockOrException(WorldPath, "world_locker.lock", MakeLockException);

            // --- Main singletons ---
            GlobRef.Set(this);
            GlobRef.New<PhysicsManager>();
            GlobRef.Set(new DataSaver(WorldPath));
            GlobRef.Set(new Generator(Database.GetSeed(suggestions.Seed)));
            GlobRef.Set(new Clock(Database.GetServerTick()));
            GlobRef.Set<IWorldAPI>(new WorldAPI());

            // --- Scripts ---
            GlobRef.Set(new Scripts(
                // In execution order:
                GlobRef.New<AtomicChunks>(),
                GlobRef.New<Chunks>(), // 1st
                GlobRef.New<EntityManager>(), // 2nd
                GlobRef.New<PlayerActions>(),
                GlobRef.New<BlockSender>(),
                GlobRef.New<CmdManager>()
            ));

            // --- QuickServer ---
            GlobRef.New<QuickSettings>();
            GlobRef.Set(new QuickServer(
                port: Type == ServerType.Remote ?
                    ServerConfig.Port : (ushort)0,
                userAccess: Database,
                config: QuickSettings
            ));
            GlobRef.Set(QuickServer.IUserManager);
            GlobRef.New<Receiver>();
            GlobRef.New<CmdManager>();

            // --- Configure ---
            ConfigureConsole();
            TryEstablishRelay(suggestions.RelayAddress, out var relayTask);

            // --- Finalize ---
            answer = new ServerAnswer(LocalAddress, Authcode, relayTask);
            Core.Debug.LogSuccess("Server is ready!");
        }

        private void ConfigureConsole()
        {
            void PrintBorder() => Core.Debug.LogRaw($"{new string('-', 60)}\n");

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
        }

        private bool TryEstablishRelay(string relaySuggestion, out Task<string> relayTask)
        {
            // === Global properties should be accessed from the main thread!
            /**/ QuickServer quickServer = QuickServer;
            /**/ string remoteRelayAddress = ServerConfig.Network_RelayAddress;
            // ======== //

            if (Type == ServerType.Remote)
            {
                if (ServerConfig.Network_UseRelay)
                {
                    relayTask = Task.Run(
                        () => quickServer.EstablishRelayAsync(remoteRelayAddress));
                    return true;
                }
            }
            else
            {
                if (relaySuggestion != null)
                {
                    relayTask = Task.Run(
                        () => quickServer.EstablishRelayAsync(relaySuggestion));
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
            Scripts.Tick(Clock.DeltaTime);

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
