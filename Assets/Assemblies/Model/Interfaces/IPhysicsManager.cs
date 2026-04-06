using Larnix.Core.Vectors;
using Larnix.Model.Physics;
using Larnix.Model.Physics.Structs;

namespace Larnix.Model.Interfaces;

public interface IPhysicsManager
{
    long ColliderCount { get; }

    void EnableChunk(Vec2Int chunk);
    void DisableChunk(Vec2Int chunk);
    void AddCollider(StaticCollider collider);
    void RemoveColliderByReference(StaticCollider collider);

    OutputData TickPhysics(DynamicCollider dynCollider, InputData inputData);
    OutputData TickNoPhysics(DynamicCollider dynCollider, Vec2 targetPos);
}
