#nullable enable
using Larnix.Core.Vectors;

namespace Larnix.Model.Utils;

public static class LarnixSanitizer
{
    public static Vec2Int ToWorldPOS(Vec2Int POS)
    {
        return BlockUtils.BlockInWorld(POS)
            ? POS
            : Vec2Int.Zero;
    }

    public static Vec2Int ToWorldChunk(Vec2Int chunkpos)
    {
        return BlockUtils.ChunkInWorld(chunkpos)
            ? chunkpos
            : Vec2Int.Zero;
    }
}
