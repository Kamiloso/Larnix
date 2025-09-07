using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Blocks
{
    public interface IBlockInterface
    {
        BlockServer This => (BlockServer)this;
        IWorldAPI WorldAPI => This.WorldAPI;
    }
}
