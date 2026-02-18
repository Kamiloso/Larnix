using System;
using System.Collections;
using Larnix.Blocks.Structs;
using Larnix.Core.Utils;
using Larnix.Core.Vectors;
using System.Collections.Generic;
using System.Linq;
using Larnix.Blocks.All;

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

        private readonly Vec2Int _chunkpos;
        private readonly IWorldAPI _worldAPI;
        private readonly Block[,] _blocksFront;
        private readonly Block[,] _blocksBack;

        private class ActionPair
        {
            public Action Action { get; }
            public bool IsSecAtomic { get; }
            
            public ActionPair(Action action, bool isSecAtomic)
            {
                Action = action ?? throw new ArgumentNullException(nameof(action));
                IsSecAtomic = isSecAtomic;
            }

            public static bool operator ==(ActionPair a, ActionPair b) => ReferenceEquals(a.Action, b.Action);
            public static bool operator !=(ActionPair a, ActionPair b) => !(a == b);

            public override bool Equals(object obj)
            {
                if (obj is ActionPair other)
                {
                    return ReferenceEquals(Action, other.Action);
                }
                return false;
            }

            public override int GetHashCode()
            {
                return Action.GetHashCode();
            }
        }

        private readonly PriorityQueue<ActionPair, (Vec2Int, bool)>[] _eventActions =
            new PriorityQueue<ActionPair, (Vec2Int, bool)>[_blockEvents.Length];

        public BlockEvents(Vec2Int chunkpos, IWorldAPI worldAPI, Block[,] blocksFront, Block[,] blocksBack)
        {
            if (blocksFront.GetLength(0) != CHUNK_SIZE || blocksFront.GetLength(1) != CHUNK_SIZE ||
                blocksBack.GetLength(0) != CHUNK_SIZE || blocksBack.GetLength(1) != CHUNK_SIZE)
            {
                throw new ArgumentException($"Block arrays must be of size {CHUNK_SIZE} x {CHUNK_SIZE}!");
            }

            _chunkpos = chunkpos;
            _worldAPI = worldAPI;
            _blocksFront = blocksFront;
            _blocksBack = blocksBack;

            foreach (var (type, order) in _blockEvents)
            {
                bool isRandom = order == IterationOrder.Random;
                _eventActions[(int)type] = new PriorityQueue<ActionPair, (Vec2Int, bool)>((a, b) =>
                {
                    if (a.Item2 != b.Item2)
                        return a.Item2 ? 1 : -1; // back before front
                    
                    return ChunkIterator.Compare(a.Item1, b.Item1,
                        isRandom ? IterationOrder.XY : order, true);
                });
            }
        }

        public void Subscribe(Vec2Int pos, bool front, BlockOrder type, Action action, bool isSecAtomic)
        {
            var queue = _eventActions[(int)type];
            var toAdd = new ActionPair(action, isSecAtomic);
            queue.Enqueue(toAdd, (pos, front));
        }

        public void Unsubscribe(BlockOrder type, Action action)
        {
            var queue = _eventActions[(int)type];
            var toRemove = new ActionPair(action, default);
            queue.Remove(toRemove);
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
                foreach (var actionPair in _eventActions[(int)type].OrderedSnapshot(
                    shuffle: order == IterationOrder.Random
                ))
                {
                    Action action = actionPair.Action;
                    bool isSecAtomic = actionPair.IsSecAtomic;

                    if (!isSecAtomic || _worldAPI.IsChunkAtomicLoaded(_chunkpos))
                    {
                        action();
                    }
                }
            }

            yield break; // END FRAME (no need to reset flag)
        }
    }
}
