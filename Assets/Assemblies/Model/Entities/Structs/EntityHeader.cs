#nullable enable
using Larnix.Core.Serialization;
using Larnix.Core.Vectors;
using System.Runtime.InteropServices;

namespace Larnix.Model.Entities.Structs;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct EntityHeader : ISanitizable<EntityHeader>
{
    public EntityID Id { get; }
    public Vec2 Position { get; }
    public float Rotation { get; }

    public EntityHeader(EntityID id, Vec2 position, float rotation)
    {
        Id = id;
        Position = position.Sanitize();
        Rotation = float.IsFinite(rotation) ? rotation : 0f;
    }

    public EntityHeader Sanitize()
    {
        return new EntityHeader(Id, Position, Rotation);
    }

    public override string ToString()
    {
        return Id.ToString();
    }
}
