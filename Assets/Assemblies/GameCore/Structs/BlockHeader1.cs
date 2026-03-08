using System.Drawing;
using Larnix.Core.Binary;
using Larnix.Core.Enums;
using Larnix.Core.Misc;
using Larnix.Core.Vectors;

namespace Larnix.GameCore.Structs
{
    public struct BlockHeader1 : IBinary<BlockHeader1>
    {
        public const int SIZE = sizeof(BlockID) + sizeof(byte) + Vec2Int.SIZE;

        public const byte MAX_VARIANT = 0b0000_1111; // 4 bits for variant (0-15)
        private static byte Reduce(byte value) => (byte)(value & MAX_VARIANT);

        public BlockID ID { get; private set; }
        public byte Variant { get; private set; }
        public Vec2Int POS { get; private set; }

        public BlockHeader1(BlockID id, byte variant, Vec2Int pos)
        {
            ID = id;
            Variant = Reduce(variant);
            POS = pos;
        }

        public bool Deserialize(byte[] bytes, int offset = 0)
        {
            if (offset + SIZE > bytes.Length)
                return false;

            ID = Primitives.FromBytes<BlockID>(bytes, offset);
            offset += sizeof(BlockID);

            Variant = Reduce(Primitives.FromBytes<byte>(bytes, offset));
            offset += sizeof(byte);

            POS = Structures.FromBytes<Vec2Int>(bytes, offset);
            offset += Vec2Int.SIZE;

            return true;
        }

        public byte[] Serialize()
        {
            return ArrayUtils.MegaConcat(
                Primitives.GetBytes(ID),
                Primitives.GetBytes(Variant),
                Structures.GetBytes(POS)
            );
        }
    }
}
