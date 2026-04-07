#nullable enable
using Larnix.Core.Vectors;
using Larnix.Model.Physics.Structs;
using Larnix.Model.Utils;
using System;
using System.Collections.Generic;

namespace Larnix.Model.Physics;

public class DynamicCollider
{
    private enum Side { Left, Right, Top, Bottom }
    private record struct CollisionReport(OutputData Report, Vec2 HitPosition, double HitTime);
    private record struct CollisionInfo(Vec2 CollisionPoint, Vec2 FinalPoint, double CollisionTime, Side Side);
    private record struct HitInfo(Vec2 HitPosition, double HitTime, Side Side);

    public PhysicsProperties Properties { get; }
    public Vec2 Offset { get; }
    public Vec2 Size { get; }

    internal Vec2? OldCenter { get; private set; }
    internal Vec2 Center { get; private set; }
    internal Vec2 Velocity { get; private set; }
    internal OutputData Report { get; private set; }

    public DynamicCollider(Vec2 center, Vec2 offset, Vec2 size, PhysicsProperties properties)
    {
        ColliderUtils.AssertSizePositive(size);
        ColliderUtils.AssertSizeWithinLimits(size, offset);

        Properties = properties;
        Offset = offset;
        Size = size;

        OldCenter = null;
        Center = center + offset;

        Velocity = Vec2.Zero;
        Report = new OutputData { Position = Center };
    }

    internal OutputData PhysicsUpdate(InputData inputData, List<StaticCollider?> staticColliders)
    {
        void HorizontalPhysics()
        {
            int wantSide = (inputData.Left ? -1 : 0) + (inputData.Right ? 1 : 0);

            if (wantSide != 0)
            {
                Velocity += wantSide * new Vec2(Properties.ControlForce, 0f);
            }
            else
            {
                int sgn1 = Math.Sign(Velocity.x);
                Velocity -= sgn1 * new Vec2(Properties.HorizontalDrag, 0f);
                int sgn2 = Math.Sign(Velocity.x);

                if (sgn1 != sgn2)
                    Velocity = new Vec2(0f, Velocity.y);
            }
        }

        void VerticalPhysics()
        {
            Velocity -= new Vec2(0, Properties.Gravity);

            if (Report.OnGround)
            {
                if (inputData.Jump)
                {
                    Velocity = new Vec2(Velocity.x, Properties.JumpSize);
                }
            }
        }

        void VelocityClamp()
        {
            if (Math.Abs(Velocity.x) > Properties.MaxHorizontalVelocity)
                Velocity = new Vec2(Math.Sign(Velocity.x) * Properties.MaxHorizontalVelocity, Velocity.y);

            if (Math.Abs(Velocity.y) > Properties.MaxVerticalVelocity)
                Velocity = new Vec2(Velocity.x, Math.Sign(Velocity.y) * Properties.MaxVerticalVelocity);
        }

        void VelocityWallReset()
        {
            const double MAX_TOUCH_VEL = 0.1;

            if (Report.OnGround && Velocity.y < -MAX_TOUCH_VEL)
                Velocity = new Vec2(Velocity.x, -MAX_TOUCH_VEL);

            if (Report.OnCeil && Velocity.y > MAX_TOUCH_VEL)
                Velocity = new Vec2(Velocity.x, MAX_TOUCH_VEL);

            if (Report.OnLeftWall && Velocity.x < -MAX_TOUCH_VEL)
                Velocity = new Vec2(-MAX_TOUCH_VEL, Velocity.y);

            if (Report.OnRightWall && Velocity.x > MAX_TOUCH_VEL)
                Velocity = new Vec2(MAX_TOUCH_VEL, Velocity.y);
        }

        HorizontalPhysics();
        VerticalPhysics();
        VelocityClamp();
        VelocityWallReset();

        OldCenter = Center;
        Center += Velocity * Common.FixedTime;

        Report = MoveCollider(staticColliders);
        return Report with { Position = Report.Position - Offset };
    }

    internal OutputData NoPhysicsUpdate(Vec2 targetPos)
    {
        Vec2 colliderPos = targetPos + Offset;
        Velocity = Vec2.Zero;

        OldCenter = Center;
        Center = colliderPos;

        Report = new OutputData { Position = colliderPos };
        return Report with { Position = Report.Position - Offset };
    }

