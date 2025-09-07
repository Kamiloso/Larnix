using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Larnix.Packets;
using Larnix.Client.Entities;
using Larnix.Entities;
using QuickNet.Channel;
using Larnix.Physics;
using Larnix.Blocks;
using IHasCollider = Larnix.Entities.IHasCollider;

namespace Larnix.Client
{
    public class MainPlayer : MonoBehaviour
    {
        // Serialized Unity fields
        [SerializeField] Camera Camera;
        [SerializeField] EntityProjection EntityProjection;
        [SerializeField] Transform RaycastCenter; // player's head

        // Teleport with editor
        [SerializeField] Vector2Int DebugTeleportPos;
        [SerializeField] bool DebugTeleportSubmit;

        // Collider settings
        private DynamicCollider DynamicCollider;
        private readonly Vec2 ColliderOffset = EntityFactory.GetSlaveInstance<IHasCollider>(EntityID.Player).COLLIDER_OFFSET();
        private readonly Vec2 ColliderSize = EntityFactory.GetSlaveInstance<IHasCollider>(EntityID.Player).COLLIDER_SIZE();
        private readonly PhysicsProperties PhysicsProperties = new()
        {
            Gravity = 1.00,
            HorizontalForce = 2.00,
            HorizontalDrag = 2.00,
            JumpSize = 25.00,
            MaxVerticalVelocity = 45.00,
            MaxHorizontalVelocity = 15.00,
        };

        // Player / Entity data
        public bool IsAlive => transform.parent.gameObject.activeSelf;
        public Vec2 Position { get; private set; }
        public float Rotation { get; private set; }

        // Counters
        private uint FixedCounter = 0;
        private uint LastSentFixedCounter = 0;

        private void Awake()
        {
            Ref.MainPlayer = this;
            transform.parent.gameObject.SetActive(false);
        }

        private void FixedUpdate()
        {
            FixedCounter++;

            // Debug teleporting
            if (DebugTeleportSubmit)
            {
                Vec2 tppos = new Vec2(DebugTeleportPos.x, DebugTeleportPos.y);
                Teleport(tppos);
                DebugTeleportSubmit = false;
            }

            // Movement
            OutputData? odata = null;
            if (!Ref.Debug.SpectatorMode)
            {
                Vector2Int chunk = ChunkMethods.CoordsToChunk(Position);
                if (Ref.GridManager.ChunkLoaded(chunk))
                {
                    odata = DynamicCollider.PhysicsUpdate(new InputData
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

                odata = DynamicCollider.NoPhysicsUpdate(Position + will);
            }
            Position = odata?.Position ?? Position;

            RotationUpdate();
            UpdateEntityObject(FixedCounter * Time.fixedDeltaTime);
        }

        public void Teleport(Vec2 position)
        {
            OutputData odata = DynamicCollider.NoPhysicsUpdate(position);
            Position = odata.Position;
        }

        public void EarlyUpdate2()
        {
            // Send update to server

            if(LastSentFixedCounter != FixedCounter)
            {
                Packet packet = new PlayerUpdate(Position, Rotation, FixedCounter);
                Ref.Client.Send(packet, false); // fast mode (over raw udp)

                LastSentFixedCounter = FixedCounter;
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
            EntityProjection.UpdateTransform(new EntityData
            {
                ID = EntityID.Player,
                Position = Position,
                Rotation = Rotation,
                NBT = null
            }, time);
        }

        public Vector2 ClientPosition()
        {
            return transform.position;
        }

        public void LoadPlayerData(PlayerInitialize msg)
        {
            Position = msg.Position;
        }

        public void SetAlive()
        {
            if (IsAlive)
                throw new System.InvalidOperationException("Player is already alive!");

            transform.parent.gameObject.SetActive(true);
            Ref.TileSelector.Enable();

            DynamicCollider = new DynamicCollider(Ref.PhysicsManager,
                Position, ColliderOffset, ColliderSize, PhysicsProperties);

            RotationUpdate();
            UpdateEntityObject(0f);
        }

        public void SetDead()
        {
            if (!IsAlive)
                throw new System.InvalidOperationException("Player is already dead!");

            transform.parent.gameObject.SetActive(false);
            Ref.TileSelector.Disable();

            DynamicCollider = null;

            EntityProjection.ResetSmoother();
        }

        // --- OFFSET SEGMENT ---

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
            return new Vec2(position, origin);
        }
    }
}
