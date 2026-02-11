using Larnix.Core;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Utils;
using Larnix.Core.Json;

namespace Larnix.Blocks
{
    public interface IBlockInterface
    {
        BlockServer This => (BlockServer)this;
        IWorldAPI WorldAPI => This.WorldAPI;
        Storage Data => This.BlockData.Data;

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
