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
        BlockServer This => (BlockServer)this;
        IWorldAPI WorldAPI => This.WorldAPI;
        Storage Data => This.BlockData.Data;

        void SelfChangeID(BlockID id)
        {
            BlockData1 blockTemplate = new BlockData1(id, This.BlockData.Variant, This.BlockData.Data);
            WorldAPI.ReplaceBlock(This.Position, This.IsFront, blockTemplate, IWorldAPI.BreakMode.Weak);
        }

        void SelfChangeVariant(byte variant)
        {
            WorldAPI.SetBlockVariant(This.Position, This.IsFront, variant, IWorldAPI.BreakMode.Weak);
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
