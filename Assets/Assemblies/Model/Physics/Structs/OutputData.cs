#nullable enable
using Larnix.Core.Vectors;

namespace Larnix.Model.Physics.Structs;

public readonly struct OutputData
{
    public Vec2 Position { get; init; }
    public bool OnGround { get; init; }
    public bool OnCeil { get; init; }
    public bool OnLeftWall { get; init; }
    public bool OnRightWall { get; init; }

    public OutputData Merge(in OutputData other)
    {
        return other with
        {
            OnGround = OnGround || other.OnGround,
            OnCeil = OnCeil || other.OnCeil,
            OnLeftWall = OnLeftWall || other.OnLeftWall,
            OnRightWall = OnRightWall || other.OnRightWall
        };
    }
}
