#nullable enable

namespace Larnix.Core.Serialization;

public interface ISanitizable<T> where T : unmanaged
{
    T Sanitize();
}

public static class Sanitizer
{
    public static T Filter<T>(in T value) where T : unmanaged
    {
        return AutoSanitizer<T>.Filter(in value);
    }

    public static float ToFinite(float value)
    {
        return float.IsFinite(value) ? value : 0;
    }

    public static double ToFinite(double value)
    {
        return double.IsFinite(value) ? value : 0;
    }

    public static byte ToHalfByte(byte value)
    {
        return (byte)(value % 16);
    }

    public static T FillAndFilter<T, U>(T buffer, U filler)
        where T : unmanaged, IFixedBuffer<U>
        where U : unmanaged
    {
        while (!buffer.IsFull)
        {
            buffer.Add(filler);
        }

        return Filter(buffer);
    }
}