    private OutputData MoveCollider(List<StaticCollider?> staticColliders)
    {
        OutputData totalReport = new() { Position = Center };

        while (true)
        {
            int lowestIndex = -1;
            CollisionReport? bestReport = null;

            for (int i = 0; i < staticColliders.Count; i++)
            {
                StaticCollider? statCollider = staticColliders[i];
                if (statCollider is null) continue;

                OutputData report = CalculateCollision(
                    startCenter: OldCenter ?? Center,
                    endCenter: Center,
                    movingSize: Size,
                    staticCenter: statCollider.Center,
                    staticSize: statCollider.Size,
                    out HitInfo? hitInfo
                );

                double bestTime = bestReport?.HitTime ?? double.MaxValue;
                if (hitInfo is not null && hitInfo.Value.HitTime < bestTime)
                {
                    lowestIndex = i;
                    bestReport = new CollisionReport
                    {
                        Report = report,
                        HitPosition = hitInfo.Value.HitPosition,
                        HitTime = hitInfo.Value.HitTime 
                    };
                }
            }

            if (bestReport is not null)
            {
                OldCenter = bestReport.Value.HitPosition;
                Center = bestReport.Value.Report.Position;

                totalReport = totalReport.Merge(bestReport.Value.Report);
                staticColliders[lowestIndex] = null; // removing inside list is expansive --> nulling elements instead
            }
            else break;
        }

        return totalReport;
    }

    private static OutputData CalculateCollision(
        Vec2 startCenter, Vec2 endCenter, Vec2 movingSize, Vec2 staticCenter, Vec2 staticSize, out HitInfo? hitInfo
        )
    {
        hitInfo = null;

        Vec2 moved = endCenter - startCenter;
        Vec2 inflatedSize = movingSize + staticSize;

        Vec2 minCorner = staticCenter - inflatedSize / 2;
        Vec2 maxCorner = staticCenter + inflatedSize / 2;

        if ( // start pos inside rect
            startCenter.x >= minCorner.x && startCenter.x <= maxCorner.x &&
            startCenter.y >= minCorner.y && startCenter.y <= maxCorner.y
            )
            return new();

        CollisionInfo? bestCollision = null;

        if (moved.x != 0.0)
        {
            double tx_min = (minCorner.x - startCenter.x) / moved.x;
            double tx_max = (maxCorner.x - startCenter.x) / moved.x;

            Vec2 point1 = new(minCorner.x, startCenter.y + tx_min * moved.y);
            Vec2 point2 = new(maxCorner.x, startCenter.y + tx_max * moved.y);

            if (tx_min >= 0.0 && tx_min <= 1.0 && point1.y > minCorner.y && point1.y < maxCorner.y)
            {
                if (tx_min < (bestCollision?.CollisionTime ?? double.MaxValue))
                    bestCollision = new CollisionInfo(point1, new Vec2(point1.x, endCenter.y), tx_min, Side.Left);
            }

            if (tx_max >= 0.0 && tx_max <= 1.0 && point2.y > minCorner.y && point2.y < maxCorner.y)
            {
                if (tx_max < (bestCollision?.CollisionTime ?? double.MaxValue))
                    bestCollision = new CollisionInfo(point2, new Vec2(point2.x, endCenter.y), tx_max, Side.Right);
            }
        }

        if (moved.y != 0.0)
        {
            double ty_min = (minCorner.y - startCenter.y) / moved.y;
            double ty_max = (maxCorner.y - startCenter.y) / moved.y;

            Vec2 point1 = new(startCenter.x + ty_min * moved.x, minCorner.y);
            Vec2 point2 = new(startCenter.x + ty_max * moved.x, maxCorner.y);

            if (ty_min >= 0.0 && ty_min <= 1.0 && point1.x > minCorner.x && point1.x < maxCorner.x)
            {
                if (ty_min < (bestCollision?.CollisionTime ?? double.MaxValue))
                    bestCollision = new CollisionInfo(point1, new Vec2(endCenter.x, point1.y), ty_min, Side.Bottom);
            }

            if (ty_max >= 0.0 && ty_max <= 1.0 && point2.x > minCorner.x && point2.x < maxCorner.x)
            {
                if (ty_max < (bestCollision?.CollisionTime ?? double.MaxValue))
                    bestCollision = new CollisionInfo(point2, new Vec2(endCenter.x, point2.y), ty_max, Side.Top);
            }
        }

        OutputData report = default;
        if (bestCollision is not null)
        {
            Vec2 endPoint = ApplyEpsilon(bestCollision.Value.FinalPoint, moved);
            Vec2 colPoint = ApplyEpsilon(bestCollision.Value.CollisionPoint, moved);

            report = new OutputData
            {
                Position = endPoint,
                OnGround = bestCollision.Value.Side == Side.Top,
                OnCeil = bestCollision.Value.Side == Side.Bottom,
                OnLeftWall = bestCollision.Value.Side == Side.Right,
                OnRightWall = bestCollision.Value.Side == Side.Left,
            };

            hitInfo = new HitInfo
            {
                HitPosition = colPoint,
                HitTime = bestCollision.Value.CollisionTime,
                Side = bestCollision.Value.Side
            };
        }
        return report;
    }

    private static Vec2 ApplyEpsilon(Vec2 vect, Vec2 moved)
    {
        return new Vec2(
            moved.x > 0.0 ? vect.x - Common.WorldEpsilon.x : moved.x < 0.0 ? vect.x + Common.WorldEpsilon.x : vect.x,
            moved.y > 0.0 ? vect.y - Common.WorldEpsilon.y : moved.y < 0.0 ? vect.y + Common.WorldEpsilon.y : vect.y
            );
    }
}
