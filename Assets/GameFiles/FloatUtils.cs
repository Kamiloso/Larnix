using System;

public static class FloatUtils
{
    public static float BitIncrement(float x, int iterations = 1)
    {
        if (iterations <= 0) return x;

        if (float.IsNaN(x)) return x;
        if (x == float.PositiveInfinity) return x;

        if (x == 0f) return float.Epsilon;

        int bits = BitConverter.SingleToInt32Bits(x);

        if (x > 0)
        {
            bits++;
        }
        else
        {
            bits--;
        }

        return BitIncrement(BitConverter.Int32BitsToSingle(bits), iterations - 1);
    }

    public static float BitDecrement(float x, int iterations = 1)
    {
        if(iterations <= 0) return x;

        if (float.IsNaN(x)) return x;
        if (x == float.NegativeInfinity) return x;

        if (x == 0f) return -float.Epsilon;

        int bits = BitConverter.SingleToInt32Bits(x);

        if (x > 0)
        {
            bits--;
        }
        else
        {
            bits++;
        }

        return BitDecrement(BitConverter.Int32BitsToSingle(bits), iterations - 1);
    }
}
