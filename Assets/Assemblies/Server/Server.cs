using System;
using Larnix.Server.Data;
using Larnix.Core;
using System.IO;
using Larnix.GameCore.Physics;
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
using Larnix.Core.Interfaces;
using Version = Larnix.GameCore.Version;
using RunSuggestions = Larnix.Server.ServerRunner.RunSuggestions;
using ServerAnswer = Larnix.Server.ServerRunner.ServerAnswer;
using Larnix.Core.Misc;
using Larnix.GameCore.DbStructs;

namespace Larnix.Server;

internal class Server : IDisposable, ITickable
{
    public ServerType Type { get; }
    public string WorldPath { get; }
    public Action CloseServer { get; }

    public ushort Port => QuickServer.Port;
    public string LocalAddress => "localhost:" + Port;
    public string Authcode => QuickServer.Authcode;
    public string SocketPath => Path.Combine(WorldPath, "Socket");

    private IDbControl Db => GlobRef.Get<IDbControl>();
    private QuickServer QuickServer => GlobRef.Get<QuickServer>();
    private ServerConfig ServerConfig => GlobRef.Get<ServerConfig>();
    private QuickSettings QuickSettings => GlobRef.Get<QuickSettings>();
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
            Echo.LogRaw("Starting the server...\n");
        }

        IOException MakeLockException() =>
            new($"Trying to access world at {WorldPath} that is already open.");

        _locker = Locker.LockOrException(WorldPath, "world_locker.lock", MakeLockException);

        // --- Main singletons ---
        GlobRef.Set(this);
        GlobRef.New<PhysicsManager>();
        GlobRef.New<Chat>();

        GlobRef.Set(new DataSaver(WorldPath));
        long seed = Db.Values.GetOrPut("seed", () => suggestions.Seed ?? RandUtils.SecureLong());
        long serverTick = Db.Values.GetOrPut("server_tick", () => 0L);

        GlobRef.Set(new Generator(seed));
        GlobRef.Set(new Clock(serverTick));

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
            port: Type == ServerType.Remote ? ServerConfig.Port : (ushort)0,
            userAccess: (IDbUserAccess)Db.Users,
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
        Echo.LogSuccess("Server is ready!");
    }

    private void ConfigureConsole()
    {
        void PrintBorder() => Echo.LogRaw($"{new string('-', 60)}\n");

        if (Type == ServerType.Remote)
        {
            Echo.SetTitle("Larnix Server " + Version.Current);
            PrintBorder();

            Echo.LogRaw($"Socket created on port: {Port}\n");
            Echo.LogRaw($"Authcode: {Authcode}\n");
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
            Echo.Log($"Port: {Port} | Authcode: {Authcode}");
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

    public void Dispose() => Dispose(false);
    public void Dispose(bool emergency)
    {
        if (!_disposed)
        {
            _disposed = true;

            QuickServer?.Dispose();
            DataSaver?.Dispose(emergency);

            _locker?.Dispose();

            Echo.Log(emergency ?
                "Server has crashed!" :
                "Server has been closed.");
        }
    }
}
