using System.Collections;
using System.Collections.Generic;
using Larnix.Entities;
using Larnix.Entities.Structs;
using Larnix.Socket.Backend;
using Larnix.Core.Physics;
using Larnix.Core.References;
using Larnix.Core.Vectors;
using System;
using Larnix.Core.Utils;
using EntityInits = Larnix.Entities.Entity.EntityInits;

namespace Larnix.Server.Entities
{
    internal enum EntityLoadState { Loading, Active, Unloaded }
    internal class EntityAbstraction : RefObject<Server>
    {
        public ulong UID { get; init; }
        public EntityData ActiveData { get; init; }
        public bool IsPlayer => ActiveData.ID == EntityID.Player;

        private readonly string _nickname;
        public string Nickname => IsPlayer ? _nickname : throw new InvalidOperationException("Only player entities have nicknames!");

        private Entity _controller;
        public Entity Controller => _controller;

        public EntityLoadState LoadState { get; private set; } = EntityLoadState.Loading;
        public bool IsActive => LoadState == EntityLoadState.Active;
        public bool IsUnloaded => LoadState == EntityLoadState.Unloaded;

        private UserManager UserManager => Ref<UserManager>();
        private EntityDataManager EntityDataManager => Ref<EntityDataManager>();
        private PhysicsManager PhysicsManager => Ref<PhysicsManager>();

        public static EntityAbstraction CreatePlayerController(RefObject<Server> reff, string nickname) =>
            new EntityAbstraction(reff, nickname);

        private EntityAbstraction(RefObject<Server> reff, string nickname) : base(reff)
        {
            UID = (ulong)UserManager.GetUserID(nickname);
            ActiveData = EntityDataManager.TryFindEntityData(UID) ?? new EntityData(
                id: EntityID.Player,
                position: new Vec2(0, 0),
                rotation: 0.0f
            );
            ActiveData.Position += Common.UP_EPSILON;
            _nickname = nickname;
            
            EntityDataManager.SetEntityData(UID, ActiveData);
        }

        public static EntityAbstraction CreateEntityController(RefObject<Server> reff, EntityData entityData, ulong uid) =>
            new EntityAbstraction(reff, entityData, uid);

        private EntityAbstraction(RefObject<Server> reff, EntityData entityData, ulong uid) : base(reff)
        {
            if (entityData.ID == EntityID.Player)
                throw new ArgumentException("Cannot create player instance as a generic entity!");

            UID = uid;
            ActiveData = entityData;
            ActiveData.Position += Common.UP_EPSILON;

            EntityDataManager.SetEntityData(UID, ActiveData);
        }

        public void Activate()
        {
            if (!IsActive)
            {
                _controller = EntityFactory.ConstructEntityObject(
                    new EntityInits(UID, ActiveData, PhysicsManager));
                LoadState = EntityLoadState.Active;
            }
            else throw new InvalidOperationException("Entity abstraction is already active!");
        }

        public void FrameUpdate()
        {
            if (IsActive)
            {
                _controller.FrameUpdate();
            }
            else throw new InvalidOperationException("Entity abstraction is not active!");
        }

        public void DeleteEntityInstant()
        {
            if (IsActive)
            {
                EntityDataManager.DeleteEntityData(UID);
                LoadState = EntityLoadState.Unloaded;
            }
            else throw new InvalidOperationException("Entity abstraction is already unloaded!");
        }

        public void UnloadEntityInstant()
        {
            if (IsActive)
            {
                EntityDataManager.UnloadEntityData(UID);
                LoadState = EntityLoadState.Unloaded;
            }
            else throw new InvalidOperationException("Trying to unload an already unloaded entity!");
        }
    }
}
