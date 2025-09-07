using Larnix.Physics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Larnix.Physics
{
    public struct InputData
    {
        public bool Left;
        public bool Right;
        public bool Jump;
    }

    public struct OutputData
    {
        public Vec2 Position;
        public bool OnGround;
        public bool OnCeil;
        public bool OnLeftWall;
        public bool OnRightWall;

        public void Merge(OutputData other)
        {
            Position = other.Position;
            OnGround |= other.OnGround;
            OnLeftWall |= other.OnLeftWall;
            OnRightWall |= other.OnRightWall;
            OnCeil |= other.OnCeil;
        }
    }

    public struct PhysicsProperties
    {
        public double Gravity;
        public double HorizontalForce;
        public double HorizontalDrag;
        public double JumpSize;
        public double MaxVerticalVelocity;
        public double MaxHorizontalVelocity;
    }

    public class DynamicCollider
    {
        private const double MaxTouchVel = 0.1f;

        private readonly PhysicsManager Physics;
        private readonly PhysicsProperties Properties;

        internal Vec2? OldCenter;
        internal Vec2 Center;
        private readonly Vec2 Offset;
        internal readonly Vec2 Size;

        private Vec2 Velocity;
        private OutputData Report;

        public DynamicCollider(PhysicsManager physics, Vec2 center, Vec2 offset, Vec2 size, PhysicsProperties properties)
        {
            Physics = physics;
            Properties = properties;

            OldCenter = null;
            Center = center + offset;
            Offset = offset;
            Size = size;

            Velocity = Vec2.Zero;
            Report = new OutputData { Position = Center };
        }

        public OutputData PhysicsUpdate(InputData inputData)
        {
            // Horizontal physics

            int wantSide = (inputData.Left ? -1 : 0) + (inputData.Right ? 1 : 0);

            if (wantSide != 0)
                Velocity += wantSide * new Vec2(Properties.HorizontalForce, 0f);
            else
            {
                int sgn1 = Math.Sign(Velocity.x);
                Velocity -= sgn1 * new Vec2(Properties.HorizontalDrag, 0f);
                int sgn2 = Math.Sign(Velocity.x);

                if (sgn1 != sgn2)
                    Velocity = new Vec2(0f, Velocity.y);
            }

            // Vertical physics

            Velocity -= new Vec2(0, Properties.Gravity);

            if (Report.OnGround)
            {
                if (inputData.Jump)
                {
                    Velocity = new Vec2(Velocity.x, Properties.JumpSize);
                }
            }

            // Velocity clamp

            if (Math.Abs(Velocity.x) > Properties.MaxHorizontalVelocity)
                Velocity = new Vec2(Math.Sign(Velocity.x) * Properties.MaxHorizontalVelocity, Velocity.y);

            if (Math.Abs(Velocity.y) > Properties.MaxVerticalVelocity)
                Velocity = new Vec2(Velocity.x, Math.Sign(Velocity.y) * Properties.MaxVerticalVelocity);

            // Velocity wall reset

            if (Report.OnGround && Velocity.y < -MaxTouchVel)
                Velocity = new Vec2(Velocity.x, -MaxTouchVel);

            if (Report.OnCeil && Velocity.y > MaxTouchVel)
                Velocity = new Vec2(Velocity.x, MaxTouchVel);

            if (Report.OnLeftWall && Velocity.x < -MaxTouchVel)
                Velocity = new Vec2(-MaxTouchVel, Velocity.y);

            if (Report.OnRightWall && Velocity.x > MaxTouchVel)
                Velocity = new Vec2(MaxTouchVel, Velocity.y);

            // Generate report & update position

            SetWill(Center, Center + Velocity * Time.fixedDeltaTime);
            Report = Physics.MoveCollider(this);
            return RemoveOffset(Report);
        }

        public OutputData NoPhysicsUpdate(Vec2 targetPos)
        {
            Vec2 colliderPos = targetPos + Offset;
            Velocity = Vec2.Zero;

            SetWill(Center, colliderPos);
            Report = new OutputData { Position = colliderPos };
            return RemoveOffset(Report);
        }

        internal void SetWill(Vec2 oldCenter, Vec2 newCenter)
        {
            OldCenter = oldCenter;
            Center = newCenter;
        }

        private OutputData RemoveOffset(OutputData odata)
        {
            odata.Position -= Offset;
            return odata;
        }
    }
}
