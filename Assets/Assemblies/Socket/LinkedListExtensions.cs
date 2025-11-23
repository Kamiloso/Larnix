using System;
using System.Collections.Generic;

namespace Socket
{
    internal static class LinkedListExtensions
    {
        internal static void ForEachRemove<T>(this LinkedList<T> list, Predicate<T> predicate, Action<T> action = null)
        {
            var node = list.First;
            while (node != null)
            {
                var next = node.Next;
                if (predicate(node.Value))
                {
                    if (action != null) action(node.Value);
                    list.Remove(node);
                }
                node = next;
            }
        }

        internal static double Median(this LinkedList<long> list)
        {
            int count = list.Count;
            if (count == 0)
                throw new InvalidOperationException("Empty LinkedList");

            var arr = new long[count];
            int i = 0;
            for (var node = list.First; node != null; node = node.Next)
                arr[i++] = node.Value;

            Array.Sort(arr);

            if (count % 2 == 0)
            {
                return (arr[count / 2 - 1] + arr[count / 2]) / 2.0;
            }
            else
            {
                return arr[count / 2];
            }
        }
    }
}
