using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Larnix.Server.Entities;
using Larnix.Core.Utils;
using Larnix.Core.Vectors;
using Larnix.Server.Data;

namespace Larnix.Server.Terrain
{
    internal class AtomicChunks : Singleton
    {
        public AtomicChunks(Server server) : base(server) {}

        public void SuggestAtomicLoad(Vec2Int chunk)
        {
            
        }

        public bool IsAtomicLoaded(Vec2Int chunk)
        {
            return true;
        }
    }
}
