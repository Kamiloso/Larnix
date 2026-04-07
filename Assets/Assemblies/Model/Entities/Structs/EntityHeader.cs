#nullable enable
using Larnix.Core;
using Larnix.Core.Vectors;
using System.Runtime.InteropServices;

namespace Larnix.Model.Entities.Structs;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct EntityHeader : IFixedStruct<EntityHeader>
{
    public readonly EntityID ID;
    public readonly Vec2 Position;
    public readonly float Rotation;

    public EntityHeader(EntityID id, Vec2 position, float rotation)
    {
        ID = id;
        Position = position.Sanitize();
        Rotation = float.IsFinite(rotation) ? rotation : 0f;
    }

    public EntityHeader Sanitize() => new(ID, Position, Rotation);

    public override string ToString()
    {
        return ID.ToString();
    }
}
