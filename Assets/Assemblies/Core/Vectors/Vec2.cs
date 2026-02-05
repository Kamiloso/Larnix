using System;
using Larnix.Core.Utils;
using Larnix.Core.Binary;

namespace Larnix.Core.Vectors
{
    public struct Vec2 : IEquatable<Vec2>, IBinary<Vec2>
    {
        public const int SIZE = sizeof(double) * 2;
        
        public double x { get; private set; }
        public double y { get; private set; }

        public Vec2(double x, double y)
        {
            this.x = double.IsFinite(x) ? x : 0.0;
            this.y = double.IsFinite(y) ? y : 0.0;
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

            double _x = Primitives.FromBytes<double>(bytes, offset);
            double _y = Primitives.FromBytes<double>(bytes, offset + 8);

            x = double.IsFinite(_x) ? _x : 0.0;
            y = double.IsFinite(_y) ? _y : 0.0;

            return true;
        }

        public static Vec2 Zero => new Vec2(0, 0);
        public static Vec2 One => new Vec2(1, 1);
        public double Magnitude => Math.Sqrt(x * x + y * y);
        public double SqrMagnitude => x * x + y * y;
        public static double Distance(Vec2 a, Vec2 b) => (a - b).Magnitude;

        public Vec2 Normalized
        {
            get
            {
                var mag = Magnitude;
                return mag > 0 ? this / mag : new Vec2(0, 0);
            }
        }

        public override string ToString() => $"({x}, {y})";
        public static implicit operator string(Vec2 value) => value.ToString();

        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.x + b.x, a.y + b.y);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.x - b.x, a.y - b.y);
        public static Vec2 operator *(Vec2 a, double s) => new Vec2(a.x * s, a.y * s);
        public static Vec2 operator *(double s, Vec2 a) => new Vec2(a.x * s, a.y * s);
        public static Vec2 operator /(Vec2 a, double s) => new Vec2(a.x / s, a.y / s);
        public static Vec2 operator -(Vec2 a) => new Vec2(-a.x, -a.y);
        public static bool operator ==(Vec2 a, Vec2 b) => a.Equals(b);
        public static bool operator !=(Vec2 a, Vec2 b) => !a.Equals(b);

        public override bool Equals(object obj) => obj is Vec2 v && Equals(v);
        public bool Equals(Vec2 other) => x.Equals(other.x) && y.Equals(other.y);

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
