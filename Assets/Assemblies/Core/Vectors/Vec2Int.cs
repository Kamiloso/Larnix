using System;
using Larnix.Core.Utils;
using Larnix.Core.Binary;

namespace Larnix.Core.Vectors
{
    public struct Vec2Int : IEquatable<Vec2Int>, IBinary<Vec2Int>
    {
        public const int SIZE = sizeof(int) * 2;

        public int x { get; private set; }
        public int y { get; private set; }

        public Vec2Int(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public Vec2 ToVec2()
        {
            return new Vec2(x, y);
        }

        public int ManhattanDistance(Vec2Int other)
        {
            return GeometryUtils.ManhattanDistance(this, other);
        }

        public byte[] Serialize()
        {
            return ArrayUtils.MegaConcat(
                Primitives.GetBytes(x),
                Primitives.GetBytes(y)
            );
        }

        public bool Deserialize(byte[] bytes, int offset = 0)
        {
            if (offset + SIZE > bytes.Length)
                return false;

            x = Primitives.FromBytes<int>(bytes, offset);
            y = Primitives.FromBytes<int>(bytes, offset + 4);

            return true;
        }

        public double Magnitude => ToVec2().Magnitude;
        public double SqrMagnitude => ToVec2().SqrMagnitude;

        public static Vec2Int Zero => new Vec2Int(0, 0);
        public static Vec2Int One => new Vec2Int(1, 1);

        public static Vec2Int Up => new Vec2Int(0, 1);
        public static Vec2Int Down => new Vec2Int(0, -1);
        public static Vec2Int Left => new Vec2Int(-1, 0);
        public static Vec2Int Right => new Vec2Int(1, 0);

        public static Vec2Int[] CardinalDirections => new[] { Up, Right, Down, Left };

        public override string ToString() => $"({x}, {y})";
        public static implicit operator string(Vec2Int value) => value.ToString();

        public static Vec2Int operator +(Vec2Int a, Vec2Int b) => new Vec2Int(a.x + b.x, a.y + b.y);
        public static Vec2Int operator -(Vec2Int a, Vec2Int b) => new Vec2Int(a.x - b.x, a.y - b.y);
        public static Vec2Int operator *(Vec2Int a, int scalar) => new Vec2Int(a.x * scalar, a.y * scalar);
        public static Vec2Int operator *(int scalar, Vec2Int a) => new Vec2Int(a.x * scalar, a.y * scalar);
        public static Vec2Int operator /(Vec2Int a, int scalar) => new Vec2Int(a.x / scalar, a.y / scalar);
        public static Vec2Int operator -(Vec2Int a) => new Vec2Int(-a.x, -a.y);
        public static bool operator ==(Vec2Int lhs, Vec2Int rhs) => lhs.x == rhs.x && lhs.y == rhs.y;
        public static bool operator !=(Vec2Int lhs, Vec2Int rhs) => !(lhs == rhs);

        public override bool Equals(object obj) => obj is Vec2Int v && Equals(v);
        public bool Equals(Vec2Int other) => x == other.x && y == other.y;

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + x.GetHashCode();
                hash = hash * 31 + y.GetHashCode();
                return hash;
            }
        }
    }
}