using System.Collections;
using QuickNet;

namespace Larnix.Blocks
{
    public class BlockData2
    {
        public BlockData1 Front = null;
        public BlockData1 Back = null;

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

        public BlockData2 DeepCopy()
        {
            return new BlockData2
            (
                new BlockData1
                {
                    ID = Front.ID,
                    Variant = Front.Variant,
                    NBT = Front.NBT
                },
                new BlockData1
                {
                    ID = Back.ID,
                    Variant = Back.Variant,
                    NBT = Back.NBT
                }
            );
        }

        public byte[] Serialize()
        {
            return ArrayUtils.MegaConcat(
                EndianUnsafe.GetBytes(Front.ID),
                EndianUnsafe.GetBytes(Back.ID),
                new byte[] { (byte)(16 * Front.Variant + Back.Variant) }
                );
        }

        public static BlockData2 Deserialize(byte[] bytes, int offset = 0)
        {
            byte variants = bytes[4 + offset];

            return new BlockData2
            (
                new BlockData1
                {
                    ID = EndianUnsafe.FromBytes<BlockID>(bytes, 0 + offset),
                    Variant = (byte)(variants / 16),
                },
                new BlockData1
                {
                    ID = EndianUnsafe.FromBytes<BlockID>(bytes, 2 + offset),
                    Variant = (byte)(variants % 16),
                }
            );
        }

        public long UniqueLong()
        {
            long frontPart = ((long)Front.ID & 0xFFFF) | (((long)Front.Variant & 0xFF) << 16);
            long backPart = (((long)Back.ID & 0xFFFF) | (((long)Back.Variant & 0xFF) << 16)) << 24;

            return frontPart | backPart;
        }
    }
}
