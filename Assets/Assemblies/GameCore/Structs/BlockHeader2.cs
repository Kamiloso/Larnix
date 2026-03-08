using Larnix.Core.Binary;
using Larnix.Core.Misc;
using Larnix.GameCore.Structs;

namespace Larnix.GameCore
{
    public struct BlockHeader2 : IBinary<BlockHeader2>
    {
        public const int SIZE = BlockHeader1.SIZE * 2;

        public BlockHeader1 Front { get; private set; }
        public BlockHeader1 Back { get; private set; }

        public BlockHeader2(BlockHeader1 front, BlockHeader1 back)
        {
            Front = front;
            Back = back;
        }

        public bool Deserialize(byte[] data, int offset = 0)
        {
            if (offset + SIZE > data.Length)
                return false;

            Front = Structures.FromBytes<BlockHeader1>(data, offset);
            offset += BlockHeader1.SIZE;

            Back = Structures.FromBytes<BlockHeader1>(data, offset);
            offset += BlockHeader1.SIZE;
            
            return true;
        }

        public readonly byte[] Serialize()
        {
            return ArrayUtils.MegaConcat(
                Structures.GetBytes(Front),
                Structures.GetBytes(Back)
            );
        }
    }
}
