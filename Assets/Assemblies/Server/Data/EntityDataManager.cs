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

internal class EntityDataManager
{
    private readonly Dictionary<ulong, EntityData> _entityData = new();
    private readonly Dictionary<ulong, EntityData> _unloadedEntityData = new();
    private readonly Dictionary<ulong, EntityData> _deletedEntityData = new();

    private IDbControl Db => GlobRef.Get<IDbControl>();

    public ulong NextUID()
    {
        return Db.Entities.NextEntityUid();
    }

    public EntityData? TryFindEntityData(ulong uid)
    {
        if (_deletedEntityData.ContainsKey(uid))
            return null;

        if (_unloadedEntityData.ContainsKey(uid))
            return _unloadedEntityData[uid];

        if (_entityData.ContainsKey(uid))
            return _entityData[uid];

        return Db.Entities.FindEntity(uid);
    }

    public Dictionary<ulong, EntityData> GetUnloadedEntitiesByChunk(Vec2Int chunkCoords)
    {
        var entityList = Db.Entities.GetEntitiesByChunkNoPlayers(chunkCoords);

        foreach (var uid in _entityData.Keys)
        {
            entityList.Remove(uid); // already loaded entity
        }

        foreach (var uid in _deletedEntityData.Keys)
        {
            entityList.Remove(uid); // entity already removed
        }

        foreach (var uid in _unloadedEntityData.Keys)
        {
            EntityData newData = _unloadedEntityData[uid];

            Vec2Int newChunkCoords = BlockUtils.CoordsToChunk(newData.Position);
            bool inChunk = newChunkCoords == chunkCoords;

            if (entityList.ContainsKey(uid))
            {
                if (inChunk)
                    entityList[uid] = newData; // update existing data

                if (!inChunk)
                    entityList.Remove(uid); // no longer in this chunk
            }
            else
            {
                if (inChunk)
                {
                    if (newData.ID != EntityID.Player)
                        entityList.Add(uid, newData); // additional data found
                }
            }
        }

        return entityList;
    }

    public void SetEntityData(ulong uid, EntityData entityData)
    {
        _unloadedEntityData.Remove(uid);
        _deletedEntityData.Remove(uid);

        _entityData[uid] = entityData;
    }

    public void UnloadEntityData(ulong uid)
    {
        if (_entityData.ContainsKey(uid))
        {
            _unloadedEntityData.Add(uid, _entityData[uid]);
            _entityData.Remove(uid);
        }
    }

    public void DeleteEntityData(ulong uid)
    {
        if (_entityData.ContainsKey(uid))
        {
            _deletedEntityData.Add(uid, _entityData[uid]);
            _entityData.Remove(uid);
        }
    }

    public void FlushIntoDatabase()
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
