using System;
using System.Collections;
using Larnix.Blocks.Structs;
using Larnix.Core.Utils;
using Larnix.Core.Vectors;
using System.Collections.Generic;

namespace Larnix.Blocks
{
    public enum BlockEvent
    {
        PreFrame, PreFrameSelfMutations,
        Conway,
        Sequential, Random,
        ElectricPropagation, ElectricFinalize, ElectricDevices,
        SequentialLate, RandomLate,
        TechCmdExecute,
    }

    public class BlockEvents
    {
        private const int CHUNK_SIZE = BlockUtils.CHUNK_SIZE;
        private record EventInfo(BlockEvent Type, IterationOrder Order);

        private static readonly EventInfo[] _blockEvents = new[]
        {
            new EventInfo(BlockEvent.PreFrame, IterationOrder.YX),
            new EventInfo(BlockEvent.PreFrameSelfMutations, IterationOrder.YX),
            new EventInfo(BlockEvent.Conway, IterationOrder.YX),
            new EventInfo(BlockEvent.Sequential, IterationOrder.YX),
            new EventInfo(BlockEvent.Random, IterationOrder.Random),
            new EventInfo(BlockEvent.ElectricPropagation, IterationOrder.YX),
            new EventInfo(BlockEvent.ElectricFinalize, IterationOrder.YX),
            new EventInfo(BlockEvent.ElectricDevices, IterationOrder.YX),
            new EventInfo(BlockEvent.SequentialLate, IterationOrder.YX),
            new EventInfo(BlockEvent.RandomLate, IterationOrder.Random),
            new EventInfo(BlockEvent.TechCmdExecute, IterationOrder.YX)
        };

        private readonly BlockServer[,] _blocksFront;
        private readonly BlockServer[,] _blocksBack;

        private readonly PriorityQueue<Action, (Vec2Int, bool)>[] _eventActions =
            new PriorityQueue<Action, (Vec2Int, bool)>[_blockEvents.Length];

        public BlockEvents(BlockServer[,] blocksFront, BlockServer[,] blocksBack)
        {
            if (blocksFront.GetLength(0) != CHUNK_SIZE || blocksFront.GetLength(1) != CHUNK_SIZE ||
                blocksBack.GetLength(0) != CHUNK_SIZE || blocksBack.GetLength(1) != CHUNK_SIZE)
            {
                throw new ArgumentException($"Block arrays must be of size {CHUNK_SIZE} x {CHUNK_SIZE}!");
            }

            _blocksFront = blocksFront;
            _blocksBack = blocksBack;

            foreach (var (type, order) in _blockEvents)
            {
                _eventActions[(int)type] = new PriorityQueue<Action, (Vec2Int, bool)>((a, b) =>
                {
                    if (a.Item2 != b.Item2)
                        return a.Item2 ? 1 : -1; // back before front
                    
                    return ChunkIterator.Compare(a.Item1, b.Item1, order);
                });
            }
        }

        public void Subscribe(Vec2Int pos, bool front, BlockEvent type, Action action)
        {
            var queue = _eventActions[(int)type];
            queue.Enqueue(action, (pos, front));
        }

        public void Unsubscribe(BlockEvent type, Action action)
        {
            _eventActions[(int)type]?.Remove(action);
        }

        public IEnumerable GetFrameInvoker()
        {
            ChunkIterator.Iterate((x, y) => // START FRAME
            {
                _blocksBack[x, y].EventFlag = true;
                _blocksFront[x, y].EventFlag = true;
            });

            foreach (var (type, order) in _blockEvents) // EXECUTE EVENTS
            {
                yield return null;
                foreach (var action in _eventActions[(int)type].OrderedSnapshot())
                {
                    action();
                }
            }

            yield break; // END FRAME (no need to reset flag)
        }
    }
}
