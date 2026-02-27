using System;
using System.Collections.Generic;
using System.Linq;
using Larnix.Core.Utils;

namespace Larnix.Core.DataStructures
{
    public class PriorityQueue<TElement, TPriority>
    {
        private struct Node
        {
            public TElement Element;
            public TPriority Priority;
            public long InsertionIndex;

            public Node(TElement element, TPriority priority, long insertionIndex)
            {
                Element = element;
                Priority = priority;
                InsertionIndex = insertionIndex;
            }
        }

        private readonly List<Node> _heap = new();
        private long _nextInsertionIndex;

        // support duplicates: map element to a set of indices where it appears
        private readonly Dictionary<TElement, HashSet<int>> _indexMap = new();
        private readonly IComparer<TPriority> _comparer;

        private int CompareNodes(Node a, Node b)
        {
            int cmp = _comparer.Compare(a.Priority, b.Priority);
            if (cmp != 0)
                return cmp;
            return a.InsertionIndex.CompareTo(b.InsertionIndex);
        }

        // Snapshot cache
        private List<TElement> _orderedSnapshot;
        private bool _snapshotDirty = true;

        public int Count => _heap.Count;

        public PriorityQueue(Func<TPriority, TPriority, int> compare = null)
        {
            _comparer = compare != null
                ? Comparer<TPriority>.Create((a, b) => compare(a, b))
                : Comparer<TPriority>.Default;
        }

        public void Enqueue(TElement element, TPriority priority)
        {
            int index = _heap.Count;
            _heap.Add(new Node(element, priority, _nextInsertionIndex++));

            if (!_indexMap.TryGetValue(element, out var set))
            {
                set = new HashSet<int>();
                _indexMap[element] = set;
            }
            set.Add(index);

            HeapifyUp(index);
            MarkDirty();
        }

        public bool TryDequeue(out TElement element)
        {
            if (_heap.Count == 0)
            {
                element = default;
                return false;
            }

            element = RemoveAt(0);
            return true;
        }

        public TElement Dequeue()
        {
            if (!TryDequeue(out var element))
                throw new InvalidOperationException("Queue is empty.");

            return element;
        }

        public bool Remove(TElement element)
        {
            if (!_indexMap.TryGetValue(element, out var set) || set.Count == 0)
                return false;

            int index = set.Count > 0 ? set.First() : -1;
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }

            return false;
        }

        public TElement Peek()
        {
            if (_heap.Count == 0)
                throw new InvalidOperationException("Queue is empty.");

            return _heap[0].Element;
        }

        public IReadOnlyList<TElement> OrderedSnapshot(bool shuffle = false)
        {
            if (_snapshotDirty || _orderedSnapshot == null)
            {
                // create a temporary list of nodes and sort by priority then insertion index
                var nodes = new List<Node>(_heap);
                nodes.Sort(CompareNodes);

                _orderedSnapshot = new List<TElement>(nodes.Count);
                foreach (var n in nodes)
                    _orderedSnapshot.Add(n.Element);

                _snapshotDirty = false;
            }

            if (shuffle)
                Shuffle(_orderedSnapshot);

            return _orderedSnapshot;
        }

        private void MarkDirty()
        {
            _snapshotDirty = true;
        }

        private TElement RemoveAt(int index)
        {
            var removedNode = _heap[index];
            int lastIndex = _heap.Count - 1;

            Swap(index, lastIndex);
            RemoveLast();

            if (index < _heap.Count)
            {
                HeapifyDown(index);
                HeapifyUp(index);
            }

            MarkDirty();
            return removedNode.Element;
        }

        private void RemoveLast()
        {
            var lastIndex = _heap.Count - 1;
            var lastNode = _heap[lastIndex];

            if (_indexMap.TryGetValue(lastNode.Element, out var set))
            {
                set.Remove(lastIndex);
                if (set.Count == 0)
                    _indexMap.Remove(lastNode.Element);
            }

            _heap.RemoveAt(lastIndex);
        }

        private void HeapifyUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) / 2;

                if (CompareNodes(_heap[index], _heap[parent]) >= 0)
                    break;

                Swap(index, parent);
                index = parent;
            }
        }

        private void HeapifyDown(int index)
        {
            int count = _heap.Count;

            while (true)
            {
                int left = 2 * index + 1;
                int right = 2 * index + 2;
                int smallest = index;

                if (left < count &&
                    CompareNodes(_heap[left], _heap[smallest]) < 0)
                {
                    smallest = left;
                }

                if (right < count &&
                    CompareNodes(_heap[right], _heap[smallest]) < 0)
                {
                    smallest = right;
                }

                if (smallest == index)
                    break;

                Swap(index, smallest);
                index = smallest;
            }
        }

        private void Swap(int a, int b)
        {
            if (a == b) return;

            var temp = _heap[a];
            _heap[a] = _heap[b];
            _heap[b] = temp;

            var ea = _heap[a].Element;
            var eb = _heap[b].Element;

            var setA = _indexMap[ea];
            var setB = _indexMap[eb];

            // remove old indices
            setA.Remove(b);
            setB.Remove(a);

            // add new indices
            setA.Add(a);
            setB.Add(b);
        }

        private static void Shuffle<T>(IList<T> list)
        {
            var rng = Common.Rand();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }
    }
}
