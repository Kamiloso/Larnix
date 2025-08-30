using System.Collections;
using QuickNet;

namespace Larnix.Blocks
{
    public class BlockData
    {
        public SingleBlockData Front = new();
        public SingleBlockData Back = new();

        public BlockData() { }
        public BlockData(SingleBlockData front, SingleBlockData back)
        {
            Front = front;
            Back = back;
        }

        public BlockData ShallowCopy()
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

        public byte[] SerializeBaseData()
        {
            return ArrayUtils.MegaConcat(
                EndianUnsafe.GetBytes(Front.ID),
                EndianUnsafe.GetBytes(Back.ID),
                new byte[] { (byte)(16 * Front.Variant + Back.Variant) }
                );
        }

        public void DeserializeBaseData(byte[] bytes)
        {
            Front.ID = EndianUnsafe.FromBytes<BlockID>(bytes, 0);
            Back.ID = EndianUnsafe.FromBytes<BlockID>(bytes, 2);

            byte variants = bytes[4];
            Front.Variant = (byte)(variants / 16);
            Back.Variant = (byte)(variants % 16);
        }
    }
}
