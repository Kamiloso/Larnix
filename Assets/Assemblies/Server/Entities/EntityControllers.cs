#nullable enable
using Larnix.Core;
using Larnix.Core.Vectors;
using Larnix.Model.Entities;
using Larnix.Model.Entities.Structs;
using Larnix.Model.Utils;
using Larnix.Server.Data;
using Larnix.Server.Entities.Controllers;
using Larnix.Server.Terrain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Larnix.Server.Entities;

internal interface IEntityControllers
{
    IEnumerable<ulong> Uids { get; }
    IEnumerable<ulong> EntityUids { get; }
    IEnumerable<ulong> PlayerUids { get; } // WARNING: Doesn't include dead players! Use IConnectedPlayers for that.

    event Action<BaseController>? OnUnload;
    event Action<BaseController>? OnKill;

    void LoadEntityControllersByChunk(Vec2Int chunk);
    bool CreateEntityController(EntityData entityTemplate);
    void LoadPlayerController(ulong uid, string nickname);

    BaseController? GetController(ulong uid);
    EntityController? GetEntityController(ulong uid) => GetController(uid) as EntityController;
    PlayerController? GetPlayerController(ulong uid) => GetController(uid) as PlayerController;

    void UnloadController(ulong uid);
    void KillController(ulong uid);
}

internal class EntityControllers : IEntityControllers
{
    private readonly Dictionary<ulong, BaseController> _controllers = new();

    public IEnumerable<ulong> Uids => _controllers.Keys.ToList();
    public IEnumerable<ulong> EntityUids => _controllers.Where(kvp => kvp.Value is EntityController).Select(kvp => kvp.Key).ToList();
    public IEnumerable<ulong> PlayerUids => _controllers.Where(kvp => kvp.Value is PlayerController).Select(kvp => kvp.Key).ToList();

    public event Action<BaseController>? OnUnload;
    public event Action<BaseController>? OnKill;

    private IEntityRepository EntityRepository => GlobRef.Get<IEntityRepository>();
    private Chunks Chunks => GlobRef.Get<Chunks>();

    public void LoadEntityControllersByChunk(Vec2Int chunk)
    {
        Dictionary<ulong, EntityData> entitiesToLoad = EntityRepository.EntitiesToLoadByChunk(chunk);
        foreach (var (uid, entityData) in entitiesToLoad)
        {
            AttachEntityController(uid, entityData);
        }
    }

    public bool CreateEntityController(EntityData entityTemplate)
    {
        ulong uid = EntityRepository.NextUid();
        EntityData entityData = entityTemplate.DeepCopy();

        if (Chunks.IsLoadedPosition(entityData.Position))
        {
            AttachEntityController(uid, entityData);
            return true;
        }

        return false;
    }

    private void AttachEntityController(ulong uid, EntityData entityData)
    {
        EntityRepository.SetEntityData(uid, entityData);
        var controller = new EntityController(uid, entityData);
        _controllers.Add(uid, controller);
    }

    public void LoadPlayerController(ulong uid, string nickname)
    {
        EntityData? entityData = EntityRepository.FindEntityData(uid);

        if (entityData is not null && entityData.Header.ID != EntityID.Player)
            throw new InvalidOperationException($"Entity with UID {uid} is not a player.");

        entityData ??= new EntityData( // default data for a new player
            id: EntityID.Player,
            position: new Vec2(0, 0) + Common.UpEpsilon,
            rotation: 0.0f,
            nbt: null
        );

        AttachPlayerController(uid, nickname, entityData);
    }

    private void AttachPlayerController(ulong uid, string nickname, EntityData entityData)
    {
        EntityRepository.SetEntityData(uid, entityData);
        var controller = new PlayerController(uid, nickname, entityData);
        _controllers.Add(uid, controller);
    }

    public BaseController? GetController(ulong uid)
    {
        _controllers.TryGetValue(uid, out BaseController? controller);
        return controller;
    }

    public void UnloadController(ulong uid)
    {
        if (!_controllers.TryGetValue(uid, out var controller))
            throw new InvalidOperationException($"Controller with UID {uid} is not loaded.");

        OnUnload?.Invoke(controller);
        controller.Unload();

        EntityRepository.UnloadEntityData(uid);
        _controllers.Remove(uid);
    }

    public void KillController(ulong uid)
    {
        if (!_controllers.TryGetValue(uid, out var controller))
            throw new InvalidOperationException($"Controller with UID {uid} is not loaded.");

        OnKill?.Invoke(controller);
        controller.Kill();

        EntityRepository.DeleteEntityData(uid);
        _controllers.Remove(uid);
    }
}
