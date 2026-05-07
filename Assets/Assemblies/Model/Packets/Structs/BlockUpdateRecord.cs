#nullable enable
using Larnix.Core.Serialization;
using Larnix.Core.Vectors;
using Larnix.Model.Blocks.Structs;
using System.Runtime.InteropServices;
using static Larnix.Model.Blocks.IWorldAPI;

namespace Larnix.Server.Packets.Structs;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct BlockUpdateRecord : ISanitizable<BlockUpdateRecord>
{
    public Vec2Int Position { get; }
    public BlockHeader2 Block { get; }
    public BreakMode BreakMode { get; }

    public BlockUpdateRecord(Vec2Int position, BlockHeader2 block, BreakMode breakMode)
    {
        Position = Sanitizer.Filter(position);
        Block = Sanitizer.Filter(block);
        BreakMode = Sanitizer.Filter(breakMode);
    }

    public BlockUpdateRecord Sanitize()
    {
        return new BlockUpdateRecord(Position, Block, BreakMode);
    }
}
