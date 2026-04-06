using Larnix.Model;
using System.Collections;
using System.Collections.Generic;
using Larnix.Model.Utils;
using Larnix.Model.Json;
using Larnix.Model.Blocks.Structs;
using Larnix.Model.Interfaces;

namespace Larnix.Model.Blocks.All;

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
