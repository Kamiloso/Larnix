using Larnix.Blocks;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

namespace Larnix.Physics
{
    public class PhysicsManager : MonoBehaviour
    {
        private readonly SpatialDictionary<StaticCollider> StaticColliders = new(3f);
        private readonly HashSet<Vector2Int> ActiveChunks = new();
        public int TotalColliders = 0;

        private void Awake()
        {
            if (gameObject.scene.name == "Client")
                Client.References.PhysicsManager = this;

            if (gameObject.scene.name == "Server")
                Server.References.PhysicsManager = this;
        }

        public void SetChunkActive(Vector2Int chunk, bool active)
        {
            if(active) ActiveChunks.Add(chunk);
            else ActiveChunks.Remove(chunk);
        }

        public void AddCollider(StaticCollider collider)
        {
            StaticColliders.Add(collider.Center, collider);
            TotalColliders++;
        }

        public void RemoveColliderByReference(StaticCollider collider)
        {
            StaticColliders.RemoveByReference(collider.Center, collider);
            TotalColliders--;
        }

        public PhysicsReport MoveCollider(DynamicCollider dynCollider)
        {
            PhysicsReport totalReport = new PhysicsReport { Position = dynCollider.Center };
            List<StaticCollider> list = StaticColliders.Get3x3SectorList(dynCollider.Center);

            Vector2Int middleChunk = ChunkMethods.CoordsToChunk(dynCollider.Center);
            for (int dx = -1; dx <= 1 ; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    Vector2Int chunk = middleChunk + new Vector2Int(dx, dy);
                    if(!ActiveChunks.Contains(chunk))
                    {
                        list.Add(new StaticCollider(
                            ChunkMethods.GlobalBlockCoords(chunk, new Vector2Int(0, 0)) + new Vector2(7.5f, 7.5f),
                            new Vector2(16.1f, 16.1f)
                            ));
                    }
                }

            while (true)
            {
                int? lowestIndex = null;
                (PhysicsReport report, Vector2 hitPosition, float hitTime)? lowestReportPlus = null;

                for (int i = 0; i < list.Count; i++)
                {
                    StaticCollider statCollider = list[i];
                    if(statCollider == null) continue; // removing inside list is expansive, so I'm nulling elements instead

                    PhysicsReport report = CalculateCollision(
                        startCenter: dynCollider.OldCenter ?? dynCollider.Center,
                        endCenter: dynCollider.Center,
                        movingSize: dynCollider.Size,
                        staticCenter: statCollider.Center,
                        staticSize: statCollider.Size,
                        out Vector2? hitPosition,
                        out float? hitTime
                    );

                    if (hitTime != null && (hitTime < (lowestReportPlus?.hitTime ?? float.MaxValue)))
                    {
                        lowestIndex = i;
                        lowestReportPlus = (report, hitPosition ?? Vector2.zero, hitTime ?? 0f);
                    }
                }

                if (lowestIndex != null)
                {
                    dynCollider.SetWill(
                        lowestReportPlus?.hitPosition ?? Vector2.zero,
                        lowestReportPlus?.report.Position ?? Vector2.zero
                    );
                    totalReport.Merge(lowestReportPlus?.report ?? new());
                }
                else break;
            }

            return totalReport;
        }

        private static PhysicsReport CalculateCollision(
            Vector2 startCenter,
            Vector2 endCenter,
            Vector2 movingSize,
            Vector2 staticCenter,
            Vector2 staticSize,
            out Vector2? hitPosition,
            out float? hitTime)
        {
            hitPosition = null;
            hitTime = null;

            Vector2 moved = endCenter - startCenter;
            Vector2 inflatedSize = movingSize + staticSize;

            Vector2 minCorner = staticCenter - inflatedSize / 2;
            Vector2 maxCorner = staticCenter + inflatedSize / 2;

            if ( // start pos inside rect
                startCenter.x >= minCorner.x && startCenter.x <= maxCorner.x &&
                startCenter.y >= minCorner.y && startCenter.y <= maxCorner.y
                )
                return new();

            (Vector2 collisionPoint, Vector2 endPoint, float collisionTime, Side side)? bestCollision = null;

            if (moved.x != 0f)
            {
                float tx_min = (minCorner.x - startCenter.x) / moved.x;
                float tx_max = (maxCorner.x - startCenter.x) / moved.x;

                Vector2 point1 = new Vector2(minCorner.x, startCenter.y + tx_min * moved.y);
                Vector2 point2 = new Vector2(maxCorner.x, startCenter.y + tx_max * moved.y);

                if (tx_min >= 0f && tx_min <= 1f && point1.y > minCorner.y && point1.y < maxCorner.y)
                {
                    if (tx_min < (bestCollision?.collisionTime ?? float.MaxValue))
                        bestCollision = (point1, new Vector2(point1.x, endCenter.y), tx_min, Side.Left);
                }

                if (tx_max >= 0f && tx_max <= 1f && point2.y > minCorner.y && point2.y < maxCorner.y)
                {
                    if(tx_max < (bestCollision?.collisionTime ?? float.MaxValue))
                        bestCollision = (point2, new Vector2(point2.x, endCenter.y), tx_max, Side.Right);
                }
            }

            if (moved.y != 0f)
            {
                float ty_min = (minCorner.y - startCenter.y) / moved.y;
                float ty_max = (maxCorner.y - startCenter.y) / moved.y;

                Vector2 point1 = new Vector2(startCenter.x + ty_min * moved.x, minCorner.y);
                Vector2 point2 = new Vector2(startCenter.x + ty_max * moved.x, maxCorner.y);

                if (ty_min >= 0f && ty_min <= 1f && point1.x > minCorner.x && point1.x < maxCorner.x)
                {
                    if(ty_min < (bestCollision?.collisionTime ?? float.MaxValue))
                        bestCollision = (point1, new Vector2(endCenter.x, point1.y), ty_min, Side.Bottom);
                }

                if (ty_max >= 0f && ty_max <= 1f && point2.x > minCorner.x && point2.x < maxCorner.x)
                {
                    if(ty_max < (bestCollision?.collisionTime ?? float.MaxValue))
                        bestCollision = (point2, new Vector2(endCenter.x, point2.y), ty_max, Side.Top);
                }
            }

            PhysicsReport report = new();
            if (bestCollision != null)
            {
                Vector2 endPoint = BitChangeVector(bestCollision?.endPoint ?? Vector2.zero, moved);
                Vector2 colPoint = BitChangeVector(bestCollision?.collisionPoint ?? Vector2.zero, moved);

                report.Position = endPoint;
                report.OnGround = bestCollision?.side == Side.Top;
                report.OnCeil = bestCollision?.side == Side.Bottom;
                report.OnLeftWall = bestCollision?.side == Side.Right;
                report.OnRightWall = bestCollision?.side == Side.Left;

                hitPosition = colPoint;
                hitTime = bestCollision?.collisionTime;
            }
            return report;
        }

        private static Vector2 BitChangeVector(Vector2 vect, Vector2 moved)
        {
            return new Vector2(
                moved.x > 0 ? FloatUtils.BitDecrement(vect.x, 1) : (moved.x < 0 ? FloatUtils.BitIncrement(vect.x, 1) : vect.x),
                moved.y > 0 ? FloatUtils.BitDecrement(vect.y, 1) : (moved.y < 0 ? FloatUtils.BitIncrement(vect.y, 1) : vect.y)
                );
        }
    }
}
