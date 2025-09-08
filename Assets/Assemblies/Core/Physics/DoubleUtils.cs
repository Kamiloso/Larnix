using System;

namespace Larnix.Core.Physics
{
    public static class DoubleUtils
    {
        public static double BitIncrement(double x, int iterations = 1)
        {
            if (iterations <= 0) return x;

            if (double.IsNaN(x)) return x;
            if (x == double.PositiveInfinity) return x;

            if (x == 0.0) return double.Epsilon;

            long bits = BitConverter.DoubleToInt64Bits(x);

            if (x > 0)
            {
                bits++;
            }
            else
            {
                bits--;
            }

            return BitIncrement(BitConverter.Int64BitsToDouble(bits), iterations - 1);
        }

        public static double BitDecrement(double x, int iterations = 1)
        {
            if (iterations <= 0) return x;

            if (double.IsNaN(x)) return x;
            if (x == double.NegativeInfinity) return x;

            if (x == 0.0) return -double.Epsilon;

            long bits = BitConverter.DoubleToInt64Bits(x);

            if (x > 0)
            {
                bits--;
            }
            else
            {
                bits++;
            }

            return BitDecrement(BitConverter.Int64BitsToDouble(bits), iterations - 1);
        }
    }
}
