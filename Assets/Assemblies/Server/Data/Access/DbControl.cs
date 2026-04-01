#nullable enable
using Larnix.Server.Data.Access;
using Larnix.Server.Data.Database;
using System;
using System.Collections.Generic;

namespace Larnix.Server;

internal interface IDbControl
{
    public IDbHandle Handle { get; }

    public IChunkAccess Chunks { get; }
    public IEntityAccess Entities { get; }
    public IUserAccess Users { get; }
    public IValueAccess Values { get; }
}

internal class DbControl : IDbControl
{
    private readonly IDbHandle _db;
    private readonly Dictionary<Type, object> _accessors = new();

    public IDbHandle Handle => _db;

    public IChunkAccess Chunks => GetAccessor<ChunkAccess>();
    public IEntityAccess Entities => GetAccessor<EntityAccess>();
    public IUserAccess Users => GetAccessor<UserAccess>();
    public IValueAccess Values => GetAccessor<ValueAccess>();

    private static string FORMAT(string cmd) => cmd.Replace("#", "NOT NULL");

    public DbControl(IDbHandle db)
    {
        // MIGRATION TIPS:
        // ----------------------------------------------------------------------------------------
        // Old tables (from before 0.0.45) may not have NOT NULL restrictions,
        // but they shouldn't contain any NULLs anyway.
        // ----------------------------------------------------------------------------------------
        // Column chunks.nbt may rarely have a default value of ''.
        // It happens when it was added into an already existing old table (from before 0.0.45).
        // ----------------------------------------------------------------------------------------

        _db = db;
        _db.Execute(FORMAT(@"
            CREATE TABLE IF NOT EXISTS players(
                uid INTEGER # PRIMARY KEY,
                nickname TEXT #,
                password_hash TEXT #,
                challenge_id INTEGER #
            );
            CREATE INDEX IF NOT EXISTS idx_players
                ON players(nickname);

            CREATE TABLE IF NOT EXISTS entities(
                uid INTEGER # PRIMARY KEY,
                type INTEGER #,
                chunk_x INTEGER #,
                chunk_y INTEGER #,
                pos_x REAL #,
                pos_y REAL #,
                rotation REAL #,
                nbt TEXT #
            );
            CREATE INDEX IF NOT EXISTS idx_chunk_entities
                ON entities(chunk_x, chunk_y);

            CREATE TABLE IF NOT EXISTS chunks(
                chunk_x INTEGER #,
                chunk_y INTEGER #,
                block_bytes BLOB #,
                nbt TEXT #,
                PRIMARY KEY(chunk_x, chunk_y)
            );

            CREATE TABLE IF NOT EXISTS key_values (
                key TEXT # PRIMARY KEY,
                value INTEGER #
            );

        "));

        try { _db.Execute(FORMAT(@"ALTER TABLE chunks ADD COLUMN nbt TEXT # DEFAULT '';")); } catch { }
    }

    private T GetAccessor<T>()
    {
        if (_accessors.TryGetValue(typeof(T), out object? accessor))
            return (T)accessor;

        accessor = Activator.CreateInstance(typeof(T), _db) ??
            throw new InvalidOperationException($"Failed to create an instance of {typeof(T).FullName}");

        _accessors[typeof(T)] = accessor;
        return (T)accessor;
    }
}
