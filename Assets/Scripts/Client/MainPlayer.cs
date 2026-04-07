using UnityEngine;
using Larnix.Socket.Packets;
using Larnix.Client.Entities;
using Larnix.Model.Entities;
using Larnix.Model.Physics;
using Larnix.Core.Vectors;
using Larnix.Model.Utils;
using Larnix.Server.Packets;
using Larnix.Client.Terrain;
using System;
using Larnix.Model.Entities.All;
using Larnix.Scoping;
using Larnix.Core;
using Larnix.Model.Physics.Structs;
using Larnix.Client.Relativity;
using Larnix.Model.Entities.Structs;

namespace Larnix.Client
{
    public class MainPlayer : MonoBehaviour, IRelativityOrigin
    {
        [SerializeField] Camera Camera;
        [SerializeField] EntityProjection EntityProjection;
        [SerializeField] Transform RaycastCenter; // player's head

        private Client Client => GlobRef.Get<Client>();
        private GridManager GridManager => GlobRef.Get<GridManager>();
        private IPhysicsManager PhysicsManager => GlobRef.Get<IPhysicsManager>();
        private Debugger Debugger => GlobRef.Get<Debugger>();

        Vec2 IRelativityOrigin.OriginOffset => Position.ExtractOrigin();
        public EntityProjection Projection => EntityProjection;

        public bool Alive
        {
            get => transform.parent.gameObject.activeSelf;
            set
            {
                if (value != Alive)
                {
                    transform.parent.gameObject.SetActive(value);
                    (value ? OnRespawn : OnDeath)?.Invoke();
                }
            }
        }

        public ulong MyUid { get; private set; }
        public Vec2 Position { get; private set; }
        public float Rotation
        {
            get
            {
                Vector2 mousePos = Input.mousePosition;
                Vector2 headPos = Camera.WorldToScreenPoint(RaycastCenter.position);
                Vector2 raycastVect = mousePos - headPos;

                return Mathf.Atan2(raycastVect.y, raycastVect.x) * 180f / Mathf.PI;
            }
        }

        public event Action OnRespawn;
        public event Action OnDeath;

        private DynamicCollider _dynamicCollider;
        private uint _lastSentFixedCounter = 0;

        private void Awake()
        {
            GlobRef.Set(this);

            OnRespawn += () =>
            {
                _dynamicCollider = Player.MakeDynamicCollider(Position);
                UpdateTransform();
                EntityProjection.ResetSmoother();
            };

            OnDeath += () =>
            {
                _dynamicCollider = null;
            };

            transform.parent.gameObject.SetActive(false); // start as dead until loaded
        }

        public void FixedPlayerUpdate()
        {
            OutputData? odata = null;
            if (!Debugger.SpectatorMode) // normal movement
            {
                Vec2Int chunk = BlockUtils.CoordsToChunk(Position);
                if (GridManager.ChunkLoaded(chunk))
                {
                    odata = PhysicsManager.TickPhysics(_dynamicCollider, new InputData
                    {
                        Left = MyInput.PressedLeft(),
                        Right = MyInput.PressedRight(),
                        Jump = MyInput.PressedJump(),
                    });
                }
            }
            else // spectator mode
            {
                const float WILL_SIZE = 45f;
                const float STRONG_WILL_SIZE = 90f;

                bool turbo = MyInput.GetKey(KeyCode.Space);

                Vec2 will = (turbo ? STRONG_WILL_SIZE : WILL_SIZE) * Time.fixedDeltaTime * (
                    (MyInput.PressedRight() ? 1 : 0) * new Vec2(1, 0) +
                    (MyInput.PressedLeft() ? 1 : 0) * new Vec2(-1, 0) +
                    (MyInput.PressedUp() ? 1 : 0) * new Vec2(0, 1) +
                    (MyInput.PressedDown() ? 1 : 0) * new Vec2(0, -1)
                );

                odata = PhysicsManager.TickNoPhysics(_dynamicCollider, Position + will);
            }
            Position = odata?.Position ?? Position;

            UpdateTransform();
        }

        public void Teleport(Vec2 position)
        {
            OutputData odata = PhysicsManager.TickNoPhysics(_dynamicCollider, position);
            Position = odata.Position;
        }

        public void LoadPlayerData(Vec2 position, ulong myUid)
        {
            if (!Alive)
            {
                MyUid = myUid;
                Position = position;
            }
        }

        public void EarlyUpdate2()
        {
            if (_lastSentFixedCounter != Client.FixedFrame)
            {
                Payload packet = new PlayerUpdate(Position, Rotation, Client.FixedFrame);
                Client.Send(packet, false); // fast mode

                _lastSentFixedCounter = Client.FixedFrame;
            }
        }

        private void UpdateTransform()
        {
            transform.position = Position.ToUnityPos();
            EntityHeader playerHeader = new(EntityID.Player, Position, Rotation);

            double time = Client.FixedFrame * Time.fixedDeltaTime;
            EntityProjection.UpdateTransform(playerHeader, time);
        }
    }
}
