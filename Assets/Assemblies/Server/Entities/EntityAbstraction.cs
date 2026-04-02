using Larnix.Core;
using Larnix.Core.Vectors;
using Larnix.Model.Entities;
using Larnix.Model.Entities.Structs;
using Larnix.Model.Json;
using Larnix.Model.Physics;
using Larnix.Model.Utils;
using Larnix.Server.Data;
using System;
using EntityInits = Larnix.Model.Entities.Entity.EntityInits;

namespace Larnix.Server.Entities;

internal class EntityAbstraction
{
    public ulong Uid { get; init; }
    public EntityData ActiveData { get; init; }
    public bool IsPlayer => ActiveData.ID == EntityID.Player;

    private readonly string _nickname;
    public string Nickname => IsPlayer ? _nickname : throw new InvalidOperationException("Only player entities have nicknames!");

    private Entity _controller;
    public Entity Controller => _controller;

    public EntityLoadState LoadState { get; private set; } = EntityLoadState.Loading;
    public bool IsActive => LoadState == EntityLoadState.Active;
    public bool IsLoading => LoadState == EntityLoadState.Loading;
    public bool IsUnloaded => LoadState == EntityLoadState.Unloaded;

    private IPlayerActions PlayerActions => GlobRef.Get<IPlayerActions>();
    private EntityDataManager EntityDataManager => GlobRef.Get<EntityDataManager>();
    private PhysicsManager PhysicsManager => GlobRef.Get<PhysicsManager>();

    private EntityAbstraction() { }

    public static EntityAbstraction CreatePlayerController(string nickname) => new(nickname);
    private EntityAbstraction(string nickname)
    {
        Uid = PlayerActions.UidByNickname(nickname);
        ActiveData = EntityDataManager.TryFindEntityData(Uid) ?? new EntityData(
            id: EntityID.Player,
            position: new Vec2(0, 0),
            rotation: 0.0f,
            nbt: new Storage()
        );
        ActiveData.Position += Common.UpEpsilon;
        _nickname = nickname;

        EntityDataManager.SetEntityData(Uid, ActiveData);
    }

    public static EntityAbstraction CreateEntityController(EntityData entityData, ulong uid) => new(entityData, uid);
    private EntityAbstraction(EntityData entityData, ulong uid)
    {
        if (entityData.ID == EntityID.Player)
            throw new ArgumentException("Cannot create player instance as a generic entity!");

        Uid = uid;
        ActiveData = entityData;
        ActiveData.Position += Common.UpEpsilon;

        EntityDataManager.SetEntityData(Uid, ActiveData);
    }

    public void Activate()
    {
        if (!IsLoading)
            throw new InvalidOperationException("Only entities in loading state can be activated!");

        _controller = EntityFactory.ConstructEntityObject(
            new EntityInits(Uid, ActiveData, PhysicsManager));

        LoadState = EntityLoadState.Active;
    }

    public void FrameUpdate()
    {
        if (!IsActive)
            throw new InvalidOperationException("Only active entities can be updated!");

        _controller.FrameUpdate();
    }

    public void DeleteEntityInstant()
    {
        if (!IsUnloaded)
        {
            EntityDataManager.DeleteEntityData(Uid);
            LoadState = EntityLoadState.Unloaded;
        }
    }

    public void UnloadEntityInstant()
    {
        if (!IsUnloaded)
        {
            EntityDataManager.UnloadEntityData(Uid);
            LoadState = EntityLoadState.Unloaded;
        }
    }
}
