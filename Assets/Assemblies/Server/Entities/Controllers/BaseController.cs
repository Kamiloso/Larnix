#nullable enable
using System;
using Larnix.Core;
using Larnix.Core.Vectors;
using Larnix.Model.Entities;
using Larnix.Model.Entities.Structs;
using Larnix.Model.Physics;
using Larnix.Server.Commands;
using Larnix.Server.Entities.Data;

namespace Larnix.Server.Entities.Controllers;

internal abstract class BaseController
{
    public ulong Uid { get; }
    public EntityData ActiveData { get; } // directly binded to entity-saving system
    public Vec2 Position => ActiveData.Position;
    public float Rotation => ActiveData.Rotation;

    protected Entity? RealInstance { get; private set; }
    public bool IsActive => RealInstance is not null;

    private EntityInterfaces Interfaces => new(
        Physics: GlobRef.Get<IPhysicsManager>(),
        CmdExecutor: GlobRef.Get<ICmdManager>()
        );

    private IEntityRepository EntityRepository => GlobRef.Get<IEntityRepository>();

    private bool _deactivated = false;

    public BaseController(ulong uid, EntityData entityData)
    {
        Uid = uid;
        ActiveData = entityData;

        EntityRepository.SetEntityData(Uid, ActiveData);
    }

    public void Activate() // idempotent
    {
        if (IsActive) return;

        EntityInits inits = new(Uid, ActiveData, Interfaces);
        RealInstance = EntityFactory.ConstructEntityObject(inits);
    }

    public void Deactivate() // idempotent
    {
        if (!IsActive) return;

        RealInstance = null;
    }

    public void FrameUpdate()
    {
        if (!IsActive)
            throw new InvalidOperationException($"Controller with UID {Uid} is not active and cannot be updated.");

        RealInstance?.FrameUpdate();
    }

    public void Unload()
    {
        if (_deactivated)
            throw new InvalidOperationException($"Controller with UID {Uid} is already deactivated and cannot be unloaded.");

        OnUnload();
        EntityRepository.UnloadEntityData(Uid);
        _deactivated = true;
    }

    public void Kill()
    {
        if (_deactivated)
            throw new InvalidOperationException($"Controller with UID {Uid} is already deactivated and cannot be killed.");

        OnKill();
        EntityRepository.DeleteEntityData(Uid);
        _deactivated = true;
    }

    protected virtual void OnUnload() { }
    protected virtual void OnKill() { }
}
