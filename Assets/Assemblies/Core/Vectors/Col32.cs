using System;
using Larnix.Core.Utils;
using Larnix.Core.Binary;

namespace Larnix.Core.Vectors
{
    public struct Col32 : IEquatable<Col32>, IBinary<Col32>
    {
        public const int SIZE = sizeof(byte) * 4;

        public byte r { get; private set; }
        public byte g { get; private set; }
        public byte b { get; private set; }
        public byte a { get; private set; }

        public Col32(byte r, byte g, byte b, byte a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public static Col32 Red => new Col32(255, 0, 0, 255);
        public static Col32 Green => new Col32(0, 255, 0, 255);
        public static Col32 Blue => new Col32(0, 0, 255, 255);
        public static Col32 White => new Col32(255, 255, 255, 255);
        public static Col32 Black => new Col32(0, 0, 0, 255);
        public static Col32 Yellow => new Col32(255, 255, 0, 255);
        public static Col32 Cyan => new Col32(0, 255, 255, 255);
        public static Col32 Magenta => new Col32(255, 0, 255, 255);
        public static Col32 Transparent => new Col32(0, 0, 0, 0);

        public byte[] Serialize()
        {
            return new byte[] { r, g, b, a };
        }

        public bool Deserialize(byte[] bytes, int offset = 0)
        {
            if (offset + SIZE > bytes.Length)
                return false;

            r = bytes[offset + 0];
            g = bytes[offset + 1];
            b = bytes[offset + 2];
            a = bytes[offset + 3];

            return true;
        }

        public override string ToString() => $"(R: {r}, G: {g}, B: {b}, A: {a})";

        public override bool Equals(object obj) => obj is Col32 other && Equals(other);

        public bool Equals(Col32 other) => r == other.r && g == other.g && b == other.b && a == other.a;

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + r.GetHashCode();
                hash = hash * 31 + g.GetHashCode();
                hash = hash * 31 + b.GetHashCode();
                hash = hash * 31 + a.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(Col32 lhs, Col32 rhs) => lhs.Equals(rhs);

        public static bool operator !=(Col32 lhs, Col32 rhs) => !(lhs == rhs);

        public static Col32 Lerp(Col32 start, Col32 end, float t = 0.5f)
        {
            t = Math.Clamp(t, 0f, 1f);
            byte lerp(byte a, byte b) => (byte)(a + (b - a) * t);

            return new Col32(
                lerp(start.r, end.r),
                lerp(start.g, end.g),
                lerp(start.b, end.b),
                lerp(start.a, end.a)
            );
        }
    }
}
