using Larnix.GameCore;
using System.Collections;
using System.Collections.Generic;
using Larnix.GameCore.Utils;
using Larnix.GameCore.Json;
using Larnix.Blocks.Structs;

namespace Larnix.Blocks.All
{
    public interface IBlockInterface
    {
        Block This => (Block)this;
        IWorldAPI WorldAPI => This.WorldAPI;
        Storage Data => This.BlockData.NBT;

        void SelfChangeVariant(byte variant)
        {
            WorldAPI.MutateBlockVariant(This.Position, This.IsFront, variant);
        }

        string STATIC_GetBlockName(byte variant)
        {
            return STATIC_GetBlockNameFallback(variant);
        }

        string STATIC_GetBlockNameFallback(byte variant)
        {
            return Common.SplitPascalCase(GetType().Name + (variant == 0 ? "" : $" {variant}"));
        }
    }
}
