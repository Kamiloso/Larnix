#nullable enable
using Larnix.Model.Utils;
using Larnix.Core.Vectors;
using Larnix.Model.Blocks.Structs;
using Larnix.Core.Serialization;
using Larnix.Socket.Payload;
using System.Runtime.InteropServices;

namespace Larnix.Server.Packets;

[CmdId(1)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct BlockChange : ISanitizable<BlockChange>
{
    public Vec2Int POS { get; }
    public BlockHeader1 Item { get; }
    public BlockHeader1 Tool { get; }
    public long Operation { get; }
    public boolsrl IsFront { get; }

    public BlockChange(Vec2Int POS_, BlockHeader1 item, BlockHeader1 tool, long operation, bool front)
    {
        POS = LarnixSanitizer.ToWorldPOS(POS_);
        Item = Sanitizer.Filter(item);
        Tool = Sanitizer.Filter(tool);
        Operation = Sanitizer.Filter(operation);
        IsFront = Sanitizer.Filter(front);
    }

    public BlockChange Sanitize()
    {
        return new BlockChange(POS, Item, Tool, Operation, IsFront);
    }
}
