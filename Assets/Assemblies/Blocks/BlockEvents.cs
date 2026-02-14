using System;
using System.Collections;
using Larnix.Blocks.Structs;
using Larnix.Core.Utils;

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
        private record EventInfo(BlockEvent Type, ChunkIterator.Order Order);

        private EventInfo[] _blockEvents;
        private BlockServer[,] _blocksFront;
        private BlockServer[,] _blocksBack;

        public BlockEvents(BlockServer[,] blocksFront, BlockServer[,] blocksBack)
        {
            if (blocksFront.GetLength(0) != CHUNK_SIZE || blocksFront.GetLength(1) != CHUNK_SIZE ||
                blocksBack.GetLength(0) != CHUNK_SIZE || blocksBack.GetLength(1) != CHUNK_SIZE)
            {
                throw new ArgumentException($"Block arrays must be of size {CHUNK_SIZE} x {CHUNK_SIZE}!");
            }

            _blocksFront = blocksFront;
            _blocksBack = blocksBack;

            _blockEvents = new[] {
                new EventInfo(BlockEvent.PreFrame, ChunkIterator.Order.YX),
                new EventInfo(BlockEvent.PreFrameSelfMutations, ChunkIterator.Order.YX),
                new EventInfo(BlockEvent.Conway, ChunkIterator.Order.YX),
                new EventInfo(BlockEvent.Sequential, ChunkIterator.Order.YX),
                new EventInfo(BlockEvent.Random, ChunkIterator.Order.Random),
                new EventInfo(BlockEvent.ElectricPropagation, ChunkIterator.Order.YX),
                new EventInfo(BlockEvent.ElectricFinalize, ChunkIterator.Order.YX),
                new EventInfo(BlockEvent.ElectricDevices, ChunkIterator.Order.YX),
                new EventInfo(BlockEvent.SequentialLate, ChunkIterator.Order.YX),
                new EventInfo(BlockEvent.RandomLate, ChunkIterator.Order.Random),
                new EventInfo(BlockEvent.TechCmdExecute, ChunkIterator.Order.YX)
            };
        }

        public IEnumerable GetFrameInvoker()
        {
            foreach (var pos in ChunkIterator.Iterate(ChunkIterator.Order.Any)) // START FRAME
            {
                _blocksBack[pos.x, pos.y].EventFlag = true;
                _blocksFront[pos.x, pos.y].EventFlag = true;
            }
            yield return null;

            foreach (var (type, order) in _blockEvents) // EXECUTE EVENTS
            {
                foreach (var pos in ChunkIterator.Iterate(order))
                {
                    _blocksBack[pos.x, pos.y].InvokeEvent(type);
                }
                yield return null;

                foreach (var pos in ChunkIterator.Iterate(order))
                {
                    _blocksFront[pos.x, pos.y].InvokeEvent(type);
                }
                yield return null;
            }

            foreach (var pos in ChunkIterator.Iterate(ChunkIterator.Order.Any)) // END FRAME
            {
                _blocksBack[pos.x, pos.y].EventFlag = false;
                _blocksFront[pos.x, pos.y].EventFlag = false;
            }
            yield break;
        }
    }
}
