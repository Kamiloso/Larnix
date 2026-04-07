#nullable enable
using Larnix.Core.Vectors;
using Larnix.Model.Physics.Collections;
using Larnix.Model.Physics.Structs;
using Larnix.Model.Utils;
using System.Collections.Generic;

namespace Larnix.Model.Physics;

public interface IPhysics
{
    OutputData TickPhysics(DynamicCollider dynCollider, InputData inputData);
    OutputData TickNoPhysics(DynamicCollider dynCollider, Vec2 targetPos);
}

public interface IPhysicsManager : IPhysics
{
    long ColliderCount { get; }

    void EnableChunk(Vec2Int chunk);
    void DisableChunk(Vec2Int chunk);
    void AddCollider(StaticCollider collider);
    void RemoveColliderByReference(StaticCollider collider);
}

public class PhysicsManager : IPhysicsManager
{
    private const int CHUNK_SIZE = BlockUtils.CHUNK_SIZE;

    private readonly SpatialDictionary<StaticCollider> _staticColliders;
    private readonly HashSet<Vec2Int> _activeChunks = new();

    public long ColliderCount { get; private set; }

    public PhysicsManager(double sectorSize)
    {
        _staticColliders = new SpatialDictionary<StaticCollider>(sectorSize);
    }

    public void EnableChunk(Vec2Int chunk) => _activeChunks.Add(chunk);
    public void DisableChunk(Vec2Int chunk) => _activeChunks.Remove(chunk);

    public void AddCollider(StaticCollider collider)
    {
        _staticColliders.Add(collider.Center, collider);
        ColliderCount++;
    }

    public void RemoveColliderByReference(StaticCollider collider)
    {
        _staticColliders.RemoveByReference(collider.Center, collider);
        ColliderCount--;
    }

    public OutputData TickPhysics(DynamicCollider dynCollider, InputData inputData)
    {
        List<StaticCollider?> list = _staticColliders.Get3x3SectorList(dynCollider.Center)!;

        Vec2Int middleChunk = BlockUtils.CoordsToBlock(dynCollider.Center, CHUNK_SIZE);
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                Vec2Int chunk = middleChunk + new Vec2Int(dx, dy);
                if (!_activeChunks.Contains(chunk))
                {
                    list.Add(new StaticCollider(
                        BlockUtils.ChunkCenter(chunk),
                        new Vec2(CHUNK_SIZE + 0.01, CHUNK_SIZE + 0.01)
                        ));
                }
            }

        return dynCollider.PhysicsUpdate(inputData, list);
    }

    public OutputData TickNoPhysics(DynamicCollider dynCollider, Vec2 targetPos)
    {
        return dynCollider.NoPhysicsUpdate(targetPos);
    }
}
