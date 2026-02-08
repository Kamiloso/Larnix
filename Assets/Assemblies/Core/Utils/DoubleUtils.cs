using System;
using System.Globalization;

namespace Larnix.Core.Utils
{
    public static class DoubleUtils
    {
        /// <summary>
        /// Safe parse, immune to regional settings (uses dots)
        /// </summary>
        public static double Parse(string value) =>
            double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
        
        /// <summary>
        /// Safe parse, immune to regional settings (uses dots)
        /// </summary>
        public static bool TryParse(string value, out double result) =>
            double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        
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
