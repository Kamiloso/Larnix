using System.Collections;
using Larnix.Core.Utils;
using Larnix.Core.Binary;

namespace Larnix.Blocks.Structs
{
    public class BlockData2 : IBinary<BlockData2>
    {
        public const int SIZE = sizeof(BlockID) * 2 + sizeof(byte);

        private BlockData1 _front, _back;
        public BlockData1 Front
        {
            get => _front;
            private set => _front = value ?? new();
        }
        public BlockData1 Back
        {
            get => _back;
            private set => _back = value ?? new();
        }

        public BlockData2()
        {
            Front = new();
            Back = new();
        }

        public BlockData2(BlockData1 front, BlockData1 back)
        {
            Front = front ?? new();
            Back = back ?? new();
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

            Front = new BlockData1(
                Primitives.FromBytes<BlockID>(bytes, 0 + offset),
                (byte)(variants / 16)
                );
            
            Back = new BlockData1(
                Primitives.FromBytes<BlockID>(bytes, 2 + offset),
                (byte)(variants % 16)
                );

            return true;
        }

        public BlockData2 DeepCopy()
        {
            return new BlockData2
            {
                Front = Front.DeepCopy(),
                Back = Back.DeepCopy(),
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
