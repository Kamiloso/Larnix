#nullable enable
using System;
using Larnix.Core;
using Larnix.Core.Utils;
using Larnix.Model;
using Larnix.Model.Blocks;
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
using Larnix.Server.Run.Records;
using ServerClass = Larnix.Server.Network.Server;

namespace Larnix.Server.Run;

internal class ServerRoot : ITickable, IDisposable
{
    public volatile bool IsCrashed = false;

    private readonly DbControl _db;
    private readonly WorldMetaManager _worldMetaManager;
    private readonly Clock _clock;
    private readonly DataSaver _dataSaver;
    private readonly ServerClass _server;
    private readonly Scripts _scripts;

    private bool _disposed = false;

    public ServerRoot(RunInfo runInfo, RunCallbacks runCallbacks)
    {
        try
        {
            if (GlobRef.Has<IServer>())
                throw new InvalidOperationException("Server is already running on current thread.");

            if (runInfo.Mode == RunMode.Remote)
                Echo.LogRaw("Starting the server...\n");

            // ----------------------------------------------------------------------------------------

            // Arguments
            GlobRef.Set(runInfo);
            GlobRef.Set(runCallbacks);

            // Config
            GlobRef.Set<IConfigSaver>(
                new ConfigSaver("config.json")
                );

            // Database & More
            GlobRef.Set<IDbControl>(
                _db = new DbControl(
                    new SqliteHandle(runInfo.WorldPath, Common.DatabaseFile)
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
                        () => runInfo.Seed ?? RandUtils.SecureLong())
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
            GlobRef.Set<IServer>(_server = new ServerClass());

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

            EnsureDetachedServer();
            PrintHelloToConsole();

            void EnsureDetachedServer()
            {
                if (runInfo.Mode == RunMode.Remote)
                {
                    _worldMetaManager.EnsureDetachedServer();
                }
            }

            void PrintHelloToConsole()
            {
                if (runInfo.Mode == RunMode.Remote)
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
        catch
        {
            Dispose();
            throw;
        }
    }

    public void Tick(float deltaTime)
    {
        _clock.Tick(deltaTime);

        _server.Tick(_clock.DeltaTime);
        _scripts.Tick(_clock.DeltaTime);

        _dataSaver.Tick(_clock.DeltaTime);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _server?.Dispose();

        if (!IsCrashed && _dataSaver != null)
        {
            _dataSaver.SaveWorld();
            Echo.Log("Data has been saved.");
        }

        _db?.Handle.Dispose();

        Echo.Log(IsCrashed ?
            "Server has crashed!" :
            "Server has been closed.");
    }
}
