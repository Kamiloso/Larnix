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
