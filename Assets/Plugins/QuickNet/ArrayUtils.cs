using System;
using System.Linq;

namespace QuickNet
{
    public static class ArrayUtils
    {
        public static T[] MegaConcat<T>(params T[][] arrays)
        {
            int totalLength = arrays.Sum(arr => arr.Length);
            T[] result = new T[totalLength];

            int offset = 0;
            foreach (var arr in arrays)
            {
                Array.Copy(arr, 0, result, offset, arr.Length);
                offset += arr.Length;
            }

            return result;
        }
    }
}
