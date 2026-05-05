#nullable enable
using Larnix.Core.Vectors;
using Larnix.Model.Utils;
using Larnix.Model.Blocks.Structs;
using Larnix.Core.Serialization;
using Larnix.Socket.Payload;
using System.Runtime.InteropServices;

namespace Larnix.Server.Packets;

[CmdId(10)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct RetBlockChange : ISanitizable<RetBlockChange>
{
    public Vec2Int POS { get; }
    public long Operation { get; }
    public BlockHeader2 CurrentBlock { get; }
    public boolsrl Front { get; }
    public boolsrl Success { get; }

    public RetBlockChange(Vec2Int POS_, long operation, BlockHeader2 currentBlock, boolsrl front, boolsrl success)
    {
        POS = LarnixSanitizer.ToWorldPOS(POS_);
        Operation = operation;
        CurrentBlock = Sanitizer.Filter(currentBlock);
        Front = Sanitizer.Filter(front);
        Success = Sanitizer.Filter(success);
    }

    public RetBlockChange Sanitize()
    {
        return new RetBlockChange(POS, Operation, CurrentBlock, Front, Success);
    }
}
