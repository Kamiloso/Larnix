using Larnix.Core;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Utils;
using Larnix.Core.Json;
using Larnix.Blocks.Structs;

namespace Larnix.Blocks.All
{
    public interface IBlockInterface
    {
        Block This => (Block)this;
        IWorldAPI WorldAPI => This.WorldAPI;
        Storage Data => This.BlockData.Data;

        void SelfChangeID(BlockID id)
        {
            WorldAPI.MutateBlockID(This.Position, This.IsFront, id);
        }

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
