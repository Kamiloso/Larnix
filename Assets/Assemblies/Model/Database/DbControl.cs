#nullable enable
using Larnix.Model.Database.Connection;
using System;
using System.Collections.Generic;

namespace Larnix.Model.Database;

public interface IDbControl
{
    public IDbHandle Handle { get; }

    public IChunkAccess Chunks { get; }
    public IEntityAccess Entities { get; }
    public IUserAccess Users { get; }
    public IValueAccess Values { get; }
}

public class DbControl : IDbControl
{
    private readonly IDbHandle _db;
    private readonly Dictionary<Type, object> _accessors = new();

    public IDbHandle Handle => _db;

    public IChunkAccess Chunks => GetAccessor<IChunkAccess>();
    public IEntityAccess Entities => GetAccessor<IEntityAccess>();
    public IUserAccess Users => GetAccessor<IUserAccess>();
    public IValueAccess Values => GetAccessor<IValueAccess>();
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

        _accessors[typeof(IChunkAccess)] = new ChunkAccess(_db);
        _accessors[typeof(IEntityAccess)] = new EntityAccess(_db);
        _accessors[typeof(IUserAccess)] = new UserAccess(_db);
        _accessors[typeof(IValueAccess)] = new ValueAccess(_db);
    }

    private T GetAccessor<T>()
    {
        if (!_accessors.TryGetValue(typeof(T), out object? accessor))
            throw new InvalidCastException($"Accessor of type {typeof(T)} is not registered in the database control.");

        return (T)accessor;
    }
}
