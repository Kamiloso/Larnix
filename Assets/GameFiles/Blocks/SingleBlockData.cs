using Larnix.Blocks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Larnix.Blocks
{
    public class SingleBlockData
    {
        public BlockID ID = BlockID.Air;
        public byte Variant = 0; // 0 - 16
        public string NBT = "{}";

        public static bool BaseEquals(SingleBlockData b1, SingleBlockData b2)
        {
            return (
                b1.ID == b2.ID &&
                b1.Variant == b2.Variant
                );
        }

        public static bool FullEquals(SingleBlockData b1, SingleBlockData b2)
        {
            return (
                BaseEquals(b1, b2) &&
                b1.NBT == b2.NBT
                );
        }

        public SingleBlockData ShallowCopy()
        {
            return new SingleBlockData
            {
                ID = ID,
                Variant = Variant,
                NBT = NBT,
            };
        }
    }
}
