#nullable enable
using Larnix.Core.Vectors;
using Larnix.Entities.Structs;
using Larnix.GameCore.Enums;
using Larnix.GameCore.Json;
using Larnix.GameCore.Utils;
using Larnix.Server.Data.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Larnix.Server.Data.Access;

internal interface IEntityAccess
{
    ulong NextEntityUid();
    EntityData? FindEntity(ulong uid);
    Dictionary<ulong, EntityData> GetEntitiesByChunkNoPlayers(Vec2Int chunk);
    void FlushEntities(Dictionary<ulong, EntityData> entities);
    void DeleteEntities(List<ulong> uids);
}

internal class EntityAccess : IEntityAccess
{
    private readonly IDbHandle _db;
    public EntityAccess(IDbHandle db) => _db = db;

    private ulong _minUid = 0;
    public ulong NextEntityUid()
    {
        string cmd = @"
            SELECT min(uid) AS scalar FROM entities;
        ";

        if (_minUid == 0)
        {
            DbRecord record = _db.QuerySingle(cmd)!;

            long minUid = record.Get<long>("scalar", 0);
            _minUid = (ulong)Math.Min(minUid, 0) - 1;
        }

        return --_minUid;
    }

    public EntityData? FindEntity(ulong uid)
    {
        string cmd = @"
            SELECT * FROM entities WHERE uid = $p1;
        ";

        DbRecord? record = _db.QuerySingle(cmd, (long)uid);

        if (record is not null)
        {
            return ExtractEntityData(record);
        }

        return null;
    }

    public Dictionary<ulong, EntityData> GetEntitiesByChunkNoPlayers(Vec2Int chunk)
    {
        string cmd = $@"
            SELECT * FROM entities
                WHERE chunk_x = $p1 AND chunk_y = $p2 AND type <> {(long)EntityID.Player};
        ";

        return _db.QueryList(cmd, (long)chunk.x, (long)chunk.y)
            .Select(record =>
            {
                ulong uid = (ulong)record.Get<long>("uid");
                EntityData entity = ExtractEntityData(record);
                return (uid, entity);
            })
            .ToDictionary(tuple => tuple.uid, tuple => tuple.entity);
    }

    public void FlushEntities(Dictionary<ulong, EntityData> entities)
    {
        _db.AsTransaction(() =>
        {
            foreach (var kvp in entities)
            {
                FlushEntity(kvp.Key, kvp.Value);
            }
        });
    }

    public void DeleteEntities(List<ulong> uids)
    {
        if (uids.Count == 0) return;

        _db.AsTransaction(() =>
        {
            const int BATCH_SIZE = 500;

            for (int i = 0; i < uids.Count; i += BATCH_SIZE)
            {
                int size = Math.Min(BATCH_SIZE, uids.Count - i);

                List<long> batch = uids.GetRange(i, size)
                    .Select(uid => (long)uid)
                    .ToList();

                string cmd = $@"
                DELETE FROM entities WHERE uid IN (
                    {string.Join(", ", batch)}
                    );
                ";

                _db.Execute(cmd);
            }
        });
    }

    private static EntityData ExtractEntityData(DbRecord record)
    {
        EntityID type = record.Get<EntityID>("type");
        Vec2 position = new(
            record.Get<double>("pos_x"),
            record.Get<double>("pos_y")
        );
        float rotation = record.Get<float>("rotation");
        string? nbtString = record.Get<string>("nbt");

        Storage nbt = Storage.FromString(nbtString);
        return new EntityData(type, position, rotation, nbt);
    }

    private void FlushEntity(ulong uid, EntityData entity)
    {
        string cmd = @"
            INSERT OR REPLACE INTO entities
                (uid, type, chunk_x, chunk_y, pos_x, pos_y, rotation, nbt) VALUES
                ($p1, $p2, $p3, $p4, $p5, $p6, $p7, $p8);
        ";

        Vec2Int chunk = BlockUtils.CoordsToChunk(entity.Position);

        _db.Execute(cmd,
            (long)uid,
            (long)entity.ID,
            (long)chunk.x,
            (long)chunk.y,
            entity.Position.x,
            entity.Position.y,
            entity.Rotation,
            entity.NBT.ToString()
            );
    }
}
