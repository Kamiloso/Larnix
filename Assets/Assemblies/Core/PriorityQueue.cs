using System;
using System.Collections.Generic;

public class PriorityQueue<TElement, TPriority>
{
    private struct Node
    {
        public TElement Element;
        public TPriority Priority;

        public Node(TElement element, TPriority priority)
        {
            Element = element;
            Priority = priority;
        }
    }

    private readonly List<Node> _heap = new();
    private readonly Dictionary<TElement, int> _indexMap = new();
    private readonly IComparer<TPriority> _comparer;

    // Snapshot cache
    private List<TElement> _orderedSnapshot;
    private PriorityQueue<TElement, TPriority> _snapshotHelper = null; // stays empty or null
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
        if (_indexMap.ContainsKey(element))
            throw new InvalidOperationException("Element already exists in queue.");

        int index = _heap.Count;
        _heap.Add(new Node(element, priority));
        _indexMap[element] = index;

        HeapifyUp(index);
        MarkDirty();
    }

    public TElement Dequeue()
    {
        if (_heap.Count == 0)
            throw new InvalidOperationException("Queue is empty.");

        return RemoveAt(0);
    }

    public bool Remove(TElement element)
    {
        if (!_indexMap.TryGetValue(element, out int index))
            return false;

        RemoveAt(index);
        return true;
    }

    public TElement Peek()
    {
        if (_heap.Count == 0)
            throw new InvalidOperationException("Queue is empty.");

        return _heap[0].Element;
    }

    public IReadOnlyList<TElement> OrderedSnapshot()
    {
        if (_snapshotDirty || _orderedSnapshot == null)
        {
            if (_snapshotHelper == null)
                _snapshotHelper = new PriorityQueue<TElement, TPriority>((a, b) => _comparer.Compare(a, b));

            foreach (var node in _heap)
                _snapshotHelper.Enqueue(node.Element, node.Priority);

            _orderedSnapshot = new List<TElement>(_heap.Count);

            while (_snapshotHelper.Count > 0)
                _orderedSnapshot.Add(_snapshotHelper.Dequeue());

            _snapshotDirty = false;
        }

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

        _heap.RemoveAt(lastIndex);
        _indexMap.Remove(removedNode.Element);

        if (index < _heap.Count)
        {
            HeapifyDown(index);
            HeapifyUp(index);
        }

        MarkDirty();
        return removedNode.Element;
    }

    private void HeapifyUp(int index)
    {
        while (index > 0)
        {
            int parent = (index - 1) / 2;

            if (_comparer.Compare(_heap[index].Priority, _heap[parent].Priority) >= 0)
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
                _comparer.Compare(_heap[left].Priority, _heap[smallest].Priority) < 0)
            {
                smallest = left;
            }

            if (right < count &&
                _comparer.Compare(_heap[right].Priority, _heap[smallest].Priority) < 0)
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

        _indexMap[_heap[a].Element] = a;
        _indexMap[_heap[b].Element] = b;
    }
}
