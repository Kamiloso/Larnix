#nullable enable
using System;
using Larnix.Core;
using Larnix.Core.Vectors;
using Larnix.Model.Entities;
using Larnix.Model.Entities.Structs;
using Larnix.Model.Utils;
using Larnix.Server.Entities.Controllers;
using Larnix.Server.Entities.Data;
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

    void PrepareEntityControllers(Vec2Int chunk);
    void ActivateEntityControllers(Vec2Int chunk);
    void UnloadEntityControllers(Vec2Int chunk);

    void SpawnEntity(EntityData entityTemplate);
    void LoadPlayerController(ulong uid, string nickname);
    BaseController? GetController(ulong uid);

    void UnloadController(ulong uid);
    void KillController(ulong uid);
}

internal class EntityControllers : IEntityControllers
{
    private readonly Dictionary<ulong, BaseController> _controllers = new();
    private readonly HashSet<Vec2Int> _activeChunks = new();

    public IEnumerable<ulong> Uids => _controllers.Keys.ToList();
    public IEnumerable<ulong> EntityUids => _controllers.Where(kvp => kvp.Value is EntityController).Select(kvp => kvp.Key).ToList();
    public IEnumerable<ulong> PlayerUids => _controllers.Where(kvp => kvp.Value is PlayerController).Select(kvp => kvp.Key).ToList();

    public event Action<BaseController>? OnUnload;
    public event Action<BaseController>? OnKill;

    private IEntityRepository EntityRepository => GlobRef.Get<IEntityRepository>();

    public void PrepareEntityControllers(Vec2Int chunk)
    {
        // load controllers with center in this chunk
        var entitiesToLoad = EntityRepository.EntitiesToLoadByChunk(chunk);
        foreach (var (uid, entityData) in entitiesToLoad)
        {
            AttachEntityController(uid, entityData);
        }
    }

    public void ActivateEntityControllers(Vec2Int chunk)
    {
        _activeChunks.Add(chunk);

        ForEachEntityControllersIf(
            position => ColliderUtils.InActiveChunks(position, _activeChunks),
            (_, controller) => controller.Activate()
            );
    }

    public void UnloadEntityControllers(Vec2Int chunk)
    {
        _activeChunks.Remove(chunk);

        ForEachEntityControllersIf(
            position => !ColliderUtils.InActiveChunks(position, _activeChunks),
            (_, controller) => controller.Deactivate()
            );

        ForEachEntityControllersIf(
            position => ColliderUtils.CenterInChunk(position, chunk),
            (uid, _) => UnloadController(uid)
            );
    }

    private void ForEachEntityControllersIf(Predicate<Vec2> predicate, Action<ulong, EntityController> action)
    {
        foreach (ulong uid in EntityUids)
        {
            var controller = (EntityController)_controllers[uid];
            var position = controller.ActiveData.Position;

            if (predicate.Invoke(position))
            {
                action.Invoke(uid, controller);
            }
        }
    }

    public void SpawnEntity(EntityData entityTemplate)
    {
        ulong uid = EntityRepository.NextUid();
        EntityData entityData = entityTemplate.DeepCopy();

        AttachEntityController(uid, entityData);

        Vec2 position = entityData.Position;
        if (!ColliderUtils.CenterInAnyChunk(position, _activeChunks))
        {
            UnloadController(uid); // instantly unload
        }
    }

    private void AttachEntityController(ulong uid, EntityData entityData)
    {
        var controller = new EntityController(uid, entityData);
        _controllers.Add(uid, controller);

        Vec2 position = entityData.Position;
        if (ColliderUtils.InActiveChunks(position, _activeChunks))
        {
            controller.Activate();
        }
    }

    public void LoadPlayerController(ulong uid, string nickname)
    {
        EntityData? entityData = EntityRepository.FindEntityData(uid);

        if (entityData is not null && entityData.Header.Id != EntityID.Player)
            throw new InvalidOperationException($"Entity with UID {uid} is not a player.");

        entityData ??= new EntityData( // default data for a new player (temporary)
            id: EntityID.Player,
            position: new Vec2(0, 0) + Common.WorldEpsilonUp,
            rotation: 0.0f,
            nbt: null
        );

        AttachPlayerController(uid, nickname, entityData);
    }

    private void AttachPlayerController(ulong uid, string nickname, EntityData entityData)
    {
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

        _controllers.Remove(uid);
    }

    public void KillController(ulong uid)
    {
        if (!_controllers.TryGetValue(uid, out var controller))
            throw new InvalidOperationException($"Controller with UID {uid} is not loaded.");

        OnKill?.Invoke(controller);
        controller.Kill();

        _controllers.Remove(uid);
    }
}
