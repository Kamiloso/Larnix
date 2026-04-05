#nullable enable
using Larnix.Core;
using Larnix.Core.Files;
using Larnix.Core.Utils;
using Larnix.Model.Blocks;
using Larnix.Model.Json;
using Larnix.Model.Physics;
using Larnix.Model.Utils;
using Larnix.Model.Worldgen;
using Larnix.Server.Commands;
using Larnix.Server.Data;
using Larnix.Model.Database.Connection;
using Larnix.Server.Terrain;
using Larnix.Server.Transmission;
using Larnix.Socket.Backend;
using System;
using System.IO;
using System.Threading.Tasks;
using Larnix.Model.Database;
using RunSuggestions = Larnix.Server.ServerRunner.RunSuggestions;
using ServerAnswer = Larnix.Server.ServerRunner.ServerAnswer;
using Larnix.Server.Entities;

namespace Larnix.Server;

internal interface IServerHandle : IDisposable, ITickable
{
    ServerAnswer Answer { get; }
    void IDisposable.Dispose() => Dispose(false);
    void Dispose(bool emergency);
}

internal class ServerHandle : IServerHandle
{
    public ServerAnswer Answer { get; }

    private ServerType ServerType { get; }
    private string WorldPath { get; }
    private RunSuggestions Suggestions { get; }
    private Action StopSignal { get; }

    private IDbControl Db => GlobRef.Get<IDbControl>();
    private IServer Server => GlobRef.Get<IServer>();
    private IClock Clock => GlobRef.Get<IClock>();
    private QuickServer QuickServer => GlobRef.Get<QuickServer>();
    private ServerConfig ServerConfig => GlobRef.Get<ServerConfig>();
    private IWorldMetaManager WorldMetaManager => GlobRef.Get<IWorldMetaManager>();
    private IDataSaver DataSaver => GlobRef.Get<IDataSaver>();

    private Locker? Locker { get; set; }
    private Receiver? Receiver { get; set; }
    private Scripts? Scripts { get; set; }

    private bool _disposed = false;

    public ServerHandle(ServerType serverType, string worldPath, RunSuggestions suggestions, Action stopSignal)
    {
        ServerType = serverType;
        WorldPath = worldPath;
        Suggestions = suggestions;
        StopSignal = stopSignal;

        if (GlobRef.Has<IServer>())
            throw new InvalidOperationException("Server is already running on current thread.");
        
        if (ServerType == ServerType.Remote)
            Echo.LogRaw("Starting the server...\n");

        Locker = Locker.LockOrException(WorldPath, "world_locker.lock", () =>
            new IOException($"Trying to access world at \"{WorldPath}\" that is already open."));
        
        CreateSingletons();

        Server.PrintHelloToConsole();

        if (ServerType == ServerType.Remote)
        {
            WorldMetaManager.EnsureDetachedServer();
        }

        TryEstablishRelay(Suggestions.RelayAddress, out Task<string>? relayTask);

        Answer = new ServerAnswer(
            Address: Server.LocalAddress,
            Authcode: Server.Authcode,
            RelayEstablishment: relayTask
            );

        Echo.LogSuccess($"Server is running...");
    }

    private void CreateSingletons()
    {
        GlobRef.Set<IServer>(new Server(ServerType, WorldPath, StopSignal));

        GlobRef.New<PhysicsManager, PhysicsManager>();
        GlobRef.New<Chat, Chat>();

        GlobRef.Set<ServerConfig>(
            Config.FromFile<ServerConfig>(WorldPath, Common.ConfigFile)
        );

        GlobRef.Set<IDbControl>(
            new DbControl(
                new SqliteHandle(WorldPath, Common.DatabaseFile)
                )
            );

        GlobRef.New<IDataSaver, DataSaver>();

        GlobRef.New<IWorldMetaManager, WorldMetaManager>();
        GlobRef.New<IChunkRepository, ChunkRepository>();
        GlobRef.New<IEntityRepository, EntityRepository>();
        GlobRef.New<IUserRepository, UserRepository>();

        GlobRef.New<IClock, Clock>();

        GlobRef.New<IEntityControllers, EntityControllers>();
        GlobRef.New<IConnectedPlayers, ConnectedPlayers>();

        long MakeSeed() => Suggestions.Seed ?? RandUtils.SecureLong();

        GlobRef.Set<Generator>(new Generator(Db.Values.GetOrPut("seed", MakeSeed)));
        GlobRef.New<IWorldAPI, WorldAPI>();

        Scripts = new Scripts(
            (0, new IScript[] {
                GlobRef.New<IAtomicChunks, AtomicChunks>(),
            }),
            (1, new IScript[] {
                GlobRef.New<Chunks, Chunks>(), // 1st
                GlobRef.New<IEntityManager, EntityManager>() // 2nd
            }),
            (2, new IScript[] {
                GlobRef.New<BlockSender, BlockSender>(),
                GlobRef.New<ICmdManager, CmdManager>()
            }));

        GlobRef.Set<QuickServer>(
            new QuickServer(
                port: ServerType == ServerType.Remote ? ServerConfig.Port : (ushort)0,
                userAccess: Db.Users,
                config: new QuickSettings()
            ));

        GlobRef.Set<IUserManager>(
            QuickServer.IUserManager
            );

        Receiver = new Receiver();
    }

    private bool TryEstablishRelay(string? relaySuggestion, out Task<string>? relayTask)
    {
        // WARNING: GlobRef should only be accessed from the main thread!
        QuickServer quickServer = GlobRef.Get<QuickServer>();
        string relayAddress = GlobRef.Get<ServerConfig>().Network_RelayAddress;

        if (ServerType == ServerType.Remote)
        {
            if (ServerConfig.Network_UseRelay)
            {
                relayTask = Task.Run(
                    () => quickServer.EstablishRelayAsync(relayAddress));
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
        Receiver!.Tick(Clock.DeltaTime); // for limits
        QuickServer.Tick(Clock.DeltaTime); // refresh & process packets

        // Process server logic
        Scripts!.Tick(Clock.DeltaTime);

        // Tick data saving
        DataSaver.Tick(Clock.DeltaTime);
    }

    public void Dispose(bool emergency)
    {
        if (_disposed) return;
        _disposed = true;

        QuickServer?.Dispose();

        if (DataSaver != null && !emergency)
        {
            DataSaver.SaveAll();
            Echo.Log("Data has been saved.");
        }

        Db?.Handle.Dispose();
        Locker?.Dispose();

        Echo.Log(emergency ?
            "Server has crashed!" :
            "Server has been closed.");
    }
}
