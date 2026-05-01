#nullable enable
using Larnix.Core.Serialization;
using Larnix.Core.Vectors;
using System.Runtime.InteropServices;

namespace Larnix.Model.Entities.Structs;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct EntityHeader : ISanitizable<EntityHeader>
{
    public EntityID Id { get; }
    public Vec2 Position { get; }
    public float Rotation { get; }

    public EntityHeader(EntityID id, Vec2 position, float rotation)
    {
        Id = Sanitizer.Filter(id);
        Position = Sanitizer.Filter(position);
        Rotation = Sanitizer.ToFinite(rotation);
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
