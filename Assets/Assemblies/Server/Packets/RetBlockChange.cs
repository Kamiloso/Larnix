#nullable enable
using Larnix.Core.Vectors;
using Larnix.Model.Utils;
using Larnix.Model.Blocks.Structs;
using Larnix.Core.Serialization;
using Larnix.Socket.Payload;
using System.Runtime.InteropServices;

namespace Larnix.Server.Packets;

[CmdId(0x0A)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct RetBlockChange : ISanitizable<RetBlockChange>
{
    public Vec2Int POS { get; }
    public long Operation { get; }
    public BlockHeader2 CurrentBlock { get; }

    private readonly byte _flags;
    public bool Front => (_flags & 0b01) != 0;
    public bool Success => (_flags & 0b10) != 0;

    public RetBlockChange(Vec2Int POS_, long operation, BlockHeader2 currentBlock, bool front, bool success)
    {
        POS = BlockUtils.BlockInWorld(POS_) ? POS_ : Vec2Int.Zero;
        Operation = operation;
        CurrentBlock = currentBlock;
        _flags = (byte)((front ? 0b01 : 0b00) | (success ? 0b10 : 0b00));
    }

    public RetBlockChange Sanitize()
    {
        return new RetBlockChange(POS, Operation, CurrentBlock, Front, Success);
    }
}
