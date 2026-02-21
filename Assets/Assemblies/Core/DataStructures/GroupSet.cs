using System;
using System.Collections.Generic;
using System.Linq;
using Larnix.Core.Utils;

namespace Larnix.Core.DataStructures
{
    public class GroupSet<T>
    {
        private readonly Dictionary<T, HashSet<T>> _groups = new();
        private readonly bool _groupsAlwaysDistinct;

        public GroupSet(bool groupsAlwaysDistinct = false)
        {
            _groupsAlwaysDistinct = groupsAlwaysDistinct;
        }

        public void Clear()
        {
            _groups.Clear();
        }

        public void AddGroup(IEnumerable<T> items)
        {
            HashSet<T> newGroup = new(items);

            foreach (T item in items)
            {
                if (_groups.TryGetValue(item, out var group))
                {
                    if (_groupsAlwaysDistinct)
                        throw new InvalidOperationException($"Item {item} already belongs to a group.");

                    group.Remove(item);
                    _groups.Remove(item);
                }
                _groups[item] = newGroup;
            }
        }

        public bool TryGetGroup(T item, out IEnumerable<T> group)
        {
            if (_groups.TryGetValue(item, out var group1))
            {
                group = group1;
                return true;
            }
            else
            {
                group = default;
                return false;
            }
        }
    }
}
