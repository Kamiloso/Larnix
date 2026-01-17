using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Larnix.Blocks;

namespace Larnix.Blocks.Structs
{
    public class BlockData1
    {
        public BlockID ID = 0;
        public byte Variant = 0; // 0 - 16
        public string NBT = "{}";

        public static bool BaseEquals(BlockData1 b1, BlockData1 b2)
        {
            return (
                b1.ID == b2.ID &&
                b1.Variant == b2.Variant
                );
        }

        public static bool FullEquals(BlockData1 b1, BlockData1 b2)
        {
            return (
                BaseEquals(b1, b2) &&
                b1.NBT == b2.NBT
                );
        }

        public BlockData1 DeepCopy()
        {
            return new BlockData1
            {
                ID = ID,
                Variant = Variant,
                NBT = NBT,
            };
        }
    }
}
