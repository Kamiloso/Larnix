using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Larnix.Core.Binary;
using Larnix.Core.Json;
using Larnix.Core.Utils;

namespace Larnix.Blocks.Structs
{
    public class BlockData1 : IBinary<BlockData1>
    {
        public const int SIZE = sizeof(BlockID) + sizeof(byte);

        public BlockID ID { get; private set; }
        private byte _variant;
        public byte Variant
        {
            get => _variant;
            private set => _variant = (byte)(value & 0b00001111);
        }
        public Storage Data { get; private set; }

        // USE CAREFULLY! Breaks immutability, but prevents unnecessary allocations in some cases.
        public void __MutateID__(BlockID id) => ID = id;
        public void __MutateVariant__(byte variant) => Variant = variant;

        public static BlockData1 Air => new(BlockID.Air, 0);
        public static BlockData1 UltimateTool => new(BlockID.UltimateTool, 0);

        public BlockData1() => Data = new();
        public BlockData1(BlockID id, byte variant, Storage data = null)
        {
            ID = id;
            Variant = variant;
            Data = data ?? new();
        }

        public byte[] Serialize()
        {
            return ArrayUtils.MegaConcat(
                Primitives.GetBytes(ID),
                Primitives.GetBytes(Variant)
                );
        }

        public bool Deserialize(byte[] bytes, int offset = 0)
        {
            if (offset + SIZE > bytes.Length)
                return false;
            
            ID = Primitives.FromBytes<BlockID>(bytes, offset);
            offset += sizeof(BlockID);

            Variant = Primitives.FromBytes<byte>(bytes, offset);
            offset += sizeof(byte);

            return true;
        }

        public BlockData1 BinaryCopy() => ((IBinary<BlockData1>)this).BinaryCopy();
        BlockData1 IBinary<BlockData1>.BinaryCopy()
        {
            return new BlockData1
            {
                ID = ID,
                Variant = Variant,
            };
        }

        public bool BinaryEquals(BlockData1 other) => ((IBinary<BlockData1>)this).BinaryEquals(other);
        bool IBinary<BlockData1>.BinaryEquals(BlockData1 other)
        {
            return ID == other.ID && Variant == other.Variant;
        }

        public BlockData1 DeepCopy()
        {
            return new BlockData1
            {
                ID = ID,
                Variant = Variant,
                Data = Data.DeepCopy(),
            };
        }
    }
}
