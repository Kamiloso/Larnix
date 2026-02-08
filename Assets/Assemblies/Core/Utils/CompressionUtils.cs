using System;
using Larnix.Core.Binary;

namespace Larnix.Core.Utils
{
    public static class CompressionUtils
    {
        // ===== POSITION =====

        public const int COMPRESSED_DOUBLE_SIZE = 5;
        private static readonly double LESS_THAN_256 = DoubleUtils.BitDecrement(256.0);

        public static byte[] CompressWorldDouble(double value) // COMPRESSED_DOUBLE_SIZE bytes
        {
            if (Math.Abs(value) < int.MaxValue)
            {
                int integer = (int)Math.Floor(value);
                double fraction = value - integer; // [0,1]
                byte fracByte = (byte)Math.Clamp(fraction * 256.0, 0.0, LESS_THAN_256); // [0, 256) -> floor by cast

                return ArrayUtils.MegaConcat(
                    Primitives.GetBytes(integer),
                    new[] { fracByte }
                );
            }
            return new byte[5];
        }

        public static double DecompressWorldDouble(byte[] compressed, int offset = 0)
        {
            if (offset + COMPRESSED_DOUBLE_SIZE > compressed.Length)
                throw new ArgumentException("Compressed data is too short.");

            int integer = Primitives.FromBytes<int>(compressed, offset);
            byte fracByte = compressed[offset + 4];
            double fraction = fracByte / 256.0;

            return integer + fraction;
        }

        // ===== ROTATION =====

        public static byte CompressRotation(float angle)
        {
            float normalized = angle % 360f; // [-360, 360]
            if (normalized < 0f)
                normalized += 360f; // [0, 360]

            return (byte)(normalized / 360f * 256f); // floor by cast
        }

        public static float DecompressRotation(byte compressed)
        {
            return compressed * (360f / 256f);
        }
    }
}
