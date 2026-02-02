using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Binary;
using Larnix.Core.Utils;

namespace Larnix.Blocks.Structs
{
    public class Item : IBinary<Item>
    {
        public const int SIZE = BlockData1.SIZE + sizeof(int);

        public BlockData1 Block { get; private set; }
        public int Count { get; private set; }

        public Item() => Block = new();
        public Item(BlockData1 block, int count)
        {
            Block = block ?? new();
            Count = count;
        }

        public byte[] Serialize()
        {
            return ArrayUtils.MegaConcat(
                Structures.GetBytes(Block),
                Primitives.GetBytes(Count)
            );
        }

        public bool Deserialize(byte[] bytes, int offset = 0)
        {
            if (offset + SIZE > bytes.Length)
                return false;

            Block = Structures.FromBytes<BlockData1>(bytes, offset);
            offset += BlockData1.SIZE;

            Count = Primitives.FromBytes<int>(bytes, offset);
            offset += sizeof(int);

            return true;
        }

        public Item DeepCopy()
        {
            return new Item
            {
                Block = Block.DeepCopy(),
                Count = Count
            };
        }
    }
}
