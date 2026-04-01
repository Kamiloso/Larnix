using System.Collections;
using System.Collections.Generic;
using System;
using Larnix.GameCore.Utils;
using Larnix.Core.Vectors;
using Larnix.GameCore.Physics.Structs;

namespace Larnix.GameCore.Physics;

public class DynamicCollider
{
    private const double MAX_TOUCH_VELOCITY = 0.1f;

    internal Vec2? OldCenter;
    internal Vec2 Center;
    internal readonly Vec2 Offset;
    internal readonly Vec2 Size;

    private readonly PhysicsManager _physics;
    private readonly PhysicsProperties _properties;

    private Vec2 _velocity;
    private OutputData _report;

    public DynamicCollider(PhysicsManager physics, Vec2 center, Vec2 offset, Vec2 size, PhysicsProperties properties)
    {
        _physics = physics;
        _properties = properties;

        OldCenter = null;
        Center = center + offset;
        Offset = offset;
        Size = size;

        _velocity = Vec2.Zero;
        _report = new OutputData { Position = Center };
    }

    public OutputData PhysicsUpdate(InputData inputData)
    {
        // Horizontal physics

        int wantSide = (inputData.Left ? -1 : 0) + (inputData.Right ? 1 : 0);

        if (wantSide != 0)
            _velocity += wantSide * new Vec2(_properties.ControlForce, 0f);
        else
        {
            int sgn1 = Math.Sign(_velocity.x);
            _velocity -= sgn1 * new Vec2(_properties.HorizontalDrag, 0f);
            int sgn2 = Math.Sign(_velocity.x);

            if (sgn1 != sgn2)
                _velocity = new Vec2(0f, _velocity.y);
        }

        // Vertical physics

        _velocity -= new Vec2(0, _properties.Gravity);

        if (_report.OnGround)
        {
            if (inputData.Jump)
            {
                _velocity = new Vec2(_velocity.x, _properties.JumpSize);
            }
        }

        // Velocity clamp

        if (Math.Abs(_velocity.x) > _properties.MaxHorizontalVelocity)
            _velocity = new Vec2(Math.Sign(_velocity.x) * _properties.MaxHorizontalVelocity, _velocity.y);

        if (Math.Abs(_velocity.y) > _properties.MaxVerticalVelocity)
            _velocity = new Vec2(_velocity.x, Math.Sign(_velocity.y) * _properties.MaxVerticalVelocity);

        // Velocity wall reset

        const double MTV = MAX_TOUCH_VELOCITY;

        if (_report.OnGround && _velocity.y < -MTV)
            _velocity = new Vec2(_velocity.x, -MTV);

        if (_report.OnCeil && _velocity.y > MTV)
            _velocity = new Vec2(_velocity.x, MTV);

        if (_report.OnLeftWall && _velocity.x < -MTV)
            _velocity = new Vec2(-MTV, _velocity.y);

        if (_report.OnRightWall && _velocity.x > MTV)
            _velocity = new Vec2(MTV, _velocity.y);

        // Generate report & update position

        SetWill(Center, Center + _velocity * Common.FixedTime);
        _report = _physics.MoveCollider(this);
        return RemoveOffset(_report);
    }

    public OutputData NoPhysicsUpdate(Vec2 targetPos)
    {
        Vec2 colliderPos = targetPos + Offset;
        _velocity = Vec2.Zero;

        SetWill(Center, colliderPos);
        _report = new OutputData { Position = colliderPos };
        return RemoveOffset(_report);
    }

    internal void SetWill(Vec2 oldCenter, Vec2 newCenter)
    {
        OldCenter = oldCenter;
        Center = newCenter;
    }

    private OutputData RemoveOffset(OutputData odata)
    {
        return new OutputData(odata)
        {
            Position = odata.Position - Offset
        };
    }
}
