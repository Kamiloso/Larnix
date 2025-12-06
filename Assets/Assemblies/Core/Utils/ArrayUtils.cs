using System;
using System.Linq;

namespace Larnix.Core.Utils
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

        public static T[] AddLeadingZeros<T>(T[] array, int targetLength) where T : new()
        {
            if (array.Length > targetLength)
                throw new ArgumentException(nameof(array));

            return MegaConcat(
                new T[targetLength - array.Length],
                array
                );
        }

        public static T[] RemoveLeadingZeros<T>(T[] array) where T : new()
        {
            int i = 0, lngt = array.Length;
            while (i < lngt && array[i].Equals(new T()))
                i++;

            return array[i..];
        }
    }
}
