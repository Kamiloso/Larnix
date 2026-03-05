using System;
using System.Collections.Generic;
using System.Linq;

namespace Larnix.Core.Utils
{
    public static class LinkedListExtensions
    {
        public static void ForEachRemove<T>(this LinkedList<T> list, Predicate<T> predicate, Action<T> action = null)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

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

        public static T First<T>(this LinkedList<T> list) where T: class
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            return list.First.Value;
        }

        public static T Last<T>(this LinkedList<T> list) where T : class
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            return list.Last.Value;
        }

        public static bool TryPopFirst<T>(this LinkedList<T> list, out T value)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Count == 0)
            {
                value = default;
                return false;
            }
            value = list.First.Value;
            list.RemoveFirst();
            return true;
        }

        public static bool TryPopLast<T>(this LinkedList<T> list, out T value)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Count == 0)
            {
                value = default;
                return false;
            }
            value = list.Last.Value;
            list.RemoveLast();
            return true;
        }

        public static double Median(this LinkedList<long> list)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            int count = list.Count;
            if (count == 0)
                throw new InvalidOperationException("Empty LinkedList");

            var arr = list.ToArray();
            Array.Sort(arr);

            if (count % 2 == 0)
            {
                long mid1 = arr[count / 2 - 1];
                long mid2 = arr[count / 2];
                long diff = mid2 - mid1;
                return mid1 + diff / 2.0;
            }
            else
            {
                return arr[count / 2];
            }
        }
    }
}
