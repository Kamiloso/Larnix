#nullable enable
using Larnix.Core;
using Larnix.Core.Files;
using Larnix.Core.Utils;
using Larnix.Model;
using Larnix.Model.Blocks;
using Larnix.Model.Configs;
using Larnix.Model.Database;
using Larnix.Model.Database.Connection;
using Larnix.Model.Physics;
using Larnix.Model.Worldgen;
using Larnix.Server.Chunks;
using Larnix.Server.Chunks.Scripts;
using Larnix.Server.Commands;
using Larnix.Server.Data;
using Larnix.Server.Entities;
using Larnix.Server.Entities.Scripts;
using Larnix.Server.Network;
using Larnix.Server.Repositories;
using Larnix.Socket.Server.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;
using ServerAnswer = Larnix.Server.ServerRunner.ServerAnswer;
using RunSuggestions = Larnix.Server.ServerRunner.RunSuggestions;
using ServerClass = Larnix.Server.Network.Server;

namespace Larnix.Server;

internal interface IServerHandle : IDisposable, ITickable
{
    ServerAnswer Answer { get; }
    void Dispose(bool emergency);
}

internal class ServerHandle : IServerHandle
{
    public ServerAnswer Answer { get; }

    private readonly Locker _locker;
    private readonly ServerInfo _serverInfo;
    private readonly DbControl _db;
    private readonly WorldMetaManager _worldMetaManager;
    private readonly Clock _clock;
    private readonly DataSaver _dataSaver;
    private readonly ServerClass _server;
    private readonly Scripts _scripts;

    private bool _disposed = false;

    public ServerHandle(ServerType serverType, string worldPath, RunSuggestions suggestions, Action stopSignal)
    {
        if (GlobRef.Has<IServer>())
            throw new InvalidOperationException("Server is already running on current thread.");
        
        if (serverType == ServerType.Remote)
            Echo.LogRaw("Starting the server...\n");

        _locker = Locker.LockOrException(worldPath, "world_locker.lock", () =>
            new IOException($"Trying to access world at \"{worldPath}\" that is already open."));

        // ----------------------------------------------------------------------------------------

        // Config
        GlobRef.Set<IServerInfo>(_serverInfo = new ServerInfo(serverType, worldPath));
        GlobRef.Set(
            BaseConfig.FromFile<Config>(
                worldPath, Common.ConfigFile
                ));

        // Database & More
        GlobRef.Set<IDbControl>(
            _db = new DbControl(
                new SqliteHandle(worldPath, Common.DatabaseFile)
                ));
        GlobRef.Set<IDataSaver>(_dataSaver = new DataSaver());
        GlobRef.Set<IWorldMetaManager>(_worldMetaManager = new WorldMetaManager());

        // Repositories
        GlobRef.New<IChunkRepository, ChunkRepository>();
        GlobRef.New<IEntityRepository, EntityRepository>();
        GlobRef.New<IUserRepository, UserRepository>();
        GlobRef.New<IValueRepository, ValueRepository>();

        // Services
        GlobRef.Set<IGenerator>(
            new Generator(
                _db.Values.GetOrInsert("seed",
                    () => suggestions.Seed ?? RandUtils.SecureLong())
                ));
        GlobRef.Set<IPhysicsManager>(
            new PhysicsManager(
                Common.PhysicsSectorSize
                ));
        GlobRef.Set<IClock>(_clock = new Clock());
        GlobRef.New<IChat, Chat>();

        // Socket Management
        GlobRef.New<IBanProvider, BanProvider>();
        GlobRef.New<IPasswordHasher, PasswordHasher>();
        GlobRef.Set<IServer>(_server = new ServerClass(suggestions, stopSignal, out Task<string?>? relayTask));

        // Chunks
        GlobRef.New<IAtomicChunks, AtomicChunks>();
        GlobRef.New<IChunkLoader, ChunkLoader>();
        GlobRef.New<IChunkHolders, ChunkHolders>();
        GlobRef.New<IWorldAPI, WorldAPI>();

        // Entities
        GlobRef.New<IEntityControllers, EntityControllers>();
        GlobRef.New<IConnectedPlayers, ConnectedPlayers>();

        // Scripts
        _scripts = new Scripts(
            (1, new IScript[] {
                GlobRef.New<IChunkManager, ChunkManager>(),
                GlobRef.New<IEntityManager, EntityManager>()
            }),
            (2, new IScript[] {
                GlobRef.New<IChunkSender, ChunkSender>(),
                GlobRef.New<IEntitySender, EntitySender>()
            }),
            (3, new IScript[] {
                GlobRef.New<ICmdManager, CmdManager>()
            })
        );

        // ----------------------------------------------------------------------------------------

        Answer = new ServerAnswer(
            Address: _server.LocalAddress,
            Authcode: _server.Authcode,
            RelayTask: relayTask
            );

        EnsureDetachedServer();
        PrintHelloToConsole();

        void EnsureDetachedServer()
        {
            if (serverType == ServerType.Remote)
            {
                _worldMetaManager.EnsureDetachedServer();
            }
        }

        void PrintHelloToConsole()
        {
            if (_serverInfo.Type == ServerType.Remote)
            {
                Echo.SetTitle("Larnix Server " + GameInfo.Version);
                Echo.PrintBorder();

                Echo.LogRaw($"Socket created on port: {_server.Port}\n");
                Echo.LogRaw($"Authcode: {_server.Authcode}\n");
                Echo.PrintBorder();
            }
            else
            {
                Echo.Log($"Port: {_server.Port} | Authcode: {_server.Authcode}");
            }
        }

        Echo.LogSuccess($"Server is running...");
    }

    public void Tick(float deltaTime)
    {
        _clock.Tick(deltaTime);

        _server.Tick(deltaTime);
        _scripts.Tick(_clock.DeltaTime);

        _dataSaver.Tick(_clock.DeltaTime);
    }

    public void Dispose(bool emergency)
    {
        if (_disposed) return;
        _disposed = true;

        _server.Dispose();

        if (_dataSaver != null && !emergency)
        {
            _dataSaver.SaveAll();
            Echo.Log("Data has been saved.");
        }

        _db.Handle.Dispose();
        _locker.Dispose();

        Echo.Log(emergency ?
            "Server has crashed!" :
            "Server has been closed.");
    }

    public void Dispose()
    {
        Dispose(emergency: false);
    }
}
