#nullable enable
using System.Collections.Generic;
using Larnix.Model.Utils;
using Larnix.Model.Entities.Structs;
using Larnix.Core.Vectors;
using Larnix.Core;
using Larnix.Model.Entities;
using Larnix.Model.Database;
using System.Linq;

namespace Larnix.Server.Data;

internal interface IEntityRepository
{
    ulong NextUid();
    EntityData? FindEntityData(ulong uid);
    Dictionary<ulong, EntityData> EntitiesToLoadByChunk(Vec2Int chunkCoords);
    void SetEntityData(ulong uid, EntityData entityData);
    void UnloadEntityData(ulong uid);
    void DeleteEntityData(ulong uid);
}

internal class EntityRepository : IEntityRepository
{
    private readonly Dictionary<ulong, EntityData> _entityData = new();
    private readonly Dictionary<ulong, EntityData> _unloadedEntityData = new();
    private readonly Dictionary<ulong, EntityData> _deletedEntityData = new();

    private IDbControl Db => GlobRef.Get<IDbControl>();
    private IDataSaver DataSaver => GlobRef.Get<IDataSaver>();

    public EntityRepository()
    {
        DataSaver.SavingAll += FlushIntoDatabase;
    }

    public ulong NextUid()
    {
        return Db.Entities.NextEntityUid();
    }

    public EntityData? FindEntityData(ulong uid)
    {
        if (_deletedEntityData.ContainsKey(uid))
            return null;

        if (_unloadedEntityData.ContainsKey(uid))
            return _unloadedEntityData[uid];

        if (_entityData.ContainsKey(uid))
            return _entityData[uid];

        return Db.Entities.FindEntity(uid);
    }

    public Dictionary<ulong, EntityData> EntitiesToLoadByChunk(Vec2Int chunkCoords)
    {
        Dictionary<ulong, EntityData> dbEntities = Db.Entities.GetEntitiesByChunkNoPlayers(chunkCoords);

        foreach (var uid in _entityData.Keys)
        {
            dbEntities.Remove(uid); // already loaded entity
        }

        foreach (var uid in _deletedEntityData.Keys)
        {
            dbEntities.Remove(uid); // entity already removed
        }

        foreach (var uid in _unloadedEntityData.Keys)
        {
            EntityData newData = _unloadedEntityData[uid];

            Vec2Int newChunkCoords = BlockUtils.CoordsToChunk(newData.Position);
            bool inChunk = newChunkCoords == chunkCoords;

            if (dbEntities.ContainsKey(uid))
            {
                if (inChunk)
                    dbEntities[uid] = newData; // update existing data

                if (!inChunk)
                    dbEntities.Remove(uid); // no longer in this chunk
            }
            else
            {
                if (inChunk)
                {
                    if (newData.ID != EntityID.Player)
                        dbEntities.Add(uid, newData); // additional data found
                }
            }
        }

        return dbEntities;
    }

    public void SetEntityData(ulong uid, EntityData entityData)
    {
        _unloadedEntityData.Remove(uid);
        _deletedEntityData.Remove(uid);

        _entityData[uid] = entityData;
    }

    public void UnloadEntityData(ulong uid)
    {
        if (_entityData.TryGetValue(uid, out var entity))
        {
            _unloadedEntityData.Add(uid, entity);
            _entityData.Remove(uid);
        }
    }

    public void DeleteEntityData(ulong uid)
    {
        if (_entityData.TryGetValue(uid, out var entity))
        {
            _deletedEntityData.Add(uid, entity);
            _entityData.Remove(uid);
        }
    }

    private void FlushIntoDatabase()
    {
        Db.Handle.AsTransaction(() =>
        {
            Db.Entities.DeleteEntities(_deletedEntityData.Keys.ToList());
            _deletedEntityData.Clear();

            Db.Entities.FlushEntities(_entityData);
            Db.Entities.FlushEntities(_unloadedEntityData);
            _unloadedEntityData.Clear();
        });
    }
}
