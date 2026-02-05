using System;

namespace Larnix.Core
{
    public enum ParticleID : ushort // must be ushort for serialization
    {
        BlockBreak = 0,
        BlockPlace = 1,
    }
}
