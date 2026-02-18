using System;
using System.Collections;
using Larnix.Blocks.Structs;
using Larnix.Core.Utils;
using Larnix.Core.Vectors;
using System.Collections.Generic;

namespace Larnix.Blocks
{
    public enum BlockOrder
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
        private record EventInfo(BlockOrder Type, IterationOrder Order);

        private static readonly EventInfo[] _blockEvents = new[]
        {
            new EventInfo(BlockOrder.PreFrame, IterationOrder.YX),
            new EventInfo(BlockOrder.PreFrameSelfMutations, IterationOrder.YX),
            new EventInfo(BlockOrder.Conway, IterationOrder.YX),
            new EventInfo(BlockOrder.Sequential, IterationOrder.YX),
            new EventInfo(BlockOrder.Random, IterationOrder.Random),
            new EventInfo(BlockOrder.ElectricPropagation, IterationOrder.YX),
            new EventInfo(BlockOrder.ElectricFinalize, IterationOrder.YX),
            new EventInfo(BlockOrder.ElectricDevices, IterationOrder.YX),
            new EventInfo(BlockOrder.SequentialLate, IterationOrder.YX),
            new EventInfo(BlockOrder.RandomLate, IterationOrder.Random),
            new EventInfo(BlockOrder.TechCmdExecute, IterationOrder.YX)
        };

        private readonly Block[,] _blocksFront;
        private readonly Block[,] _blocksBack;

        private readonly PriorityQueue<Action, (Vec2Int, bool)>[] _eventActions =
            new PriorityQueue<Action, (Vec2Int, bool)>[_blockEvents.Length];

        public BlockEvents(Block[,] blocksFront, Block[,] blocksBack)
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
                bool isRandom = order == IterationOrder.Random;
                _eventActions[(int)type] = new PriorityQueue<Action, (Vec2Int, bool)>((a, b) =>
                {
                    if (a.Item2 != b.Item2)
                        return a.Item2 ? 1 : -1; // back before front
                    
                    return ChunkIterator.Compare(a.Item1, b.Item1,
                        isRandom ? IterationOrder.XY : order, true);
                });
            }
        }

        public void Subscribe(Vec2Int pos, bool front, BlockOrder type, Action action)
        {
            var queue = _eventActions[(int)type];
            queue.Enqueue(action, (pos, front));
        }

        public void Unsubscribe(BlockOrder type, Action action)
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
                foreach (var action in _eventActions[(int)type].OrderedSnapshot(
                    shuffle: order == IterationOrder.Random
                ))
                {
                    action();
                }
            }

            yield break; // END FRAME (no need to reset flag)
        }
    }
}
