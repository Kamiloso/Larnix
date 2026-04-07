#nullable enable
using Larnix.Core.Vectors;
using Larnix.Model.Blocks.Structs;
using Larnix.Core;
using System.Runtime.InteropServices;
using static Larnix.Model.Blocks.IWorldAPI;

namespace Larnix.Server.Packets.Structs;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct BlockUpdateRecord : IFixedStruct<BlockUpdateRecord>
{
    public readonly Vec2Int Position;
    public readonly BlockHeader2 Block;
    public readonly BreakMode BreakMode;

    public BlockUpdateRecord(Vec2Int position, BlockHeader2 block, BreakMode breakMode)
    {
        Position = position;
        Block = block;
        BreakMode = breakMode;
    }
}
