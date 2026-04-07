#nullable enable
using Larnix.Core;
using Larnix.Core.Vectors;
using System.Runtime.InteropServices;

namespace Larnix.Model.Entities.Structs;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct EntityHeader : IFixedStruct<EntityHeader>
{
    private readonly EntityID _id;
    private readonly Vec2 _position;
    private readonly float _rotation;

    public EntityID ID => _id;
    public Vec2 Position => _position;
    public float Rotation => float.IsFinite(_rotation) ? _rotation : 0f;

    public EntityHeader(EntityID id, Vec2 position, float rotation)
    {
        _id = id;
        _position = position;
        _rotation = rotation;
    }

    public override string ToString()
    {
        return ID.ToString();
    }
}
