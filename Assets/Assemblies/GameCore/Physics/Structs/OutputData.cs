using Larnix.Core.Vectors;

namespace Larnix.GameCore.Physics.Structs
{
    public readonly struct OutputData
    {
        public Vec2 Position { get; init; }
        public bool OnGround { get; init; }
        public bool OnCeil { get; init; }
        public bool OnLeftWall { get; init; }
        public bool OnRightWall { get; init; }

        public OutputData(in OutputData original)
        {
            Position = original.Position;
            OnGround = original.OnGround;
            OnCeil = original.OnCeil;
            OnLeftWall = original.OnLeftWall;
            OnRightWall = original.OnRightWall;
        }

        public OutputData Merge(OutputData other)
        {
            return new(other)
            {
                OnGround = OnGround || other.OnGround,
                OnCeil = OnCeil || other.OnCeil,
                OnLeftWall = OnLeftWall || other.OnLeftWall,
                OnRightWall = OnRightWall || other.OnRightWall
            };
        }
    }
}
