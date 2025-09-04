using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Blocks
{
    public interface IBlockInterface
    {
        BlockServer ThisBlock => (BlockServer)this;
        IWorldAPI WorldAPI => ThisBlock.WorldAPI;
    }
}
