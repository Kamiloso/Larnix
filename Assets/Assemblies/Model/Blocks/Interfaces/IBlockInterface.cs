#nullable enable
using Larnix.Model.Utils;
using Larnix.Model.Json;

namespace Larnix.Model.Blocks.All;

public interface IBlockInterface
{
    Block This => (Block)this;

    BlockID ID => This.BlockData.ID;
    byte Variant => This.BlockData.Variant;
    Storage Data => This.BlockData.NBT;

    IWorldAPI WorldAPI => This.Interfaces.WorldAPI;
    ICmdExecutor CmdExecutor => This.Interfaces.CmdExecutor;

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
