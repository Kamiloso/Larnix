using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Socket.Packets;
using Larnix.Client.Entities;
using Larnix.Entities;
using Larnix.Core.Physics;
using Larnix.Core.Vectors;
using Larnix.Entities.Structs;
using Larnix.Core.Utils;
using Larnix.Packets;
using Larnix.Client.Terrain;
using System;
using Larnix.Entities.All;
using Larnix.Core;

namespace Larnix.Client
{
    public class MainPlayer : MonoBehaviour
    {
        // Unity utils
        [SerializeField] Camera Camera;
        [SerializeField] EntityProjection EntityProjection;
        [SerializeField] Transform RaycastCenter; // player's head

        // Editor utils
        [SerializeField] Vector2Int DebugTeleportPos;
        [SerializeField] bool DebugTeleportSubmit;

        // Collider settings
        private DynamicCollider _dynamicCollider;
        private readonly Vec2 ColliderOffset = EntityFactory.GetSlaveInstance<IHasCollider>(EntityID.Player).COLLIDER_OFFSET();
        private readonly Vec2 ColliderSize = EntityFactory.GetSlaveInstance<IHasCollider>(EntityID.Player).COLLIDER_SIZE();
        private readonly PhysicsProperties PhysicsProperties = EntityFactory.GetSlaveInstance<IPhysicsProperties>(EntityID.Player).PHYSICS_PROPERTIES();

        // Singletons
        private Client Client => Ref.Client;
        private GridManager GridManager => Ref.GridManager;
        private TileSelector TileSelector => Ref.TileSelector;
        private PhysicsManager PhysicsManager => Ref.PhysicsManager;
        private Debugger Debugger => Ref.Debugger;

        // Player / Entity data
        public bool IsAlive => transform.parent.gameObject.activeSelf;
        public ulong UID { get; private set; }
        public Vec2 Position { get; private set; }
        public float Rotation { get; private set; }

        // Counters
        private uint _lastSentFixedCounter = 0;

        private void Awake()
        {
            GlobRef.Set(this);
            transform.parent.gameObject.SetActive(false);
        }

        public void FixedPlayerUpdate()
        {
            // Debug teleporting
            if (DebugTeleportSubmit)
            {
                Vec2 tppos = new Vec2(DebugTeleportPos.x, DebugTeleportPos.y);
                Teleport(tppos);
                DebugTeleportSubmit = false;
            }

            // Movement
            OutputData? odata = null;
            if (!Debugger.SpectatorMode)
            {
                Vec2Int chunk = BlockUtils.CoordsToChunk(Position);
                if (GridManager.ChunkLoaded(chunk))
                {
                    odata = _dynamicCollider.PhysicsUpdate(new InputData
                    {
                        Left = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A),
                        Right = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D),
                        Jump = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W),
                    });
                }
            }
            else
            {
                const float WILL_SIZE = 45f;
                const float STRONG_WILL_SIZE = 90f;

                bool left = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
                bool right = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);
                bool up = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W);
                bool down = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);

                bool turbo = Input.GetKey(KeyCode.Space);

                Vec2 will = (turbo ? STRONG_WILL_SIZE : WILL_SIZE) * Time.fixedDeltaTime * (
                    (right ? 1 : 0) * new Vec2(1, 0) +
                    (left ? 1 : 0) * new Vec2(-1, 0) +
                    (up ? 1 : 0) * new Vec2(0, 1) +
                    (down ? 1 : 0) * new Vec2(0, -1)
                );

                odata = _dynamicCollider.NoPhysicsUpdate(Position + will);
            }
            Position = odata?.Position ?? Position;

            RotationUpdate();
            UpdateEntityObject(Client.FixedFrame * Time.fixedDeltaTime);
        }

        public void Teleport(Vec2 position)
        {
            OutputData odata = _dynamicCollider.NoPhysicsUpdate(position);
            Position = odata.Position;
        }

        public void EarlyUpdate2()
        {
            // Send update to server

            if(_lastSentFixedCounter != Client.FixedFrame)
            {
                Payload packet = new PlayerUpdate(Position, Rotation, Client.FixedFrame);
                Client.Send(packet, false); // fast mode (over raw udp)

                _lastSentFixedCounter = Client.FixedFrame;
            }
        }

        private void RotationUpdate()
        {
            Vector2 mouse_pos = Input.mousePosition;
            Vector2 head_pos = Camera.WorldToScreenPoint(RaycastCenter.position);

            Vector2 raycast_vect = mouse_pos - head_pos;

            Rotation = Mathf.Atan2(raycast_vect.y, raycast_vect.x) * 180f / Mathf.PI;
        }

        private void UpdateEntityObject(double time)
        {
            transform.position = ToUnityPos(Position);
            EntityProjection.UpdateTransform(new EntityData(
                id: EntityID.Player,
                position: Position,
                rotation: Rotation
            ), time);
        }

        public void LoadPlayerData(PlayerInitialize msg)
        {
            Position = msg.Position;

            if (UID == 0)
                UID = msg.MyUid;
        }

        public void SetAlive()
        {
            if (IsAlive)
                throw new InvalidOperationException("Player is already alive!");

            transform.parent.gameObject.SetActive(true);
            TileSelector.Enable();

            _dynamicCollider = new DynamicCollider(PhysicsManager,
                Position, ColliderOffset, ColliderSize, PhysicsProperties);

            RotationUpdate();
            UpdateEntityObject(0f);
        }

        public void SetDead()
        {
            if (!IsAlive)
                throw new InvalidOperationException("Player is already dead!");

            transform.parent.gameObject.SetActive(false);
            TileSelector.Disable();

            _dynamicCollider = null;

            EntityProjection.ResetSmoother();
        }

        public EntityProjection GetEntityProjection() => EntityProjection;

#region Larnix offsets

        public Vec2 GetOriginOffset()
        {
            return Position.ExtractOrigin();
        }

        public Vector2 ToUnityPos(Vec2 position)
        {
            Vec2 origin = GetOriginOffset();
            return position.ExtractPosition(origin);
        }

        public Vec2 ToLarnixPos(Vector2 position)
        {
            Vec2 origin = GetOriginOffset();
            return VectorExtensions.ConstructVec2(position, origin);
        }

#endregion
    }
}
