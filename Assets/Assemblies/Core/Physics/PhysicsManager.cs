using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Core.Utils;

namespace Larnix.Core.Physics
{
    public class PhysicsManager
    {
        private const int CHUNK_SIZE = BlockUtils.CHUNK_SIZE;

        private readonly SpatialDictionary<StaticCollider> _staticColliders = new(3f);
        private readonly HashSet<Vec2Int> _activeChunks = new();

        private long _colliderCount = 0;

        public void SetChunkActive(Vec2Int chunk, bool active)
        {
            if(active) _activeChunks.Add(chunk);
            else _activeChunks.Remove(chunk);
        }

        public void AddCollider(StaticCollider collider)
        {
            _staticColliders.Add(collider.Center, collider);
            _colliderCount++;
        }

        public void RemoveColliderByReference(StaticCollider collider)
        {
            _staticColliders.RemoveByReference(collider.Center, collider);
            _colliderCount--;
        }

        public OutputData MoveCollider(DynamicCollider dynCollider)
        {
            OutputData totalReport = new OutputData { Position = dynCollider.Center };
            var list = _staticColliders.Get3x3SectorList(dynCollider.Center);

            Vec2Int middleChunk = BlockUtils.CoordsToBlock(dynCollider.Center, CHUNK_SIZE);
            for (int dx = -1; dx <= 1 ; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    Vec2Int chunk = middleChunk + new Vec2Int(dx, dy);
                    if(!_activeChunks.Contains(chunk))
                    {
                        list.Add(new StaticCollider(
                            BlockUtils.ChunkCenter(chunk),
                            new Vec2(CHUNK_SIZE + 0.01, CHUNK_SIZE + 0.01)
                            ));
                    }
                }

            while (true)
            {
                int? lowestIndex = null;
                (OutputData report, Vec2 hitPosition, double hitTime)? bestReport = null;

                for (int i = 0; i < list.Count; i++)
                {
                    StaticCollider statCollider = list[i];
                    if (statCollider == null) continue;

                    OutputData report = CalculateCollision(
                        startCenter: dynCollider.OldCenter ?? dynCollider.Center,
                        endCenter: dynCollider.Center,
                        movingSize: dynCollider.Size,
                        staticCenter: statCollider.Center,
                        staticSize: statCollider.Size,
                        out Vec2? hitPosition,
                        out double? hitTime
                    );

                    if (hitTime != null && (hitTime < (bestReport?.hitTime ?? double.MaxValue)))
                    {
                        lowestIndex = i;
                        bestReport = (report, hitPosition ?? default, hitTime ?? default);
                    }
                }

                if (lowestIndex != null)
                {
                    dynCollider.SetWill(
                        bestReport?.hitPosition ?? Vec2.Zero,
                        bestReport?.report.Position ?? Vec2.Zero
                    );
                    totalReport.Merge(bestReport?.report ?? default);
                    list[(int)lowestIndex] = null; // removing inside list is expansive --> nulling elements instead
                }
                else break;
            }

            return totalReport;
        }

        private enum Side { Left, Right, Top, Bottom }

        private static OutputData CalculateCollision(
            Vec2 startCenter,
            Vec2 endCenter,
            Vec2 movingSize,
            Vec2 staticCenter,
            Vec2 staticSize,
            out Vec2? hitPosition,
            out double? hitTime)
        {
            hitPosition = null;
            hitTime = null;

            Vec2 moved = endCenter - startCenter;
            Vec2 inflatedSize = movingSize + staticSize;

            Vec2 minCorner = staticCenter - inflatedSize / 2;
            Vec2 maxCorner = staticCenter + inflatedSize / 2;

            if ( // start pos inside rect
                startCenter.x >= minCorner.x && startCenter.x <= maxCorner.x &&
                startCenter.y >= minCorner.y && startCenter.y <= maxCorner.y
                )
                return new();

            (Vec2 collisionPoint, Vec2 endPoint, double collisionTime, Side side)? bestCollision = null;

            if (moved.x != 0.0)
            {
                double tx_min = (minCorner.x - startCenter.x) / moved.x;
                double tx_max = (maxCorner.x - startCenter.x) / moved.x;

                Vec2 point1 = new Vec2(minCorner.x, startCenter.y + tx_min * moved.y);
                Vec2 point2 = new Vec2(maxCorner.x, startCenter.y + tx_max * moved.y);

                if (tx_min >= 0.0 && tx_min <= 1.0 && point1.y > minCorner.y && point1.y < maxCorner.y)
                {
                    if (tx_min < (bestCollision?.collisionTime ?? double.MaxValue))
                        bestCollision = (point1, new Vec2(point1.x, endCenter.y), tx_min, Side.Left);
                }

                if (tx_max >= 0.0 && tx_max <= 1.0 && point2.y > minCorner.y && point2.y < maxCorner.y)
                {
                    if (tx_max < (bestCollision?.collisionTime ?? double.MaxValue))
                        bestCollision = (point2, new Vec2(point2.x, endCenter.y), tx_max, Side.Right);
                }
            }

            if (moved.y != 0.0)
            {
                double ty_min = (minCorner.y - startCenter.y) / moved.y;
                double ty_max = (maxCorner.y - startCenter.y) / moved.y;

                Vec2 point1 = new Vec2(startCenter.x + ty_min * moved.x, minCorner.y);
                Vec2 point2 = new Vec2(startCenter.x + ty_max * moved.x, maxCorner.y);

                if (ty_min >= 0.0 && ty_min <= 1.0 && point1.x > minCorner.x && point1.x < maxCorner.x)
                {
                    if (ty_min < (bestCollision?.collisionTime ?? double.MaxValue))
                        bestCollision = (point1, new Vec2(endCenter.x, point1.y), ty_min, Side.Bottom);
                }

                if (ty_max >= 0.0 && ty_max <= 1.0 && point2.x > minCorner.x && point2.x < maxCorner.x)
                {
                    if (ty_max < (bestCollision?.collisionTime ?? double.MaxValue))
                        bestCollision = (point2, new Vec2(endCenter.x, point2.y), ty_max, Side.Top);
                }
            }

            OutputData report = default;
            if (bestCollision != null)
            {
                Vec2 endPoint = BitChangeVector(bestCollision?.endPoint ?? default, moved);
                Vec2 colPoint = BitChangeVector(bestCollision?.collisionPoint ?? default, moved);

                report = new OutputData
                {
                    Position = endPoint,
                    OnGround = bestCollision?.side == Side.Top,
                    OnCeil = bestCollision?.side == Side.Bottom,
                    OnLeftWall = bestCollision?.side == Side.Right,
                    OnRightWall = bestCollision?.side == Side.Left,
                };

                hitPosition = colPoint;
                hitTime = bestCollision?.collisionTime;
            }
            return report;
        }

        private static Vec2 BitChangeVector(Vec2 vect, Vec2 moved)
        {
            return new Vec2(
                moved.x > 0.0 ? DoubleUtils.BitDecrement(vect.x, 1) : (moved.x < 0.0 ? DoubleUtils.BitIncrement(vect.x, 1) : vect.x),
                moved.y > 0.0 ? DoubleUtils.BitDecrement(vect.y, 1) : (moved.y < 0.0 ? DoubleUtils.BitIncrement(vect.y, 1) : vect.y)
                );
        }
    }
}
