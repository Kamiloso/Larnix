using Larnix.Core.Binary;
using Larnix.Core.Misc;
using Larnix.Core.Enums;

namespace Larnix.Blocks.Structs
{
    public class BlockData2 : IBinary<BlockData2>
    {
        public const int SIZE = sizeof(BlockID) * 2 + sizeof(byte);

        public static BlockData2 Empty => new();

        private BlockData1 _front;
        public BlockData1 Front => _front;

        private BlockData1 _back;
        public BlockData1 Back => _back;

        public BlockData2()
        {
            _front = new();
            _back = new();
        }

        public BlockData2(BlockData1 front, BlockData1 back)
        {
            _front = front ?? new();
            _back = back ?? new();
        }

        public byte[] Serialize()
        {
            return ArrayUtils.MegaConcat(
                Primitives.GetBytes(Front.ID),
                Primitives.GetBytes(Back.ID),
                new byte[] { (byte)(16 * Front.Variant + Back.Variant) }
                );
        }

        public bool Deserialize(byte[] bytes, int offset = 0)
        {
            if (offset + SIZE > bytes.Length)
                return false;

            byte variants = bytes[4 + offset];

            _front = new BlockData1(
                Primitives.FromBytes<BlockID>(bytes, 0 + offset),
                (byte)(variants / 16)
                );
            
            _back = new BlockData1(
                Primitives.FromBytes<BlockID>(bytes, 2 + offset),
                (byte)(variants % 16)
                );

            return true;
        }

        public BlockData2 BinaryCopy() => ((IBinary<BlockData2>)this).BinaryCopy();
        BlockData2 IBinary<BlockData2>.BinaryCopy()
        {
            return new BlockData2
            {
                _front = Front.BinaryCopy(),
                _back = Back.BinaryCopy(),
            };
        }

        public bool BinaryEquals(BlockData2 other) => ((IBinary<BlockData2>)this).BinaryEquals(other);
        bool IBinary<BlockData2>.BinaryEquals(BlockData2 other)
        {
            return Front.BinaryEquals(other.Front) && Back.BinaryEquals(other.Back);
        }

        public BlockData2 DeepCopy()
        {
            return new BlockData2
            {
                _front = Front.DeepCopy(),
                _back = Back.DeepCopy(),
            };
        }

        public long UniqueLong()
        {
            // DO NOT MODIFY THIS METHOD!
            // RESPONSIBLE FOR BLOCK IDENTIFICATION IN CHUNK SERIALIZATION!

            long frontPart = ((long)Front.ID & 0xFFFF) | (((long)Front.Variant & 0xFF) << 16);
            long backPart = (((long)Back.ID & 0xFFFF) | (((long)Back.Variant & 0xFF) << 16)) << 24;

            return frontPart | backPart;
        }
    }
}
