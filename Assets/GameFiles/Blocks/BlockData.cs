using System.Collections;
using QuickNet;

namespace Larnix.Blocks
{
    public class BlockData
    {
        public SingleBlockData Front = null;
        public SingleBlockData Back = null;

        public BlockData()
        {
            Front = new();
            Back = new();
        }
        public BlockData(SingleBlockData front, SingleBlockData back)
        {
            Front = front ?? new();
            Back = back ?? new();
        }

        public BlockData DeepCopy()
        {
            return new BlockData
            (
                new SingleBlockData
                {
                    ID = Front.ID,
                    Variant = Front.Variant,
                    NBT = Front.NBT
                },
                new SingleBlockData
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

        public static BlockData Deserialize(byte[] bytes, int offset = 0)
        {
            byte variants = bytes[4 + offset];

            return new BlockData
            (
                new SingleBlockData
                {
                    ID = EndianUnsafe.FromBytes<BlockID>(bytes, 0 + offset),
                    Variant = (byte)(variants / 16),
                },
                new SingleBlockData
                {
                    ID = EndianUnsafe.FromBytes<BlockID>(bytes, 2 + offset),
                    Variant = (byte)(variants % 16),
                }
            );
        }
    }
}
