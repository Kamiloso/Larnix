using Larnix.Physics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UIElements;

namespace Larnix.Physics
{
    public struct InputData
    {
        public bool Left;
        public bool Right;
        public bool Jump;
    }

    public class DynamicCollider : MonoBehaviour
    {
        [SerializeField] Vector2 SizeInit;

        [SerializeField] float Gravity = 1.00f;
        [SerializeField] float HorizontalForce = 2.00f;
        [SerializeField] float HorizontalDrag = 2.00f;
        [SerializeField] float JumpSize = 25f;
        [SerializeField] float MaxVerticalVelocity = 45.00f;
        [SerializeField] float MaxHorizontalVelocity = 15.00f;

        private const float VELOCITY_EPSILON = 0.1f;

        public Vector2? OldCenter { get; private set; }
        public Vector2 Center { get; private set; }
        public Vector2 Size { get; private set; }

        private PhysicsManager physicsManager;
        private Vector3 localPos;

        public PhysicsReport physicsReport { get; private set; }
        public Vector2 velocity { get; private set; }

        private bool active = false;

        private void Awake()
        {
            if (transform.parent == null)
                throw new Exception("DynamicCollider must be a child of some GameObject. It can be offseted then relative to it.");

            localPos = transform.localPosition;
        }

        public void Enable()
        {
            if (gameObject.scene.name == "Client")
                physicsManager = Client.References.PhysicsManager;

            if (gameObject.scene.name == "Server")
                physicsManager = Server.References.PhysicsManager;

            OldCenter = null;
            Center = (Vector2)transform.position;
            Size = SizeInit;

            physicsReport = new();
            velocity = Vector2.zero;

            active = true;
        }

        public void Disable()
        {
            OldCenter = null;
            Center = Vector2.zero;
            Size = Vector2.zero;

            active = false;
        }

        public void PhysicsUpdate(InputData inputData)
        {
            // Horizontal physics

            int wantSide = (inputData.Left ? -1 : 0) + (inputData.Right ? 1 : 0);

            if (wantSide != 0)
                velocity += wantSide * new Vector2(HorizontalForce, 0f);
            else
            {
                int sgn1 = System.Math.Sign(velocity.x);
                velocity -= sgn1 * new Vector2(HorizontalDrag, 0f);
                int sgn2 = System.Math.Sign(velocity.x);

                if (sgn1 != sgn2)
                    velocity = new Vector2(0f, velocity.y);
            }

            // Vertical physics

            velocity -= new Vector2(0, Gravity);

            if (physicsReport.OnGround)
            {
                if (inputData.Jump)
                {
                    velocity = new Vector2(velocity.x, JumpSize);
                }
            }

            // Velocity clamp

            if (Math.Abs(velocity.x) > MaxHorizontalVelocity)
                velocity = new Vector2(Math.Sign(velocity.x) * MaxHorizontalVelocity, velocity.y);

            if (Math.Abs(velocity.y) > MaxVerticalVelocity)
                velocity = new Vector2(velocity.x, Math.Sign(velocity.y) * MaxVerticalVelocity);

            // Velocity wall reset

            if (physicsReport.OnGround && velocity.y < -VELOCITY_EPSILON)
                velocity = new Vector2(velocity.x, -VELOCITY_EPSILON);

            if (physicsReport.OnCeil && velocity.y > VELOCITY_EPSILON)
                velocity = new Vector2(velocity.x, VELOCITY_EPSILON);

            if (physicsReport.OnLeftWall && velocity.x < -VELOCITY_EPSILON)
                velocity = new Vector2(-VELOCITY_EPSILON, velocity.y);

            if (physicsReport.OnRightWall && velocity.x > VELOCITY_EPSILON)
                velocity = new Vector2(VELOCITY_EPSILON, velocity.y);

            // Generate report & update position

            SetWill(Center, (Vector2)transform.position + velocity * Time.fixedDeltaTime);
            physicsReport = physicsManager.MoveCollider(this);
            UpdatePosition(physicsReport.Position ?? transform.position);
        }

        public void NoPhysicsUpdate(Vector2 newPosition)
        {
            Vector2 colliderPos = newPosition + (Vector2)localPos;
            velocity = Vector2.zero;

            SetWill(Center, colliderPos);
            physicsReport = new PhysicsReport { Position = colliderPos };
            UpdatePosition(colliderPos);
        }

        public void SetWill(Vector2 oldCenter, Vector2 newCenter)
        {
            OldCenter = oldCenter;
            Center = newCenter;
        }

        private void UpdatePosition(Vector2 colliderPosTarget)
        {
            transform.parent.position = colliderPosTarget - (Vector2)localPos;
            transform.position = colliderPosTarget;
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || active)
            {
                Vector2 drawCenter = active ? Center : (Vector2)transform.position;
                Vector2 drawSize = active ? Size : SizeInit;

                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(drawCenter, drawSize);
            }
        }
    }
}
